﻿using System;
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
using static KingdomsSharedCode.Generic.Logger;
using System.Threading;

namespace RelayServer
{
    public class Relay
    {
        class BrokenMessageException : Exception { }

        public class UnknownSessionException : Exception { public UnknownSessionException(object data) : base(data.ToString()) { } }
        public class InvalidSessionException : Exception { }
        public class UnexpectedSessionException : Exception { }
        public class MalformedRequestException : Exception { }
        public class MissingSessionException : Exception { public MissingSessionException(object data) : base(data.ToString()) { } }

        public bool directMode = false;

        public static Logger logger;

        Dictionary<uint, Session> sessions = new Dictionary<uint, Session>();
        List<Client> clients = new List<Client>();

        TcpListener listener;
        List<uint> availableSessionIds = new List<uint>();

        bool shouldRun = true;
        
        public Relay(string address, int port, bool directMode)
        {
            for (uint i = 1; i < MAX_SESSIONS; i++)
                availableSessionIds.Add(i);

            if (directMode)
            {
                availableSessionIds = new List<uint>() { 1 };
                this.directMode = directMode;
            }

            IPAddress localAddress = IPAddress.Parse(address);
            listener = new TcpListener(localAddress, port);

            listener.Start();

            logger.Info("Listening on " + localAddress + ":" + port);

            listener.BeginAcceptSocket(OnClientConnect, null);
        }

        public async Task WaitUntilDeath()
        {
            while (shouldRun)
            {
                CleanClients();
                await Task.Delay(1000);
                if (directMode && clients.Count < 1)
                {
                    // Headless server
                    logger.Info("Relay is in directMode and headless. Exiting.");
                    break;
                }
            }
        }

        void ReceiveMessage(Client client, Message message)
        {
            Session session;

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
            }
            else
            {
                ControllerSet.relay.Execute(this, client, session, message);
            }
        }

        void CleanClients()
        {
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()%int.MaxValue;
            logger.Trace(string.Format("Preparing to clean {0} clients", clients.Count));

            foreach (var client in clients.ToArray())
            {
                if (client.HasTimedOut((int)time))
                {
                    try
                    {
                        if (client.session != null)
                        {
                            logger.Debug("Destroying client " + client + " from session "+client.session);
                            RemoveFromSession(client.session.id, client);
                        }

                        client.Die();
                    }
                    catch { }
                    clients.Remove(client);
                    logger.Debug("Destroyed client " + client + " (time out)");
                }
            
            }
        }

        void OnClientConnect(IAsyncResult asyn)
        {

            Socket worker = listener.EndAcceptSocket(asyn);

            logger.Debug("Accepted connection from " + ((IPEndPoint)worker.RemoteEndPoint).Address);

            var client = new Client(worker);
            clients.Add(client);
            client.OnMessageReception += (msg) => { ReceiveMessage(client, msg); };

            listener.BeginAcceptSocket(new AsyncCallback(OnClientConnect), null);
        }

        public Session GetSession(uint session)
        {
            if (session == 0) return null;

            if (!sessions.ContainsKey(session))
                throw new UnknownSessionException("Tried to get session " + session.ToString("X2") + " which does NOT exist");

            return sessions[session];
        }


        Session FindSession(uint session, Client worker)
        {
            if (session == 0) return null;

            if (!sessions.ContainsKey(session))
                throw new UnknownSessionException("Client "+worker+" tried to get session "+session.ToString("X2")+" which does NOT exist");
            
            var verifiedSession = sessions[session];
            if (verifiedSession.Contains(worker))
                return verifiedSession;

            throw new InvalidSessionException();
        }

        public uint CreateSession(Client client)
        {
            uint id = availableSessionIds.PickRandom();
            availableSessionIds.Remove(id);

            sessions.Add(id, new Session(id, client));

            logger.Debug("Mobilized session " + id.ToString("X"));
            return id;
        }

        void ReleaseSession(uint id)
        {

            if (availableSessionIds.Contains(id))
                throw new Exception("Something went very wrong: Released session " + id.ToString("X2") + ", but it had already been released.");

            sessions.Remove(id);
            availableSessionIds.Add(id);

            logger.Debug("Released session " + id.ToString("X"));
        }

        public void AddToSession(uint id, Client client)
        {
            if (sessions[id] == null) throw new UnknownSessionException("Client "+client+" wants to be added to session " + id.ToString("X2") + " which does NOT exist");
            if (sessions[id].Contains(client)) throw new InvalidSessionException(); 

            sessions[id].Add(client);
        }

        public void RemoveFromSession(uint id, Client client)
        {
            if (sessions[id] == null) throw new UnknownSessionException("Client " + client + " wants to be removed from session " + id.ToString("X2") + " which does NOT exist");
            if (!sessions[id].Contains(client)) throw new InvalidSessionException();

            sessions[id].Remove(client);
            logger.Trace("Removed client from " + sessions[id] + ", remaining " + sessions[id].GetClients().Count() + " clients");

            if (sessions[id].IsEmpty())
                ReleaseSession(id);
        }

        public void Kill()
        {
            logger.Info("Termination request for Relay server, killing all clients.");
            shouldRun = false;
            foreach(var client in clients)
            {
                client.thread.Abort();
            }
        }
    }
}
