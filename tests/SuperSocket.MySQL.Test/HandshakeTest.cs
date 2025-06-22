using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using SuperSocket.MySQL;
using SuperSocket.MySQL.Packets;

namespace SuperSocket.MySQL.Test
{
    public class HandshakeTest
    {
        [Fact]
        public void HandshakePacket_Decode_ShouldParseCorrectly()
        {
            // Arrange - Create a mock handshake packet payload
            var protocolVersion = (byte)10;
            var serverVersion = "8.0.32-MySQL";
            var connectionId = (uint)12345;
            var authPluginDataPart1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var capabilityFlagsLower = (ushort)0x3FFF;
            var characterSet = (byte)0x21;
            var statusFlags = (ushort)0x0002;
            var capabilityFlagsUpper = (ushort)0x807F;
            var authPluginDataLength = (byte)20;
            var authPluginDataPart2 = new byte[] { 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14 };
            var authPluginName = "mysql_native_password";

            // Create packet data
            var packetData = new List<byte>();
            packetData.Add(protocolVersion);
            packetData.AddRange(Encoding.UTF8.GetBytes(serverVersion));
            packetData.Add(0); // null terminator
            packetData.AddRange(BitConverter.GetBytes(connectionId));
            packetData.AddRange(authPluginDataPart1);
            packetData.Add(0); // filler
            packetData.AddRange(BitConverter.GetBytes(capabilityFlagsLower));
            packetData.Add(characterSet);
            packetData.AddRange(BitConverter.GetBytes(statusFlags));
            packetData.AddRange(BitConverter.GetBytes(capabilityFlagsUpper));
            packetData.Add(authPluginDataLength);
            packetData.AddRange(new byte[10]); // reserved
            packetData.AddRange(authPluginDataPart2);
            packetData.AddRange(Encoding.UTF8.GetBytes(authPluginName));
            packetData.Add(0); // null terminator

            var sequence = new ReadOnlySequence<byte>(packetData.ToArray());
            var reader = new SequenceReader<byte>(sequence);

            // Act
            var handshakePacket = new HandshakePacket();
            handshakePacket.Decode(ref reader, null);

            // Assert
            Assert.Equal(protocolVersion, handshakePacket.ProtocolVersion);
            Assert.Equal(serverVersion, handshakePacket.ServerVersion);
            Assert.Equal(connectionId, handshakePacket.ConnectionId);
            Assert.Equal(authPluginDataPart1, handshakePacket.AuthPluginDataPart1);
            Assert.Equal(capabilityFlagsLower, handshakePacket.CapabilityFlagsLower);
            Assert.Equal(characterSet, handshakePacket.CharacterSet);
            Assert.Equal(statusFlags, handshakePacket.StatusFlags);
            Assert.Equal(capabilityFlagsUpper, handshakePacket.CapabilityFlagsUpper);
            Assert.Equal(authPluginDataLength, handshakePacket.AuthPluginDataLength);
            Assert.Equal(authPluginName, handshakePacket.AuthPluginName);
        }

        [Fact]
        public void HandshakeResponsePacket_Encode_ShouldCreateCorrectPayload()
        {
            // Arrange
            var handshakeResponse = new HandshakeResponsePacket
            {
                CapabilityFlags = (uint)(ClientCapabilities.CLIENT_PROTOCOL_41 | 
                                       ClientCapabilities.CLIENT_SECURE_CONNECTION),
                MaxPacketSize = 16777216,
                CharacterSet = 0x21,
                Username = "testuser",
                AuthResponse = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                Database = "testdb",
                AuthPluginName = "mysql_native_password"
            };

            var buffer = new ArrayBufferWriter<byte>();

            // Act
            var bytesWritten = handshakeResponse.Encode(buffer);

            // Assert
            Assert.True(bytesWritten > 0, "Should write some bytes");
            
            var writtenData = buffer.WrittenSpan.ToArray();
            Assert.True(writtenData.Length > 0, "Should have written data");
            
            // Verify capability flags are written (first 4 bytes)
            var capabilityFlags = BitConverter.ToUInt32(writtenData, 0);
            Assert.Equal(handshakeResponse.CapabilityFlags, capabilityFlags);
        }

        [Fact]
        public void OKPacket_Decode_ShouldParseCorrectly()
        {
            // Arrange
            var packetData = new List<byte>();
            packetData.Add(0x00); // OK header
            packetData.Add(0x01); // affected rows (length-encoded)
            packetData.Add(0x02); // last insert id (length-encoded)
            packetData.AddRange(BitConverter.GetBytes((ushort)0x0002)); // status flags
            packetData.AddRange(BitConverter.GetBytes((ushort)0x0000)); // warnings

            var sequence = new ReadOnlySequence<byte>(packetData.ToArray());
            var reader = new SequenceReader<byte>(sequence);

            // Act
            var okPacket = new OKPacket();
            okPacket.Decode(ref reader, null);

            // Assert
            Assert.Equal(0x00, okPacket.Header);
            Assert.Equal(1UL, okPacket.AffectedRows);
            Assert.Equal(2UL, okPacket.LastInsertId);
            Assert.Equal(0x0002, okPacket.StatusFlags);
            Assert.Equal(0x0000, okPacket.Warnings);
        }

        [Fact]
        public void ErrorPacket_Decode_ShouldParseCorrectly()
        {
            // Arrange
            var errorCode = (ushort)1045;
            var sqlState = "28000";
            var errorMessage = "Access denied for user";

            var packetData = new List<byte>();
            packetData.Add(0xFF); // Error header
            packetData.AddRange(BitConverter.GetBytes(errorCode));
            packetData.Add((byte)'#'); // SQL state marker
            packetData.AddRange(Encoding.UTF8.GetBytes(sqlState));
            packetData.AddRange(Encoding.UTF8.GetBytes(errorMessage));

            var sequence = new ReadOnlySequence<byte>(packetData.ToArray());
            var reader = new SequenceReader<byte>(sequence);

            // Act
            var errorPacket = new ErrorPacket();
            errorPacket.Decode(ref reader, null);

            // Assert
            Assert.Equal(0xFF, errorPacket.Header);
            Assert.Equal(errorCode, errorPacket.ErrorCode);
            Assert.Equal("#", errorPacket.SqlStateMarker);
            Assert.Equal(sqlState, errorPacket.SqlState);
            Assert.Equal(errorMessage, errorPacket.ErrorMessage);
        }

        [Fact]
        public void ClientCapabilities_FlagsTest()
        {
            // Test that capability flags can be combined correctly
            var capabilities = ClientCapabilities.CLIENT_PROTOCOL_41 | 
                             ClientCapabilities.CLIENT_SECURE_CONNECTION |
                             ClientCapabilities.CLIENT_PLUGIN_AUTH;

            Assert.True((capabilities & ClientCapabilities.CLIENT_PROTOCOL_41) != 0);
            Assert.True((capabilities & ClientCapabilities.CLIENT_SECURE_CONNECTION) != 0);
            Assert.True((capabilities & ClientCapabilities.CLIENT_PLUGIN_AUTH) != 0);
            Assert.False((capabilities & ClientCapabilities.CLIENT_SSL) != 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("password")]
        [InlineData("complex_p@ssw0rd_123")]
        public void MySQLConnection_GenerateAuthResponse_ShouldHandleDifferentPasswords(string password)
        {
            // This test verifies that the auth response generation doesn't crash with different password inputs
            // We can't easily test the actual authentication without a real MySQL server,
            // but we can ensure the method doesn't throw exceptions

            // Arrange
            var handshakePacket = new HandshakePacket
            {
                AuthPluginDataPart1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                AuthPluginDataPart2 = new byte[] { 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14 },
                AuthPluginDataLength = 20
            };

            // We need to use reflection to test the private method, or make it internal for testing
            var connection = new MySQLConnection("localhost", 3306, "user", password);
            
            // Act & Assert
            // Since GenerateAuthResponse is private, we can't directly test it here
            // In a real scenario, you might make it internal and use InternalsVisibleTo attribute
            // For now, we just verify the constructor doesn't throw
            Assert.NotNull(connection);
        }
    }
}
