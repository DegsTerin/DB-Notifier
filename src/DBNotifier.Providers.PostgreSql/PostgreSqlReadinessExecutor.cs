// Module purpose: Executes readiness probes inside the isolated PostgreSQL provider; the core remains engine-neutral.
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>
/// Runs an approved-path <c>pg_isready</c> utility without a shell and falls back to explicitly transport-only evidence when
/// the utility is absent, while preserving bounded process and network deadlines.
/// </summary>
/// <param name="discovery">Approved-root executable discovery boundary.</param>
/// <param name="transportProbe">TCP fallback that never asserts authenticated database health.</param>
public sealed class PostgreSqlReadinessExecutor(
    IPostgreSqlExecutableDiscovery discovery,
    IPostgreSqlTransportProbe transportProbe) : IPostgreSqlReadinessExecutor
{
    /// <summary>Executes one provider readiness probe or a clearly labelled TCP-only fallback.</summary>
    /// <param name="endpoint">Validated endpoint containing no connection string or secret.</param>
    /// <param name="timeout">Positive deadline applied to utility execution or the transport fallback.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to process and socket work.</param>
    /// <returns>A canonical readiness state and evidence method.</returns>
    public async ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        Stopwatch stopwatch = Stopwatch.StartNew();
        PostgreSqlExecutableDiscoveryResult discovered = discovery.Resolve(
            PostgreSqlExecutableDiscovery.CreateRuntimeRequest(endpoint));
        if (discovered.State == PostgreSqlExecutableDiscoveryState.Invalid)
        {
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.InvalidConfiguration, "pg_isready", stopwatch.Elapsed);
        }
        if (discovered.State == PostgreSqlExecutableDiscoveryState.NotFound)
        {
            return await ProbeTransportAsync(endpoint, timeout, stopwatch, cancellationToken).ConfigureAwait(false);
        }

        endpoint = endpoint with { PgIsReadyPath = discovered.ExecutablePath! };
        try
        {
            ProcessStartInfo startInfo = CreateStartInfo(endpoint, timeout);
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
            {
                return await ProbeTransportAsync(endpoint, timeout, stopwatch, cancellationToken).ConfigureAwait(false);
            }

            if (await WaitForExitOrTerminateAsync(process, timeout, cancellationToken).ConfigureAwait(false))
            {
                return new PostgreSqlReadinessResult(PostgreSqlReadinessState.TimedOut, "pg_isready", stopwatch.Elapsed);
            }

            PostgreSqlReadinessState state = process.ExitCode switch
            {
                0 => PostgreSqlReadinessState.Accepting,
                1 => PostgreSqlReadinessState.Rejecting,
                2 => PostgreSqlReadinessState.NoResponse,
                _ => PostgreSqlReadinessState.InvalidConfiguration,
            };

            return new PostgreSqlReadinessResult(state, "pg_isready", stopwatch.Elapsed);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3)
        {
            return await ProbeTransportAsync(endpoint, timeout, stopwatch, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return await ProbeTransportAsync(endpoint, timeout, stopwatch, cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception)
        {
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.InvalidConfiguration,
                "pg_isready",
                stopwatch.Elapsed);
        }
    }

    /// <summary>Creates the fixed argument-list process contract without database or arbitrary extra arguments.</summary>
    /// <param name="endpoint">Validated endpoint containing the discovered utility path, host and port.</param>
    /// <param name="timeout">Positive readiness deadline converted to whole seconds.</param>
    /// <returns>A shell-free process start contract with redirected output.</returns>
    internal static ProcessStartInfo CreateStartInfo(PostgreSqlEndpoint endpoint, TimeSpan timeout)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = endpoint.PgIsReadyPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add(endpoint.Host);
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(endpoint.Port.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-t");
        startInfo.ArgumentList.Add(Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds)).ToString(CultureInfo.InvariantCulture));
        return startInfo;
    }

    private async ValueTask<PostgreSqlReadinessResult> ProbeTransportAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        PostgreSqlTransportState state = await transportProbe.ProbeAsync(endpoint, timeout, cancellationToken)
            .ConfigureAwait(false);
        return new PostgreSqlReadinessResult(state switch
        {
            PostgreSqlTransportState.Reachable => PostgreSqlReadinessState.TransportReachable,
            PostgreSqlTransportState.NoResponse => PostgreSqlReadinessState.NoResponse,
            PostgreSqlTransportState.TimedOut => PostgreSqlReadinessState.TimedOut,
            _ => PostgreSqlReadinessState.InvalidConfiguration,
        }, "tcp", stopwatch.Elapsed);
    }

    /// <summary>Kills the complete synthetic or provider utility tree and waits independently for confirmed exit.</summary>
    /// <param name="process">Started process whose complete tree is owned by the current readiness attempt.</param>
    /// <returns><see langword="true"/> only when the root process exit is confirmed within the cleanup bound.</returns>
    internal static async ValueTask<bool> TerminateProcessTreeAsync(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // The process exited between timeout observation and termination.
        }

        using CancellationTokenSource cleanupDeadline = new(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(cleanupDeadline.Token).ConfigureAwait(false);
            return process.HasExited;
        }
        catch (OperationCanceledException) when (cleanupDeadline.IsCancellationRequested)
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return process.HasExited;
        }
    }

    /// <summary>Waits for normal exit or performs bounded tree termination on timeout or caller cancellation.</summary>
    /// <param name="process">Started process owned by the current readiness attempt.</param>
    /// <param name="timeout">Positive readiness deadline.</param>
    /// <param name="cancellationToken">Caller cancellation propagated only after tree exit is confirmed.</param>
    /// <returns><see langword="true"/> when the process timed out; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process-tree termination cannot be confirmed.</exception>
    /// <exception cref="OperationCanceledException">Thrown after confirmed cleanup when the caller cancels.</exception>
    internal static async ValueTask<bool> WaitForExitOrTerminateAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            bool terminated = await TerminateProcessTreeAsync(process).ConfigureAwait(false);
            if (!terminated)
            {
                throw new InvalidOperationException("postgresql.pg_isready_termination_unconfirmed");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return true;
        }
    }
}

/// <summary>Checks bounded socket reachability only and never promotes it to provider readiness or health.</summary>
public sealed class TcpPostgreSqlTransportProbe : IPostgreSqlTransportProbe
{
    /// <summary>Attempts one direct TCP connection to a non-socket endpoint within the supplied deadline.</summary>
    /// <param name="endpoint">Validated host and port.</param>
    /// <param name="timeout">Positive transport deadline.</param>
    /// <param name="cancellationToken">Caller cancellation distinct from timeout.</param>
    /// <returns>Reachable, no-response, timed-out or invalid transport evidence.</returns>
    public async ValueTask<PostgreSqlTransportState> ProbeAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (endpoint.Host.StartsWith('/'))
        {
            return PostgreSqlTransportState.Invalid;
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(endpoint.Host, endpoint.Port, deadline.Token).ConfigureAwait(false);
            return PostgreSqlTransportState.Reachable;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PostgreSqlTransportState.TimedOut;
        }
        catch (SocketException)
        {
            return PostgreSqlTransportState.NoResponse;
        }
    }
}
