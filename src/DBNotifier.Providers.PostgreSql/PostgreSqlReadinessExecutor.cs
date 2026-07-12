// Module purpose: Implements Postgre Sql Readiness Executor inside the isolated PostgreSQL provider; the core remains engine-neutral.
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace DBNotifier.Providers.PostgreSql;

public sealed class PostgreSqlReadinessExecutor(
    IPostgreSqlExecutableDiscovery discovery,
    IPostgreSqlTransportProbe transportProbe) : IPostgreSqlReadinessExecutor
{
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

            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
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
        if (!string.IsNullOrWhiteSpace(endpoint.Database))
        {
            startInfo.ArgumentList.Add("-d");
            startInfo.ArgumentList.Add(endpoint.Database);
        }

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

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // The process exited between timeout observation and termination.
        }
    }
}

public sealed class TcpPostgreSqlTransportProbe : IPostgreSqlTransportProbe
{
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
