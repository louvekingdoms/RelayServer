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
        class UnknownSessionException : Exception { }
        class InvalidSessionException : Exception { }

        public class UnexpectedSessionException : Exception { }
        public class MalformedRequestException : Exception { }
        public class MissingSessionException : Exception { }

        Dictionary<uint, List<TcpClient>> sessions = new Dictionary<uint, List<TcpClient>>();
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

            listener.BeginAcceptTcpClient(OnClientConnect, null);
        }

        public async Task WaitUntilDeath()
        {
            while (listener != null)
            {
                await Task.Delay(1000);
            }
        }

        public void OnClientConnect(IAsyncResult asyn)
        {

            TcpClient worker = listener.EndAcceptTcpClient(asyn);
            listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
            
            //---get the incoming data through a network stream---
            NetworkStream nwStream = worker.GetStream();
            byte[] buffer = new byte[worker.ReceiveBufferSize];

            //---read incoming stream---
            int bytesRead = nwStream.Read(buffer, 0, worker.ReceiveBufferSize);

            Message message;
            List<TcpClient> session;

            try {
                message = MakeMessage(buffer, bytesRead);
            }
            catch (BrokenMessageException e) { throw new Exception(e.ToString()); }

            try
            {
                session = FindSession(message, worker);
            }
            catch (InvalidSessionException e) { throw new Exception(e.ToString()); }
            catch (UnknownSessionException e) { throw new Exception(e.ToString()); }

            // Controller execution
            try
            {
                ControllerSet.set[message.controller].Execute(this, worker, session, message);
                LogAction(ControllerSet.set[message.controller], worker, message);
            }
            catch (KeyNotFoundException e) {
                ControllerSet.relay.Execute(this, worker, session, message);
                LogAction(this, worker, message);
            }
        }

        List<TcpClient> FindSession(Message message, TcpClient worker)
        {
            if (message.session == 0) return null;

            if (!sessions.ContainsKey(message.session))
                throw new UnknownSessionException();
            
            var session = sessions[message.session];
            if (session.Contains(worker)) return session;

            throw new InvalidSessionException();
        }

        Message MakeMessage(byte[] buffer, int bytesRead)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(buffer));
            byte controller = reader.ReadByte();
            byte beat = reader.ReadByte();
            uint secret = reader.ReadUInt32();
            uint session = reader.ReadUInt32();
            string body = string.Empty;

            int position = (int)reader.BaseStream.Position;
            if (position < bytesRead)
            {
                body = Encoding.UTF8.GetString(buffer, position, bytesRead - position);
            }

            Message message;
            try
            {
                message = new Message()
                {
                    controller = controller,
                    secret = secret,
                    session = session,
                    body = body
                };
                message.Check();
            }
            catch(BrokenMessageException e)
            {
                throw new BrokenMessageException();
            }

            return message;
        }

        public uint CreateSession()
        {
            uint id = availableSessionIds.PickRandom();
            sessions.Add(id, new List<TcpClient>());
            return id;
        }

        void ReleaseSession(uint id)
        {
            if (availableSessionIds.Contains(id))
                throw new Exception("Something went very wrong: Released session " + id + ", but it had already been released.");

            sessions[id] = null;
            availableSessionIds.Add(id);
        }

        public void AddToSession(uint id, TcpClient worker)
        {
            if (sessions[id] == null) throw new UnknownSessionException();
            if (sessions[id].Contains(worker)) throw new UnexpectedSessionException(); 

            sessions[id].Add(worker);
        }

        public void RemoveFromSession(uint id, TcpClient worker)
        {
            if (sessions[id] == null) throw new UnknownSessionException();
            if (!sessions[id].Contains(worker)) throw new InvalidSessionException();

            sessions[id].RemoveAll(o=>o==worker);

            if (sessions[id].Count <= 0)
                ReleaseSession(id);
        }

        void LogAction(object controller, TcpClient client, Message message)
        {
            Console.Write(string.Format("FROM: {4}  [SES {1}]  [BEAT {2}]  {0}> {3}",
                controller.GetType().Name.ToUpper(),
                message.session,
                message.beat,
                message.body,
                ((IPEndPoint)client.Client.RemoteEndPoint).Address
            ));
        }
    }
}
