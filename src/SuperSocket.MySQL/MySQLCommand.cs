using System;

namespace SuperSocket.MySQL
{
    /// <summary>
    /// MySQL command types.
    /// Reference: https://dev.mysql.com/doc/internals/en/command-phase.html
    /// </summary>
    internal enum MySQLCommand : byte
    {
        COM_SLEEP = 0x00,
        COM_QUIT = 0x01,
        COM_INIT_DB = 0x02,
        COM_QUERY = 0x03,
        COM_FIELD_LIST = 0x04,
        COM_CREATE_DB = 0x05,
        COM_DROP_DB = 0x06,
        COM_REFRESH = 0x07,
        COM_SHUTDOWN = 0x08,
        COM_STATISTICS = 0x09,
        COM_PROCESS_INFO = 0x0A,
        COM_CONNECT = 0x0B,
        COM_PROCESS_KILL = 0x0C,
        COM_DEBUG = 0x0D,
        COM_PING = 0x0E,
        COM_TIME = 0x0F,
        COM_DELAYED_INSERT = 0x10,
        COM_CHANGE_USER = 0x11,
        COM_BINLOG_DUMP = 0x12,
        COM_TABLE_DUMP = 0x13,
        COM_CONNECT_OUT = 0x14,
        COM_REGISTER_SLAVE = 0x15,
        COM_STMT_PREPARE = 0x16,
        COM_STMT_EXECUTE = 0x17,
        COM_STMT_SEND_LONG_DATA = 0x18,
        COM_STMT_CLOSE = 0x19,
        COM_STMT_RESET = 0x1A,
        COM_SET_OPTION = 0x1B,
        COM_STMT_FETCH = 0x1C,
        COM_DAEMON = 0x1D,
        COM_BINLOG_DUMP_GTID = 0x1E,
        COM_RESET_CONNECTION = 0x1F
    }
}