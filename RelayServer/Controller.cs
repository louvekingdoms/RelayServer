
using KingdomsSharedCode.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RelayServer
{
    public class Controller
    {
        public virtual void Execute(Relay server, TcpClient client, List<TcpClient> session, Message message) {
            
            if (session == null) throw new Relay.MissingSessionException();

            foreach (var cl in session)
                cl.GetStream().Write(message);
        }

    }
}
