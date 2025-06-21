# MySQL Authentication Implementation

This document describes the MySQL authentication implementation for SuperSocket.MySQL that follows the MySQL protocol specification.

## Overview

The authentication system implements the standard MySQL handshake protocol:

1. **Server Hello**: Server sends initial handshake packet with protocol version, server version, connection ID, and authentication challenge (salt)
2. **Client Authentication**: Client responds with username, scrambled password, and other connection parameters
3. **Server Response**: Server validates credentials and sends OK packet (success) or ERR packet (failure)

## Architecture

The implementation consists of several key components:

### Core Classes

#### `MySQLHandshakePacket`
- Represents the initial handshake packet sent by the server
- Contains protocol version, server version, connection ID, and 20-byte salt
- Generates binary packet data according to MySQL protocol specification
- Located: `src/SuperSocket.MySQL/Authentication/MySQLHandshakePacket.cs`

#### `MySQLHandshakeResponsePacket`
- Represents the client's authentication response
- Parses client capabilities, username, scrambled password, and database name
- Handles binary packet parsing from SuperSocket pipeline
- Located: `src/SuperSocket.MySQL/Authentication/MySQLHandshakeResponsePacket.cs`

#### `MySQLAuthenticationHandler`
- Coordinates the authentication flow
- Implements MySQL native password scrambling algorithm (SHA1-based)
- Validates credentials against hardcoded username/password ("test"/"test")
- Generates OK and ERR response packets
- Located: `src/SuperSocket.MySQL/Authentication/MySQLAuthenticationHandler.cs`

#### `MySQLSession`
- Extends SuperSocket AppSession to handle MySQL connections
- Automatically sends handshake packet on connection
- Manages authentication state and credential validation
- Located: `src/SuperSocket.MySQL/Authentication/MySQLSession.cs`

#### `MySQLHandshakeResponseFilter`
- SuperSocket filter for parsing handshake response packets
- Integrates with SuperSocket's PackagePartsPipelineFilter system
- Located: `src/SuperSocket.MySQL/Authentication/MySQLHandshakeResponseFilter.cs`

## Usage

### Basic Authentication Setup

```csharp
using SuperSocket.MySQL.Authentication;

// Create authentication handler
var authHandler = new MySQLAuthenticationHandler();

// Generate handshake packet
var handshake = authHandler.CreateHandshake();
var handshakeBytes = handshake.ToBytes();

// Send handshake to client
await session.SendAsync(handshakeBytes);

// Parse client response
var response = MySQLHandshakeResponsePacket.ParseFromBytes(clientData, 0, clientData.Length);

// Validate credentials
var salt = handshake.GetFullSalt();
bool isValid = authHandler.ValidateCredentials(response, salt);

if (isValid)
{
    var okPacket = authHandler.CreateOkPacket();
    await session.SendAsync(okPacket);
}
else
{
    var errorPacket = authHandler.CreateErrorPacket(1045, "Access denied");
    await session.SendAsync(errorPacket);
}
```

### SuperSocket Integration

```csharp
using SuperSocket.MySQL.Authentication;
using SuperSocket.Server;

// Create server with MySQL authentication
var host = SuperSocketHostBuilder
    .Create<MySQLHandshakeResponsePacket, MySQLHandshakeResponseFilter>()
    .UseSession<MySQLSession>()
    .ConfigureServices((context, services) =>
    {
        services.Configure<ServerOptions>(options =>
        {
            options.Listeners = new[]
            {
                new ListenOptions { Ip = "127.0.0.1", Port = 3306 }
            };
        });
    })
    .Build();

await host.RunAsync();
```

### Custom Session Implementation

```csharp
public class CustomMySQLSession : MySQLSession
{
    protected override async ValueTask OnPackageReceived(MySQLHandshakeResponsePacket package)
    {
        if (!IsAuthenticated)
        {
            var success = await HandleAuthenticationAsync(package);
            if (!success)
            {
                await CloseAsync();
                return;
            }
            
            // Authentication successful, ready for command processing
            Logger?.LogInformation($"User {package.Username} authenticated successfully");
        }
        else
        {
            // Handle MySQL commands after authentication
            await ProcessMySQLCommand(package);
        }
    }
    
    private async Task ProcessMySQLCommand(MySQLHandshakeResponsePacket package)
    {
        // Implement your MySQL command processing logic here
        // This would typically involve switching to a different packet filter
        // for handling SQL queries, prepared statements, etc.
    }
}
```

## Protocol Details

### Handshake Packet Structure

The handshake packet follows MySQL Protocol Version 10 format:

```
1 byte     - Protocol version (10)
string     - Server version (null-terminated)
4 bytes    - Connection ID
8 bytes    - Authentication plugin data part 1
1 byte     - Filler (0x00)
2 bytes    - Capability flags (lower 2 bytes)
1 byte     - Character set
2 bytes    - Status flags
2 bytes    - Capability flags (upper 2 bytes)
1 byte     - Length of authentication plugin data
10 bytes   - Reserved (all zeros)
12 bytes   - Authentication plugin data part 2
1 byte     - Null terminator
string     - Authentication plugin name (null-terminated)
```

### Password Scrambling Algorithm

The implementation uses MySQL's native password authentication:

```
SHA1(password) XOR SHA1(salt + SHA1(SHA1(password)))
```

Where:
- `password` is the plaintext password
- `salt` is the 20-byte challenge from the handshake packet
- `SHA1()` is the SHA-1 hash function
- `XOR` is bitwise exclusive OR

### Authentication Flow

1. **Connection Established**: Client connects to server
2. **Server Hello**: Server immediately sends handshake packet with unique salt
3. **Client Response**: Client sends handshake response with scrambled password
4. **Validation**: Server validates username and password using salt
5. **Result**: Server sends OK packet (authentication success) or ERR packet (failure)

## Configuration

### Hardcoded Credentials

Currently, the system uses hardcoded credentials for simplicity:
- Username: `"test"`
- Password: `"test"`

To modify credentials, update the `MySQLAuthenticationHandler` class:

```csharp
private readonly string _validUsername = "your_username";
private readonly string _validPassword = "your_password";
```

### Server Information

Default server information can be customized in `MySQLHandshakePacket`:

```csharp
public byte ProtocolVersion { get; set; } = 10;
public string ServerVersion { get; set; } = "8.0.0-supersocket";
public ushort CapabilityFlagsLower { get; set; } = 0xF7FF;
public byte CharacterSet { get; set; } = 0x21; // utf8_general_ci
```

## Testing

Basic tests are provided in `src/SuperSocket.MySQL/Tests/AuthenticationTest.cs`:

```csharp
// Test handshake packet generation
AuthenticationTest.TestHandshakePacket();

// Test password scrambling
AuthenticationTest.TestPasswordScrambling();

// Test OK/ERR packet generation
AuthenticationTest.TestOkErrorPackets();

// Run all tests
AuthenticationTest.RunAllTests();
```

## Limitations

This is a minimal implementation with the following limitations:

1. **Single User**: Only supports one hardcoded username/password combination
2. **No Database Support**: Database name in client response is ignored
3. **Basic Capabilities**: Only implements essential capability flags
4. **No SSL/TLS**: Does not support encrypted connections
5. **No Prepared Statements**: Authentication only, no SQL processing
6. **No Multi-Factor Auth**: Only supports password authentication

## Security Considerations

1. **Password Storage**: Passwords are hardcoded in source code (development only)
2. **Salt Generation**: Uses cryptographically secure random number generator
3. **Protocol Compliance**: Follows MySQL native password authentication standard
4. **Connection Limits**: No built-in connection throttling or rate limiting

## Future Enhancements

Potential improvements for production use:

1. **Database Integration**: Store user credentials in database
2. **Multiple Auth Plugins**: Support for additional authentication methods
3. **SSL/TLS Support**: Encrypted connection support
4. **Configuration**: External configuration for server settings
5. **Logging**: Comprehensive audit logging
6. **Performance**: Connection pooling and optimization
7. **Command Processing**: Full MySQL protocol command handling

## References

- [MySQL Protocol Documentation](https://dev.mysql.com/doc/dev/mysql-server/8.0.11/PAGE_PROTOCOL.html)
- [MySQL Handshake Protocol](https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_connection_phase_packets_protocol_handshake_v10.html)
- [MySQL Authentication](https://dev.mysql.com/doc/dev/mysql-server/8.0.11/page_protocol_connection_phase_packets_protocol_handshake_response.html)
- [SuperSocket Documentation](https://docs.supersocket.net/)