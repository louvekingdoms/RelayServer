
using KingdomsSharedCode.Networking;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_HeartBeat : Controller
    {
        public override void Execute(Relay server, Client client, Session session, Message message)
        {
            client.lastHeartBeat = ((int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()) % int.MaxValue;
        }
    }
}
