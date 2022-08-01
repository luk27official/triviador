using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
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
		GameInformation gameInformation;
		string[] questionsABCDWithAnswers;
		string[] questionsNumberWithAnswers;
		Stopwatch sw;

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
			gameInformation = new GameInformation();
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
			
		}

		private void InformPlayersAboutConnection() {

			SendMessageToAllClients(Constants.P2CONNECTED);

			Thread.Sleep(Constants.DELAY_FASTUPDATE_MS); //wait 1s so the players load... TODO: consider receiving OK message?

			AssignPlayerIDs();
			//after this the game should start!
			GameStart();
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
		}

		private void AssignBaseRegions()
        {
			//firstly, we have to pick a random region and assign it to the players
			Array values = Enum.GetValues(typeof(Constants.Region));
			Random random = new Random();
			Constants.Region player1Base = (Constants.Region)values.GetValue(random.Next(values.Length));

			Constants.Region player2Base = (Constants.Region)values.GetValue(random.Next(values.Length));
			while (player1Base == player2Base || Constants.DoRegionsNeighbor(player1Base, new List<Constants.Region>() { player2Base }))
			{
				player2Base = (Constants.Region)values.GetValue(random.Next(values.Length)); //pick another one
			}

			gameInformation.setBase(1, player1Base);
			gameInformation.setBase(2, player2Base);

			SendMessageToAllClients(gameInformation.EncodeInformationToString());
		}

		private void CreateTimedEvent(int countdownMs, System.Timers.ElapsedEventHandler e)
        {
			System.Timers.Timer timer = new System.Timers.Timer(countdownMs);

			timer.Enabled = true;
			timer.Elapsed += e;
			timer.AutoReset = false;
		}

		private void GameStart()
        {
			AssignBaseRegions();

			sw = new();
			sw.Start();

			//players now have 3 seconds to get ready for the first question of the 1st round
			/*Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			FirstRound();
			Console.WriteLine("done r1");

			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			FirstRound();
			Console.WriteLine("done r2");

			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			FirstRound();
			Console.WriteLine("done r3");

			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			FirstRound();
			Console.WriteLine("done r4");
			*/
			for(int i = 0; i < 4; i++)
            {
				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				FirstRound();
			}

			for(int i = 0; i < 2; i++)
            {
				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				SecondRound(1, 2, false);

				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				SecondRound(2, 1, false);

				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				SecondRound(2, 1, false);

				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				SecondRound(1, 2, false);
			}
			//noone has won till now -> decide the winner based on points
			DecideWinnerBasedOnPoints();


			//SecondRound(2);

			/*
			//first round
			CreateTimedEvent(1, FirstRound);

			//50s is the length of the first round
			CreateTimedEvent(Constants.LENGTH_FIRSTROUND_TOTAL, FirstRound);

			//100s for the second one
			CreateTimedEvent(Constants.LENGTH_FIRSTROUND_TOTAL*2, FirstRound);

			//150s for the third one
			CreateTimedEvent(Constants.LENGTH_FIRSTROUND_TOTAL*3, FirstRound);

			//200s for the second round
			CreateTimedEvent(Constants.LENGTH_FIRSTROUND_TOTAL * 4, SecondRound);
			*/

			//CreateTimedEvent(1, SecondRound);
		}

		private void DecideWinnerBasedOnPoints()
        {
			string message = Constants.PREFIX_GAMEOVER;

            if (gameInformation.Points[0] > gameInformation.Points[1])
            {
				message += "1";
            }
			else if (gameInformation.Points[0] == gameInformation.Points[1])
            {
				message += "-1"; //tie
            }
			else
            {
				message += "2";
            }
			SendMessageToAllClients(message);
        }

		private Constants.Region PicksSecondRound(int clientID)
        {
			string message = Constants.PREFIX_PICKREGION + clientID.ToString();
			SendMessageToAllClients(message);

			//wait for the picks
			Thread.Sleep(Constants.DELAY_FIRSTROUND_PICKS);
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

			//now we received the info that the player picked some region to attack, lets store it
			Constants.Region attackedRegion = GetPickedRegionRoundTwo(receivedData);
			//the return value should be never null!
			string attackMessage = Constants.PREFIX_ATTACK + attackedRegion.ToString();
			SendMessageToAllClients(attackMessage);
			return attackedRegion;
		}

		private void SecondRound(int attackerID, int otherID, bool repeatedBaseAttack)
        {
			Constants.Region reg;
			if (repeatedBaseAttack)
            {
				reg = this.gameInformation.Bases[otherID - 1];
            }
			else
            {
				//wait for the picks and send an message which region is being attacked
				reg = PicksSecondRound(attackerID);
			}

			//wait 3s and then send the question
			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			string question = PickRandomABCDQuestion();
			SendMessageToAllClients(question);

			//now receive the answers
			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData;
			Int32 bytes;
			Byte[] data = new byte[1024];

			string[] answers = new string[Constants.MAX_PLAYERS];
			int i = 0;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine("Received: {0}", responseData);
					string[] splitData = responseData.Split('_');
					answers[i] = splitData[2];
					i++;
				}
				catch (SocketException e)
				{
					//one of the players disconnected... TODO: resolve what to do
					continue;
				}
			}

			//now it is time to send back answers...
			DecideSecondRoundWinnerFirstQuestion(answers, question, attackerID, otherID, reg);
		}

		private void DecideSecondRoundWinnerFirstQuestion(string[] answers, string question, int attackerID, int defenderID, Constants.Region attackedRegion)
        {
			//finalanswers_correctANS_P1ANS_P2ANS
			string[] splitQuestion = question.Split('_');
			string correct = splitQuestion[2];
			string p1Answer = answers[0];
			string p2Answer = answers[1];

			SendMessageToAllClients(Constants.PREFIX_FINALANSWERS + correct + '_' + p1Answer + '_' + p2Answer);

			//we've just sent the information about the correct answers...
			//we have to calculate the actual winner

			Thread.Sleep(Constants.DELAY_SHOWANSWERS);


			//here the client can receive 3 types of answers!!
			//1) end game
			//2) game info update
			//3) new question (num/ABCD)

			if(correct == answers[attackerID - 1] && correct != answers[defenderID - 1])
            {
				SecondRoundAttackerWin(attackerID, defenderID, attackedRegion);
			}
			else if(correct == answers[attackerID - 1] && correct == answers[defenderID - 1])
            {
				//both answered correctly
				//show a number question
				SecondRoundAnotherQuestion(attackerID, defenderID, attackedRegion);
            }
			else if (correct == answers[defenderID - 1] && correct != answers[attackerID - 1])
            {
				SecondRoundDefenderWin(defenderID);
			}
			else
            {
				//no one answered correctly
				//do nothing lol
				SecondRoundNoOneAnsweredCorrectly();
			}
		}

		private void SecondRoundAttackerWin(int attackerID, int defenderID, Constants.Region attackedRegion)
        {
			//only the attacker answered correctly
			//either way change the points + owner of the region or subtract 1HP from the base
			//and dont forget to add it to the high value regions
			if (attackedRegion == this.gameInformation.Bases[defenderID - 1])
			{
				this.gameInformation.decreaseBaseHealth(defenderID);
				if (this.gameInformation.BaseHealths[defenderID - 1] == 0)
				{
					SendMessageToAllClients(GameOverMessage(attackerID));
				}
				SecondRound(attackerID, defenderID, true);
			}
			else
			{
				if (this.gameInformation.HighValueRegions.Contains(attackedRegion))
				{
					this.gameInformation.addPoints(defenderID, -400);
				}
				else
				{
					this.gameInformation.addPoints(defenderID, -200);
				}
				this.gameInformation.addPoints(attackerID, 400);
				this.gameInformation.addRegion(attackerID, attackedRegion);
				this.gameInformation.removeRegion(defenderID, attackedRegion);
				this.gameInformation.addHighValueRegion(attackedRegion);
				SendMessageToAllClients(gameInformation.EncodeInformationToString());
			}
		}

		private void SecondRoundDefenderWin(int defenderID)
        {
			//defender answered ok
			//just update points (+100) def
			this.gameInformation.addPoints(defenderID, 100);
			SendMessageToAllClients(gameInformation.EncodeInformationToString());
		}

		private void SecondRoundNoOneAnsweredCorrectly()
        {
			SendMessageToAllClients(gameInformation.EncodeInformationToString());
		}

		private void SecondRoundAnotherQuestion(int attackerID, int defenderID, Constants.Region attackedRegion)
        {
			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			string question = PickRandomNumberQuestion();
			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData;
			Int32 bytes;
			Byte[] data = new byte[1024];

			int[] answers = new int[Constants.MAX_PLAYERS];
			int[] times = new int[Constants.MAX_PLAYERS];

			int i = 0;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
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
			DecideNumberQuestionWinnerAndInform(answers, times, question, SecondRoundWin, attackedRegion, defenderID, attackerID);
		}

		private void SecondRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID, Constants.Region? attackedRegion, int? defenderID, int? attackerID)
		{
			if(attackedRegion == null || defenderID == null || attackerID == null) return;
			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + "_" + answers[1] + "_" +
				times[0] + "_" + times[1] + "_" + rightAnswer + "_" + winnerID;
			SendMessageToAllClients(message);


			Thread.Sleep(Constants.DELAY_SHOWANSWERS);

			if(winnerID == attackerID)
            {
				SecondRoundAttackerWin((int)attackerID, (int)defenderID, (Constants.Region)attackedRegion);
			}
			else
            {
				SecondRoundDefenderWin((int)defenderID);
            }
		}

		private Constants.Region GetPickedRegionRoundTwo(string[] data)
        {
			//Received: picked_1_CZST
			foreach (string current in data)
			{
				string[] splitData = current.Split('_');
				if (splitData[2] != "-1")
				{
					int player = Int32.Parse(splitData[1]);
					if (Enum.TryParse(splitData[2], out Constants.Region reg))
					{
						return reg;
					}
				}
			}

			return Constants.Region.CZZL; //WILL NOT HAPPEN!
		}

		private void FirstRound(object? sender, System.Timers.ElapsedEventArgs e)
		{
			FirstRound();
        }

		private void DecideNumberQuestionWinnerAndInform(int[] answers, int[] times, string question, 
			Action<int[], int[], int, int, int, Constants.Region?, int?, int?> decideWin, 
			Constants.Region? region, int? defenderID, int? attackerID)
        {
			int rightAnswer = Int32.Parse(question.Split('_')[2]);

			if (Math.Abs(answers[0] - rightAnswer) < Math.Abs(answers[1] - rightAnswer))
			{
				decideWin(answers, times, rightAnswer, 1, 2, region, defenderID, attackerID);
				//then this means that the player 1 was closer, thus the winner
			}
			else if (Math.Abs(answers[0] - rightAnswer) == Math.Abs(answers[1] - rightAnswer))
			{
				//this means they were both same - compare by time
				if (times[0] < times[1])
				{
					decideWin(answers, times, rightAnswer, 1, 2, region, defenderID, attackerID);
				}   //TODO: consider case where equal - very unlikely but still possible?
				else
				{
					decideWin(answers, times, rightAnswer, 2, 1, region, defenderID, attackerID);
				}
			}
			else //second player was closer
			{
				decideWin(answers, times, rightAnswer, 2, 1, region, defenderID, attackerID);
			}
		}

		private void FirstRound()
        {
			//send an question to the clients...
			string question = PickRandomNumberQuestion();

			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData;
			Int32 bytes;
			Byte[] data = new byte[1024];

			int[] answers = new int[Constants.MAX_PLAYERS];
			int[] times = new int[Constants.MAX_PLAYERS];

			int i = 0;

			foreach (TcpClient client in acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
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
			DecideNumberQuestionWinnerAndInform(answers, times, question, FirstRoundWin, null, null, null);

		}

		private void FirstRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID, Constants.Region? reg, int? x, int? y)
        {
			//we dont need the region, x and y here
			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + "_" + answers[1] + "_" + 
				times[0] + "_" + times[1] + "_" + rightAnswer + "_" + winnerID;
			SendMessageToAllClients(message);

			Thread.Sleep(Constants.DELAY_SHOWANSWERS);

			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(loserID);
			sw.Stop();
			Console.WriteLine("First round total time elapsed (ms): " + sw.ElapsedMilliseconds);
		}

		private void SendFirstRoundPickAnnouncement(int clientID)
        {
			//let the winner choose twice and the loser once
			string message = Constants.PREFIX_PICKREGION + clientID;
			SendMessageToAllClients(message);

			//wait for the picks
			Thread.Sleep(Constants.DELAY_FIRSTROUND_PICKS);
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
			Thread.Sleep(Constants.DELAY_WAITFORCLIENTUPDATE);
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
						this.gameInformation.addPoints(player, Constants.POINTS_FIRSTROUND);
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

		private string GameOverMessage(int winnerID)
        {
			return Constants.PREFIX_GAMEOVER + winnerID;
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
