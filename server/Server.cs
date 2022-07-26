using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
	internal class Server
	{
		bool _running;
		TcpListener server;
		TcpClient[] acceptedClients;
		int acceptedClientsIndex;

		public bool Running
		{
			get
			{
				return _running;
			}
			set
			{
				_running = value;
			}
		}

		public Server()
		{
			Int32 port = 13000;
			IPAddress localAddr = IPAddress.Parse("127.0.0.1");

			server = new TcpListener(localAddr, port);
			acceptedClients = new TcpClient[2];
			acceptedClientsIndex = 0; //holds the number of already connected people
		}

		public void Start()
		{
			try
			{
				server.Start();
				Running = true;
				while (Running)
				{
					var client = server.AcceptTcpClient();

					Task.Run(() => SaveClient(client));
				}
			}
			catch (SocketException)
			{
				throw;
			}
		}

		public void SaveClient(TcpClient currentClient)
		{
			if (acceptedClientsIndex > 1) //a third user tries to connect, do not do anything
			{
				return;
			}

			lock (acceptedClients) //prevent from more clients accessing the array at once, register them and send the information about connection
			{
				Console.WriteLine("accepted client " + (acceptedClientsIndex + 1).ToString());
				acceptedClients[acceptedClientsIndex] = currentClient;

				if (acceptedClientsIndex == 0) //inform the first user about their connection
				{
					NetworkStream stream = currentClient.GetStream();
					string message = "You have been connected, waiting for player 2.";
					byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

					stream.Write(data, 0, data.Length);
					Console.WriteLine("Sent to 1: {0}", message);
					Interlocked.Add(ref acceptedClientsIndex, 1);
					return;
				}

				Interlocked.Add(ref acceptedClientsIndex, 1);
			}

			//TODO: watch out if one player disconnects!

			//otherwise we have already one player connected, so lets
			//inform both users they have been connected
			if(acceptedClientsIndex == 2) 
				InformPlayersAboutConnection();
			/*
			// buffer
			Byte[] bytes = new Byte[256];
			string data = null;

			// Get a stream object for reading and writing
			NetworkStream stream = client.GetStream();

			int i;

			// Loop to receive all the data sent by the client.
			while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
			{
				// Translate data bytes to a ASCII string.
				data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
				Console.WriteLine("Received: {0}", data);

				// Process the data sent by the client.
				data = data.ToUpper();

				byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

				// Send back a response.
				stream.Write(msg, 0, msg.Length);
				Console.WriteLine("Sent: {0}", data);
			}

			Shutdown and end connection
			client.Close();*/
		}

		public void InformPlayersAboutConnection() {
			int i = 0;
			foreach(TcpClient client in acceptedClients)
			{
				try
                {
					NetworkStream stream = client.GetStream();
					string message = "Both players have connected, game starting soon.";
					byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);

					stream.Write(msg, 0, msg.Length);
					Console.WriteLine("Sent to {0}: {1}", i + 1, message);

					i++;
				}
				catch (SocketException e)
                {
					//one of the players disconnected... TODO: resolve what to do
					continue;
                }
			}
		}

		public void Stop()
		{
			server.Stop();
			Running = false;
		}
	}
}
