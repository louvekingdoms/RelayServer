using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.IO;
using static RelayServer.Config;
using System.Threading.Tasks;

using KingdomsSharedCode.Networking;
using KingdomsSharedCode.Generic;

namespace RelayServer
{
    public class Relay
    {
        class BrokenMessageException : Exception { }
        class UnknownSessionException : Exception { public UnknownSessionException(object data) : base(data.ToString()) { } }
        class InvalidSessionException : Exception { }

        public class UnexpectedSessionException : Exception { }
        public class MalformedRequestException : Exception { }
        public class MissingSessionException : Exception { public MissingSessionException(object data) : base(data.ToString()) { } }

        public class Session
        {
            public uint id;

            List<Client> clients = new List<Client>();

            public Session(Client client)
            {
                Add(client);
            }

            public bool Contains(Client client)
            {
                return clients.Contains(client);
            }

            public void Add(Client client)
            {
                clients.Add(client);
                client.session = this;
            }

            public Client[] GetClients()
            {
                return clients.ToArray();
            }

            public void Clean(int time)
            {
                foreach (var client in GetClients())
                    if (client.HasTimedOut(time))
                    {

                        // Logging////////////////////////////////////////////
                        var origin = "<client killed>";
                        try { origin = ((IPEndPoint)client.tcp.RemoteEndPoint).Address.ToString(); }
                        catch (ObjectDisposedException) { }
                        Console.WriteLine("Destroying client " + origin);
                        //////////////////////////////////////////////////////

                        Kill(client);

                    }
            }

            public bool IsEmpty()
            {
                return clients.Count <= 0;
            }

            public void Remove(Client client)
            {
                clients.RemoveAll(o => o == client);
                client.session = null;
            }

            public void Kill(Client client)
            {
                Remove(client);
                client.Die();
            }
        }

        public class Client
        {
            public Socket tcp;
            public Session session;
            public int lastHeartBeat = ((int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()) % int.MaxValue;

            public void Die()
            {
                Console.WriteLine("Client " + tcp + " died");
                tcp.Dispose();
                session = null;
            }

            public bool HasTimedOut(int newTime)
            {
                return (newTime - lastHeartBeat > HEARTBEAT_TIMEOUT);
            }
        }

        Dictionary<uint, Session> sessions = new Dictionary<uint, Session>();
        List<Client> homelessClients = new List<Client>();

        TcpListener listener;
        List<uint> availableSessionIds = new List<uint>();

        public Relay(string address, int port)
        {
            for (uint i = 1; i < MAX_SESSIONS; i++)
                availableSessionIds.Add(i);

            IPAddress localAddress = IPAddress.Parse(address);
            listener = new TcpListener(localAddress, port);

            listener.Start();
            Console.WriteLine("Listening on " + localAddress + ":" + port);

            listener.BeginAcceptSocket(OnClientConnect, null);
        }

        public async Task WaitUntilDeath()
        {
            while (listener != null)
            {
                CleanClients();

                foreach (var sess in sessions.Values.ToList())
                {
                    foreach (var client in sess.GetClients())
                    {
                        ReceiveData(client);
                    }
                }

                foreach (var client in homelessClients)
                    ReceiveData(client);
            }
        }

        void ReceiveData(Client client)
        {
            using (NetworkStream nwStream = client.tcp.NewStream())
            {
                if (!nwStream.CanRead || !nwStream.DataAvailable)
                    return;

                byte[] buffer = new byte[client.tcp.ReceiveBufferSize];
                int bytesRead = nwStream.Read(buffer, 0, client.tcp.ReceiveBufferSize);

                if (bytesRead <= 0)
                    return;


                Message message;
                Session session;

                try
                {
                    message = MakeMessage(buffer, bytesRead);
                }
                catch (BrokenMessageException e) { throw new Exception(e.ToString()); }

                //Console.WriteLine("RAW MESSAGE: " + message);

                try
                {
                    session = FindSession(message.session, client);
                }
                catch (InvalidSessionException e) { throw new Exception(e.ToString()); }
                catch (UnknownSessionException e) { throw new Exception(e.ToString()); }

                // Controller execution
                if (ControllerSet.set.ContainsKey(message.controller))
                {
                    ControllerSet.set[message.controller].Execute(this, client, session, message);
                    LogAction(ControllerSet.set[message.controller], client.tcp, message);
                }
                else
                {
                    ControllerSet.relay.Execute(this, client, session, message);
                    LogAction(this, client.tcp, message);
                }
            }
        }

        void CleanClients()
        {
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()%int.MaxValue;

            foreach (var sess in sessions.Values.ToList())
            {
                sess.Clean((int)time);
                if (sess.IsEmpty())
                {
                    sessions.Remove(sess.id);
                }
            }

            foreach (var client in homelessClients.ToArray())
            {
                if (client.HasTimedOut((int)time))
                {
                    client.Die();
                    homelessClients.Remove(client);
                }
            
            }
        }

        void OnClientConnect(IAsyncResult asyn)
        {

            Socket worker = listener.EndAcceptSocket(asyn);
            
            Console.WriteLine("Received connection from " + ((IPEndPoint)worker.RemoteEndPoint).Address);

            homelessClients.Add(new Client() { tcp = worker });

            listener.BeginAcceptSocket(new AsyncCallback(OnClientConnect), null);
        }

        Session FindSession(uint session, Client worker)
        {
            if (session == 0) return null;

            if (!sessions.ContainsKey(session))
                throw new UnknownSessionException("Client "+worker+" tried to get session "+session+" which does NOT exist");
            
            var verifiedSession = sessions[session];
            if (verifiedSession.Contains(worker))
                return verifiedSession;

            throw new InvalidSessionException();
        }

        Message MakeMessage(byte[] buffer, int bytesRead)
        {

            Message message;
            try
            {
                message = new Message(buffer, bytesRead);
                message.Check();
            }
            catch(BrokenMessageException e)
            {
                throw new BrokenMessageException();
            }

            return message;
        }

        public uint CreateSession(Client client)
        {
            uint id = availableSessionIds.PickRandom();
            availableSessionIds.Remove(id);
            sessions.Add(id, new Session(client));
            return id;
        }

        void ReleaseSession(uint id)
        {

            if (availableSessionIds.Contains(id))
                throw new Exception("Something went very wrong: Released session " + id + ", but it had already been released.");

            sessions.Remove(id);
            availableSessionIds.Add(id);

            Console.WriteLine("Released session " + id.ToString("X"));
        }

        public void AddToSession(uint id, Client client)
        {
            if (sessions[id] == null) throw new UnknownSessionException("Client "+client+" wants to be added to session " + id + " which does NOT exist");
            if (sessions[id].Contains(client)) throw new UnexpectedSessionException(); 

            sessions[id].Add(client);
            homelessClients.Remove(client);
        }

        public void RemoveFromSession(uint id, Client client)
        {
            if (sessions[id] == null) throw new UnknownSessionException("Client " + client + " wants to be removed from session " + id + " which does NOT exist");
            if (!sessions[id].Contains(client)) throw new InvalidSessionException();

            sessions[id].Remove(client);
            homelessClients.Add(client);

            if (sessions[id].IsEmpty())
                ReleaseSession(id);
        }

        void LogAction(object controller, Socket client, Message message)
        {
            var origin = "<client killed>";

            try { origin = ((IPEndPoint)client.RemoteEndPoint).Address.ToString(); }
            catch (ObjectDisposedException) { }

            Console.WriteLine(string.Format("FROM: {4}  [SES {1}]  [BEAT {2}]  {0}> {3}",
                controller.GetType().Name.ToUpper(),
                message.session.ToString("X8"),
                message.beat,
                message.body,
                origin
            ));
        }
    }
}
