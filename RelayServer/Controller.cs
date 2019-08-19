
using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using static KingdomsSharedCode.Generic.Logger;
using static RelayServer.Relay;

namespace RelayServer
{
    public class Controller
    {
        // Default behavior is relaying thing to everyone
        public virtual void Execute(Relay server, Client client, Session session, Message message) {
            
            if (session == null) throw new Relay.MissingSessionException(message);

            foreach (var cl in session.GetClients())
                using (var stream = cl.tcp.NewStream())
                {
                    logger.Trace("Broadcasting " + message);
                    stream.Write(message);
                }
        }

    }
}
