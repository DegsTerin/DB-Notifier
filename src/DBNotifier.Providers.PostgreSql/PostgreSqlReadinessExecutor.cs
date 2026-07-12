using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace DBNotifier.Providers.PostgreSql;

public sealed class PostgreSqlReadinessExecutor : IPostgreSqlReadinessExecutor
{
    public async ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        Stopwatch stopwatch = Stopwatch.StartNew();
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

    private static async ValueTask<PostgreSqlReadinessResult> ProbeTransportAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        if (endpoint.Host.StartsWith('/'))
        {
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.InvalidConfiguration,
                "tcp",
                stopwatch.Elapsed);
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(endpoint.Host, endpoint.Port, deadline.Token).ConfigureAwait(false);
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.TransportReachable,
                "tcp",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.TimedOut,
                "tcp",
                stopwatch.Elapsed);
        }
        catch (SocketException)
        {
            return new PostgreSqlReadinessResult(
                PostgreSqlReadinessState.NoResponse,
                "tcp",
                stopwatch.Elapsed);
        }
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
