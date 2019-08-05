
using KingdomsSharedCode.Networking;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_LeaveSession : Controller
    {
        public override void Execute(Relay server, Client client, Session session, Message message)
        {
            if (session == null) throw new Relay.MissingSessionException(message);

            server.RemoveFromSession(message.session, client);
        }
    }
}
