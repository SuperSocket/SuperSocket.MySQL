using System;
using System.Threading.Tasks;
using SuperSocket.Server;
using SuperSocket.Channel;
using Microsoft.Extensions.Logging;

namespace SuperSocket.MySQL.Authentication
{
    /// <summary>
    /// MySQL session that handles authentication before allowing query processing.
    /// </summary>
    public class MySQLSession : AppSession
    {
        private readonly MySQLAuthenticationHandler _authHandler;
        private MySQLHandshakePacket _handshakePacket;
        private bool _isAuthenticated = false;

        public MySQLSession()
        {
            _authHandler = new MySQLAuthenticationHandler();
        }

        protected override async ValueTask OnSessionConnectedAsync()
        {
            await base.OnSessionConnectedAsync();
            
            // Send handshake packet immediately upon connection
            _handshakePacket = _authHandler.CreateHandshake();
            var handshakeBytes = _handshakePacket.ToBytes();
            
            await SendAsync(handshakeBytes);
            
            Logger?.LogInformation($"Sent handshake to connection {_handshakePacket.ConnectionId}");
        }

        public async Task<bool> HandleAuthenticationAsync(MySQLHandshakeResponsePacket response)
        {
            try
            {
                var salt = _handshakePacket?.GetFullSalt();
                if (salt == null)
                {
                    Logger?.LogWarning("No handshake salt available for authentication");
                    await SendErrorAsync(1045, "Access denied");
                    return false;
                }

                var isValid = _authHandler.ValidateCredentials(response, salt);
                
                if (isValid)
                {
                    _isAuthenticated = true;
                    var okPacket = _authHandler.CreateOkPacket();
                    await SendAsync(okPacket);
                    
                    Logger?.LogInformation($"User '{response.Username}' authenticated successfully");
                    return true;
                }
                else
                {
                    await SendErrorAsync(1045, "Access denied for user '" + response.Username + "'");
                    Logger?.LogWarning($"Authentication failed for user '{response.Username}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error during authentication");
                await SendErrorAsync(1045, "Authentication error");
                return false;
            }
        }

        public bool IsAuthenticated => _isAuthenticated;

        private async Task SendErrorAsync(ushort errorCode, string message)
        {
            var errorPacket = _authHandler.CreateErrorPacket(errorCode, message);
            await SendAsync(errorPacket);
        }

        private async Task SendAsync(byte[] data)
        {
            await Channel.SendAsync(new ReadOnlyMemory<byte>(data));
        }

        protected virtual async ValueTask OnPackageReceived(MySQLHandshakeResponsePacket package)
        {
            if (!IsAuthenticated)
            {
                // This is the authentication response
                var success = await HandleAuthenticationAsync(package);
                if (!success)
                {
                    // Authentication failed, close the connection
                    await CloseAsync();
                }
            }
            else
            {
                // This should be a command packet after authentication
                // In a real implementation, you'd switch filters after authentication
                Logger?.LogWarning("Received data after authentication - command processing not implemented");
            }
        }

        protected override async ValueTask OnSessionClosedAsync(EventArgs e)
        {
            Logger?.LogInformation($"MySQL session closed");
            await base.OnSessionClosedAsync(e);
        }
    }
}