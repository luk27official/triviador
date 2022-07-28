﻿using System;
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
		GameInformation? gameInformation;
		string[] questionsABCDWithAnswers;
		string[] questionsNumberWithAnswers;

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
			acceptedClients = new TcpClient[Constants.MAX_PLAYERS];
			acceptedClientsIndex = 0; //holds the number of already connected people
		}

		public void Start()
		{
			try
			{
				questionsABCDWithAnswers = File.ReadAllLines("questionsABCD.txt");
				questionsNumberWithAnswers = File.ReadAllLines("questionsNumber.txt");
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
			catch (Exception) 
			{
				throw;
			}
		}

		private void SaveClient(TcpClient currentClient)
		{
			if (acceptedClientsIndex > Constants.MAX_PLAYERS - 1) //a third user tries to connect, do not do anything
			{
				return;
			}

			lock (acceptedClients) //prevent from more clients accessing the array at once, register them and send the information about connection
			{
				Console.WriteLine("Accepted client " + (acceptedClientsIndex + 1).ToString());
				acceptedClients[acceptedClientsIndex] = currentClient;

				if (acceptedClientsIndex == 0) //inform the first user about their connection
				{
					NetworkStream stream = currentClient.GetStream();
					string message = Constants.P1CONNECTED;
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
			if(acceptedClientsIndex == Constants.MAX_PLAYERS) 
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

		private void InformPlayersAboutConnection() {

			SendMessageToAllClients(Constants.P2CONNECTED);

			Thread.Sleep(1000); //wait 1s so the players load... TODO: consider receiving OK message?

			AssignPlayerIDs();
		}

		private void AssignPlayerIDs()
        {
			int i = 0;
			string[] availableIDs = new string[Constants.MAX_PLAYERS];
			availableIDs[0] = Constants.P1ASSIGN;
			availableIDs[1] = Constants.P2ASSIGN;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					string message = availableIDs[i];
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

			//after this the game should start!
			GameStart();
		}

		private void GameStart()
        {
			gameInformation = new GameInformation();
			//firstly, we have to pick a random region and assign it to the players
			Array values = Enum.GetValues(typeof(Constants.Region));
			Random random = new Random();
			Constants.Region player1Base = (Constants.Region)values.GetValue(random.Next(values.Length));

			Constants.Region player2Base = (Constants.Region)values.GetValue(random.Next(values.Length));
			while(player1Base == player2Base)
            {
				player2Base = (Constants.Region)values.GetValue(random.Next(values.Length)); //pick another one
			}

			gameInformation.setBase(1, player1Base);
			gameInformation.setBase(2, player2Base);

			SendMessageToAllClients(gameInformation.EncodeInformationToString());

			//players now have 3 seconds to get ready for the first question of the 1st round
			Thread.Sleep(3000);

			FirstRound();
		}

		private async void FirstRound()
        {
			int i = 0;

			//send an question to the clients...
			string question = PickRandomNumberQuestion();

			SendMessageToAllClients(question);

			Thread.Sleep(13000);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData = "";
			Int32 bytes;
			Byte[] data = new byte[1024];

			int[] answers = new int[Constants.MAX_PLAYERS];
			int[] times = new int[Constants.MAX_PLAYERS];

			i = 0;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = await stream.ReadAsync(data, 0, data.Length);
					responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine("Received: {0}", responseData);
					string[] splitData = responseData.Split('_');
					answers[i] = Int32.Parse(splitData[2]);
					times[i] = Int32.Parse(splitData[3]);
					i++;
				}
				catch (SocketException e)
				{
					//one of the players disconnected... TODO: resolve what to do
					continue;
				}
			}

			//now it is time to compare the answers...
			//first compare by the actual answers
			int rightAnswer = Int32.Parse(question.Split('_')[2]);

			if (Math.Abs(answers[0] - rightAnswer) < Math.Abs(answers[1] - rightAnswer))
            {
				FirstRoundWin(answers, times, rightAnswer, 1, 2);
				//then this means that the player 1 was closer, thus the winner
			}
			else if (Math.Abs(answers[0] - rightAnswer) == Math.Abs(answers[1] - rightAnswer))
            {
                //this means they were both same - compare by time
                if (times[0] < times[1])
                {
					FirstRoundWin(answers, times, rightAnswer, 1, 2);
				}   //TODO: consider case where equal - very unlikely but still possible?
				else
                {
					FirstRoundWin(answers, times, rightAnswer, 2, 1);
				}
			}
			else //second player was closer
            {
				FirstRoundWin(answers, times, rightAnswer, 2, 1);
			}

		}

		private async void FirstRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID)
        {
			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + "_" + answers[1] + "_" + 
				times[0] + "_" + times[1] + "_" + rightAnswer + "_" + winnerID;
			SendMessageToAllClients(message);

			Thread.Sleep(5000);

			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(loserID);
		}

		private void SendFirstRoundPickAnnouncement(int clientID)
        {
			//let the winner choose twice and the loser once
			string message = Constants.PREFIX_PICKREGION + clientID;
			SendMessageToAllClients(message);

			//wait for the picks
			Thread.Sleep(7000);
			string[] receivedData = new string[Constants.MAX_PLAYERS];
			Int32 bytes;
			Byte[] data = new byte[1024];
			int i = 0;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					receivedData[i] = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine("Received: {0}", receivedData[i]);
					i++;
				}
				catch (SocketException e)
				{
					//one of the players disconnected... TODO: resolve what to do
					continue;
				}
			}
			UpdateGameInformationBasedOnPickFirstRound(receivedData);
			Thread.Sleep(2000);
		}

		private void UpdateGameInformationBasedOnPickFirstRound(string[] data)
        {
			//Received: picked_1_CZST
			foreach(string current in data)
            {
				string[] splitData = current.Split('_');
				if (splitData[2] != "-1")
				{
					int player = Int32.Parse(splitData[1]);
					if (Enum.TryParse(splitData[2], out Constants.Region reg))
					{
						this.gameInformation.addPoints(player, 200);
						this.gameInformation.addRegion(player, reg);
						SendMessageToAllClients(gameInformation.EncodeInformationToString());
						break;
					}
				}
			}
		}

		private void SendMessageToAllClients(string message)
        {
			byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);

			int i = 0;
			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
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

		private string PickRandomABCDQuestion()
        {
			Random rnd = new Random();
			int r = rnd.Next(questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONABCD + questionsABCDWithAnswers[r];
		}

		private string PickRandomNumberQuestion()
        {
			Random rnd = new Random();
			int r = rnd.Next(questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONNUMBER + questionsNumberWithAnswers[r];
		}

		public void Stop()
		{
			server.Stop();
			Running = false;
		}
	}
}
