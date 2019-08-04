﻿using KingdomsSharedCode.Networking;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RelayServer.Controllers
{
    class CON_JoinSession : Controller
    {
        public override void Execute(Relay server, TcpClient client, List<TcpClient> session, Message message)
        {
            if (session != null) throw new Relay.UnexpectedSessionException();

            server.AddToSession(message.session, client);
        }
    }
}