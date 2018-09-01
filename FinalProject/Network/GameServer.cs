using System;
namespace FinalProject
{
	public class GameServer
	{
		private string url;
		private int port;
		private string name;

		public GameServer(string url, int port, string name)
		{
			this.url = url;
			this.port = port;
			this.name = name;
		}

		public string getURL()
		{
			return url;
		}

		public int getPort()
		{
			return port;
		}

		public string getName()
		{
			return name;
		}
	}


}
