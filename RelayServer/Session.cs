using System;
using System.Collections.Generic;
using System.Text;

using static KingdomsSharedCode.Generic.Logger;

namespace RelayServer
{
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

        public void Clean(int time)
        {
            foreach (var client in clients.ToArray())
                if (client.HasTimedOut(time))
                {
                    Debug("Killing sessionized client " + client);
                    Kill(client);
                }
        }

        public Client[] GetClients()
        {
            return clients.ToArray();
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
}
