using SMBLibrary;
using SMBLibrary.Client;
using System.Net;

namespace OES.Services.Helpers
{
    public class NetworkConnection : IDisposable
    {
        private readonly SMB2Client _client;
        private ISMBFileStore? _fileStore;
        private bool _disposed;

        public NetworkConnection(string uncPath, string username, string password, string domain = "")
        {
            var (serverHost, shareName) = ParseUncPath(uncPath);
            _client = new SMB2Client();

            var serverIP = ResolveHost(serverHost);

            if (!_client.Connect(serverIP, SMBTransportType.DirectTCPTransport))
                throw new Exception($"Failed to connect to SMB server '{serverHost}' ({serverIP}).");

            var loginStatus = _client.Login(domain, username, password);
            if (loginStatus != NTStatus.STATUS_SUCCESS)
            {
                _client.Disconnect();
                throw new Exception($"SMB authentication failed for '{username}' on '{serverHost}'. Status: {loginStatus}");
            }

            NTStatus connectStatus;
            _fileStore = _client.TreeConnect(shareName, out connectStatus);

            if (connectStatus != NTStatus.STATUS_SUCCESS)
            {
                _client.Logoff();
                _client.Disconnect();

                throw new Exception($"Failed to open SMB share '{shareName}'. Status: {connectStatus}");
            }
        }

        public ISMBFileStore GetShare()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _fileStore!;
        }

        public void WriteFile(string fileName, byte[] content)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var store = _fileStore!;

            var createStatus = store.CreateFile(
                out var handle,
                out _,
                fileName,
                AccessMask.GENERIC_WRITE,
                SMBLibrary.FileAttributes.Normal,
                ShareAccess.None,
                CreateDisposition.FILE_OVERWRITE_IF,
                CreateOptions.FILE_NON_DIRECTORY_FILE,
                null
            );

            if (createStatus != NTStatus.STATUS_SUCCESS)
                throw new Exception($"SMB: Failed to create file '{fileName}'. Status: {createStatus}");

            try
            {
                int offset = 0;
                while (offset < content.Length)
                {
                    int length = Math.Min(65536, content.Length - offset);
                    byte[] chunk = new byte[length];
                    Buffer.BlockCopy(content, offset, chunk, 0, length);

                    var writeStatus = store.WriteFile(out _, handle, offset, chunk);
                    if (writeStatus != NTStatus.STATUS_SUCCESS)
                        throw new Exception($"SMB: Failed to write chunk at offset {offset}. Status: {writeStatus}");

                    offset += length;
                }
            }
            finally
            {
                store.CloseFile(handle);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _fileStore?.Disconnect(); } catch { }
            try { _client.Logoff(); } catch { }
            try { _client.Disconnect(); } catch { }
        }

        public static (string server, string share) ParseUncPath(string path)
        {
            var normalized = path.Replace('/', '\\').TrimStart('\\');
            var parts = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ArgumentException($"Invalid UNC path '{path}'. Expected: \\\\server\\share");
            return (parts[0], parts[1]);
        }

        private static IPAddress ResolveHost(string host)
        {
            if (IPAddress.TryParse(host, out var ip)) return ip;
            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
                throw new Exception($"Could not resolve SMB hostname '{host}'.");
            return addresses[0];
        }
    }
}