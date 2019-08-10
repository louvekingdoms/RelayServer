using System;
using System.Collections.Generic;
using System.Text;

namespace RelayServer
{
    static class Config
    {
        public const int PORT = 5000;
        public const string LOCAL_ADDR = "127.0.0.1";
        public const uint MASTER_KEY = 123456789;
        public const int MAX_SESSIONS = 2000;
        public const int HEARTBEAT_TIMEOUT = 4; // seconds
        public const byte MAXIMUM_BEAT_DIFFERENCE = 2;
    }
}
