using RelayServer.Controllers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelayServer
{
    public class ControllerSet
    {
        public static Dictionary<byte, Controller> set = new Dictionary<byte, Controller>();
        public static Controller relay = new Controller();

        public ControllerSet()
        {
            set.Add(    0,      new CON_HostSession());
            set.Add(    1,      new CON_JoinSession());
            set.Add(    2,      new CON_LeaveSession());
        }
    }
}
