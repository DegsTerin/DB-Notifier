// Module purpose: Implements Linux Secret Service Vault Adapter as an outer adapter behind application or provider contracts.
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DBNotifier.Application.Security;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

public sealed class LinuxSecretServiceVaultAdapter : ICredentialVaultAdapter
{
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(5);

    public string ProviderId => "linux-secret-service";

    public async ValueTask<IProviderCredential> ResolveAsync(
        string locator,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locator);
        if (locator.Contains('\n') || locator.Contains('\r'))
        {
            throw new CredentialUnavailableException("The Linux Secret Service locator is invalid.");
        }

        if (!OperatingSystem.IsLinux())
        {
            throw new CredentialUnavailableException("Linux Secret Service is unavailable on this platform.");
        }

        ProcessStartInfo startInfo = CreateStartInfo(locator);

        try
        {
            using Process process = new() { StartInfo = startInfo };
            if (!process.Start())
            {
                throw new CredentialUnavailableException("Linux Secret Service lookup could not be started.");
            }

            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(LookupTimeout);
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(deadline.Token);
            Task<string> standardError = process.StandardError.ReadToEndAsync(deadline.Token);
            try
            {
                await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw new CredentialUnavailableException("Linux Secret Service lookup timed out.");
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            string payload = await standardOutput.ConfigureAwait(false);
            _ = await standardError.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new CredentialUnavailableException("Linux Secret Service did not resolve the credential.");
            }

            int separator = payload.IndexOf('\n');
            if (separator <= 0 || separator == payload.Length - 1)
            {
                throw new CredentialUnavailableException(
                    "The Linux Secret Service value must contain username, newline, and secret.");
            }

            string userName = payload[..separator].TrimEnd('\r');
            char[] secret = payload[(separator + 1)..].TrimEnd('\r', '\n').ToCharArray();
            try
            {
                return new ProviderCredentialLease(userName, secret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(secret.AsSpan()));
                payload = string.Empty;
            }
        }
        catch (Win32Exception)
        {
            throw new CredentialUnavailableException("The secret-tool executable is unavailable.");
        }
    }

    internal static ProcessStartInfo CreateStartInfo(string locator)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "secret-tool",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("lookup");
        startInfo.ArgumentList.Add("application");
        startInfo.ArgumentList.Add("db-notifier");
        startInfo.ArgumentList.Add("id");
        startInfo.ArgumentList.Add(locator);
        return startInfo;
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // The process exited while the timeout path was terminating it.
        }
    }
}
