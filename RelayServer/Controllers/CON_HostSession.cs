
using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_HostSession : Controller
    {
        public override void Execute(Relay server, TcpClient client, List<TcpClient> session, Message message)
        {
            if (session != null) throw new Relay.UnexpectedSessionException();

            uint id = server.CreateSession();
            server.AddToSession(id, client);
        }
    }
}
