using System;
using System.Collections.Generic;
using System.Net;

namespace FinalProject
{
	public class NetworkManager
	{
		private GameServer localServer;
		private GameClient localClient;
		public NetworkManager()
		{
			localServer = new GameServer("127.0.0.1", 8080, "Local Server");
			localClient = new GameClient();
		}

		public List<GameServer> getServers(string restURL)
		{
			List<GameServer> serverList = new List<GameServer>();

			// for now return only local server
			serverList.Add(localServer);
			return serverList;
		}

		public void connectToServer(GameServer server, string password)
		{
			IPAddress address = Dns.GetHostAddresses(server.getURL())[0];
			IPEndPoint endPoint = new IPEndPoint(address, server.getPort());
		}
	}
}
