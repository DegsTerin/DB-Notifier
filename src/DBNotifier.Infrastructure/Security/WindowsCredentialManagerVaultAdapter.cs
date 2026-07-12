using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DBNotifier.Application.Security;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

public sealed class WindowsCredentialManagerVaultAdapter : ICredentialVaultAdapter
{
    private const int GenericCredentialType = 1;

    public string ProviderId => "windows-credential-manager";

    public ValueTask<IProviderCredential> ResolveAsync(string locator, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locator);
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return ValueTask.FromException<IProviderCredential>(new CredentialUnavailableException(
                "Windows Credential Manager is unavailable on this platform."));
        }

        if (!NativeMethods.CredRead(locator, GenericCredentialType, 0, out IntPtr credentialPointer))
        {
            int error = Marshal.GetLastPInvokeError();
            return ValueTask.FromException<IProviderCredential>(new CredentialUnavailableException(
                error == 1168
                    ? "The Windows credential target was not found."
                    : "Windows Credential Manager could not resolve the target."));
        }

        byte[]? blob = null;
        try
        {
            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                throw new CredentialUnavailableException("The Windows credential contains no secret value.");
            }

            int blobSize = checked((int)credential.CredentialBlobSize);
            blob = new byte[blobSize];
            Marshal.Copy(credential.CredentialBlob, blob, 0, blob.Length);
            string? userName = Marshal.PtrToStringUni(credential.UserName);
            char[] secret = Encoding.UTF8.GetChars(blob);
            try
            {
                return ValueTask.FromResult<IProviderCredential>(new ProviderCredentialLease(userName, secret));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(secret.AsSpan()));
            }
        }
        catch (Exception exception) when (exception is ArgumentException or Win32Exception)
        {
            return ValueTask.FromException<IProviderCredential>(new CredentialUnavailableException(
                "Windows Credential Manager returned an invalid credential payload."));
        }
        finally
        {
            if (blob is not null)
            {
                CryptographicOperations.ZeroMemory(blob);
            }

            NativeMethods.CredFree(credentialPointer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    private static class NativeMethods
    {
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPointer);

        [DllImport("advapi32.dll")]
        internal static extern void CredFree(IntPtr credentialPointer);
    }
}
