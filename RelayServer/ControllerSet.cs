using RelayServer.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using static KingdomsSharedCode.Networking.Controller;

namespace RelayServer
{
    public class ControllerSet
    {
        public static Dictionary<byte, Controller> set = new Dictionary<byte, Controller>();
        public static Controller relay = new Controller();

        static ControllerSet()
        {
            set.Add(    (byte)RELAY_HOST_SESSION,         new CON_HostSession());
            set.Add(    (byte)RELAY_JOIN_SESSION,         new CON_JoinSession());
            set.Add(    (byte)RELAY_LEAVE_SESSION,        new CON_LeaveSession());
            set.Add(    (byte)RELAY_HEARTBEAT,            new CON_HeartBeat());
        }
    }
}
