# MySQL Handshake Tests

This directory contains comprehensive unit and integration tests for the MySQL handshake process.

## Test Categories

### 1. Unit Tests (`MainTest.cs`)
- Basic connection parameter validation
- Constructor argument validation
- Authentication state management
- Error handling scenarios

### 2. Packet Tests (`HandshakeTest.cs`)
- Handshake packet decoding/encoding
- OK packet parsing
- Error packet parsing
- Client capability flags
- Binary protocol validation

### 3. Integration Tests (`MySQLIntegrationTest.cs`)
- Full handshake flow with real MySQL server
- Authentication success/failure scenarios
- Concurrent connections
- Reconnection handling
- Timeout scenarios

## Configuration

### Environment Variables

The integration tests can be configured using environment variables:

```bash
export MYSQL_HOST=localhost
export MYSQL_PORT=3306
export MYSQL_USERNAME=root
export MYSQL_PASSWORD=password
export MYSQL_DATABASE=test
```

### Default Values

If environment variables are not set, the tests use these defaults:
- Host: `localhost`
- Port: `3306`
- Username: `root`
- Password: `password`
- Database: `test`

## Running Tests

### All Tests
```bash
dotnet test
```

### Unit Tests Only
```bash
dotnet test --filter "Category!=Integration"
```

### Integration Tests Only
```bash
dotnet test --filter "Category=Integration"
```

### Specific Test Class
```bash
dotnet test --filter "ClassName=HandshakeTest"
```

## CI/CD Pipeline Setup

For automated testing with MySQL in your pipeline:

### Docker Compose Example
```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: password
      MYSQL_DATABASE: test
    ports:
      - "3306:3306"
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      timeout: 20s
      retries: 10
```

### GitHub Actions Example
```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: password
          MYSQL_DATABASE: test
        ports:
          - 3306:3306
        options: --health-cmd="mysqladmin ping" --health-interval=10s --health-timeout=5s --health-retries=3
    
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '6.0.x'
      - name: Run tests
        run: dotnet test
        env:
          MYSQL_HOST: localhost
          MYSQL_PORT: 3306
          MYSQL_USERNAME: root
          MYSQL_PASSWORD: password
          MYSQL_DATABASE: test
```

## Test Coverage

The tests cover:

### Handshake Protocol
- ✅ Server handshake packet reception
- ✅ Client handshake response generation
- ✅ Authentication response calculation (SHA1 + salt)
- ✅ OK packet handling (authentication success)
- ✅ Error packet handling (authentication failure)

### Connection Management
- ✅ Connection state tracking
- ✅ Multiple concurrent connections
- ✅ Connection cleanup and disconnection
- ✅ Reconnection scenarios

### Error Scenarios
- ✅ Invalid credentials
- ✅ Invalid host/port
- ✅ Network timeouts
- ✅ Missing MySQL server

### Edge Cases
- ✅ Empty passwords
- ✅ Special characters in passwords
- ✅ Long usernames/passwords
- ✅ Connection parameter validation

## Troubleshooting

### Common Issues

1. **MySQL Connection Refused**
   - Ensure MySQL server is running
   - Check host and port configuration
   - Verify firewall settings

2. **Authentication Failed**
   - Verify username/password
   - Check MySQL user permissions
   - Ensure MySQL authentication plugin compatibility

3. **Tests Skipped**
   - Integration tests are automatically skipped if MySQL is not available
   - Unit tests should always run regardless of MySQL availability

### Debug Information

Enable verbose logging by setting:
```bash
export MYSQL_DEBUG=true
```

### Test Data Cleanup

Tests automatically clean up connections, but if you need to reset:
```sql
-- Connect to MySQL and run:
SHOW PROCESSLIST;
KILL <connection_id>; -- for any stuck connections
```
