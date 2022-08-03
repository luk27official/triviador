using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Commons;

namespace server
{
	internal class Server
	{
		bool _running;
		TcpListener _server;
		TcpClient[] _acceptedClients;
		int _acceptedClientsIndex; //holds the number of already connected people
		GameInformation _gameInformation;
		string[] _questionsABCDWithAnswers;
		string[] _questionsNumericWithAnswers;
		bool _playerDisconnected;

		public Server()
		{
			SetupServer();
		}

		public void SetupServer()
        {
			try
            {
                using StreamReader reader = new(Constants.CONFIG_FILENAME);

                reader.ReadLine(); //we do not need the first one, its there just for reference
                string? line2 = reader.ReadLine(); //we do not need the first one
                                                  //from the second one parse ip and port
				if(line2 != null)
                {
					string[] splitLine2 = line2.Split(Constants.GLOBAL_DELIMITER);
					IPAddress localAddr = IPAddress.Parse(splitLine2[0]);
					Int32 port = Int32.Parse(splitLine2[1]);
					_server = new TcpListener(localAddr, port);
					Console.WriteLine(String.Format(Constants.SERVER_LISTEN, localAddr, port));
				}
				else
                {
					Console.WriteLine(Constants.SERVER_INVALID_CFG);
				}
				return;
            }
			catch
            {
				_server = new TcpListener(IPAddress.Parse(Constants.DEFAULT_SERVER_HOSTNAME), Constants.DEFAULT_SERVER_PORT); //default settings
				Console.WriteLine(Constants.SERVER_ERROR);
				Console.WriteLine(String.Format(Constants.SERVER_USING_DEFAULT, Constants.DEFAULT_SERVER_HOSTNAME, Constants.DEFAULT_SERVER_PORT));
			}
		}

		public void ResetInformation()
        {
			_acceptedClients = new TcpClient[Constants.MAX_PLAYERS];
			_acceptedClientsIndex = 0;
			_gameInformation = new GameInformation();
            Console.WriteLine(Constants.SERVER_RESET);
			_playerDisconnected = false;
		}

		public void Start()
		{
			ResetInformation();

			try
			{
				_questionsABCDWithAnswers = File.ReadAllLines(Constants.QUESTIONS_ABCD_FILENAME);
				_questionsNumericWithAnswers = File.ReadAllLines(Constants.QUESTIONS_NUMS_FILENAME);
				_server.Start();
				_running = true;
				while (_running)
				{
					var client = _server.AcceptTcpClient();
					var task = Task.Run(() => SaveClient(client));
					try
					{
						task.Wait();
					}
					catch (Exception)
					{
						throw new Constants.DisconnectException();
                    }
				}
			}
			catch (Constants.DisconnectException)
            {
				throw new Constants.DisconnectException();
			}
			catch (Exception e)
            {
                Console.WriteLine(Constants.ERROR, e.Message);
            }
		}

		private void SaveClient(TcpClient currentClient)
		{
			if (_acceptedClientsIndex > Constants.MAX_PLAYERS - 1) //a third user tries to connect, do not do anything
			{
				return;
			}

			lock (_acceptedClients) //prevent from more clients accessing the array at once, register them and send the information about connection
			{
				Console.WriteLine(Constants.SERVER_ACCEPT, (_acceptedClientsIndex + 1).ToString());
				_acceptedClients[_acceptedClientsIndex] = currentClient;

				if (_acceptedClientsIndex == 0) //inform the first user about their connection
				{
					NetworkStream stream = currentClient.GetStream();
					string message = Constants.P1CONNECTED;
					byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

					stream.Write(data, 0, data.Length);
					Console.WriteLine(Constants.SERVER_SENT, _acceptedClientsIndex + 1, message);
					Interlocked.Add(ref _acceptedClientsIndex, 1);
					return;
				}

				Interlocked.Add(ref _acceptedClientsIndex, 1);
			}

			//otherwise we have already one player connected, so lets
			//inform both users they have been connected
			if (_acceptedClientsIndex == Constants.MAX_PLAYERS)
				InformPlayersAboutConnection();
		}

		private void InformPlayersAboutConnection() {

			SendMessageToAllClients(Constants.P2CONNECTED);

			Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);

			AssignPlayerIDs();

			GameStart();
		}

		private void AssignPlayerIDs()
        {
			int i = 0;
			string[] availableIDs = new string[Constants.MAX_PLAYERS];
			availableIDs[0] = Constants.P1ASSIGN;
			availableIDs[1] = Constants.P2ASSIGN;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					string message = availableIDs[i];
					byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);
					stream.Write(msg, 0, msg.Length);
					Console.WriteLine(Constants.SERVER_SENT, i + 1, message);
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
					continue;
				}
			}
		}

		private void AssignBaseRegions()
        {
			//firstly, we have to pick a random region and assign it to the players
			//this could be done in an for loop, but again there are some limits so it would have to be re-done anyway
			var list = Enum.GetValues(typeof(Constants.Region)).Cast<Constants.Region>().ToList();
			Random random = new();

			Constants.Region player1Base = list[random.Next(list.Count)];
			Constants.Region player2Base = list[random.Next(list.Count)];

			while (player1Base == player2Base || Constants.DoRegionsNeighbor(player1Base, new List<Constants.Region>() { player2Base }))
			{
				player2Base = list[random.Next(list.Count)]; //pick another one
			}

			_gameInformation.setBase(1, player1Base);
			_gameInformation.setBase(2, player2Base);

			SendGameInfoAndCheckDisconnect();
		}

		private void SendGameInfoAndCheckDisconnect()
        {
			//basically just check if someone disconnected... if yes, inform the client
			if (this._playerDisconnected)
			{
				SendMessageToAllClients(Constants.PREFIX_DISCONNECTED + Constants.INVALID_CLIENT_ID);
			}
			else SendMessageToAllClients(_gameInformation.EncodeInformationToString());
        }

		private void GameStart()
        {
			AssignBaseRegions();
			
			for(int i = 0; i < Constants.FIRST_ROUND_QUESTIONS_COUNT; i++)
            {
				Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
				FirstRound();
			}

			for(int i = 0; i < (Constants.SECOND_ROUND_FIRST_VERSION_QUESTIONS_COUNT + 
				Constants.SECOND_ROUND_SECOND_VERSION_QUESTIONS_COUNT) / 4; i++)
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
		}

		private void FirstRound()
        {
			string question = PickRandomNumberQuestion();

			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData;
			Int32 bytes;
			Byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];

			int[] answers = new int[Constants.MAX_PLAYERS];
			int[] times = new int[Constants.MAX_PLAYERS];

			int i = 0;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine(Constants.SERVER_RECEIVE, responseData);
					string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
					answers[i] = Int32.Parse(splitData[2]);
					times[i] = Int32.Parse(splitData[3]);
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
					continue;
				}
			}

			DecideNumberQuestionWinnerAndInform(answers, times, question, FirstRoundWin, null, null, null);
		}

		private void FirstRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID, Constants.Region? reg, int? x, int? y)
        {
			//we dont need the region, x and y here
			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + Constants.GLOBAL_DELIMITER + answers[1] + Constants.GLOBAL_DELIMITER + 
				times[0] + Constants.GLOBAL_DELIMITER + times[1] + Constants.GLOBAL_DELIMITER + rightAnswer + Constants.GLOBAL_DELIMITER + winnerID;
			SendMessageToAllClients(message);

            Thread.Sleep(Constants.DELAY_SHOWANSWERS);

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
			Thread.Sleep(Constants.DELAY_FIRSTROUND_PICKS);
			string[] receivedData = new string[Constants.MAX_PLAYERS];
			Int32 bytes;
			Byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];
			int i = 0;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					receivedData[i] = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine(Constants.SERVER_RECEIVE, receivedData[i]);
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
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
				string[] splitData = current.Split(Constants.GLOBAL_DELIMITER);
				if (splitData[2] != Constants.INVALID_CLIENT_ID.ToString())
				{
					int player = Int32.Parse(splitData[1]);
					if (Enum.TryParse(splitData[2], out Constants.Region reg))
					{
						this._gameInformation.addPoints(player, Constants.POINTS_BASIC_REGION);
						this._gameInformation.addRegion(player, reg);
						SendGameInfoAndCheckDisconnect();
						break;
					}
				}
			}
		}

		private void SendMessageToAllClients(string message)
        {
			byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);

			int i = 0;
			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					if (client != null)
					{
						NetworkStream stream = client.GetStream();
						stream.Write(msg, 0, msg.Length);
						Console.WriteLine(Constants.SERVER_SENT, i + 1, message);
						i++;
					}
					else throw new Constants.DisconnectException();
				}
				catch (Exception)
				{
					//one of the players disconnected
					this._playerDisconnected = true;
					if (!message.StartsWith(Constants.PREFIX_DISCONNECTED))
                    {
						SendMessageToAllClients(Constants.PREFIX_DISCONNECTED + Constants.INVALID_CLIENT_ID);
						return;
					}
					continue;
				}
			}

			if(message.StartsWith(Constants.PREFIX_DISCONNECTED))
            {
				throw new Constants.DisconnectException();
            }
		}

		private Constants.Region PicksSecondRound(int clientID)
		{
			string message = Constants.PREFIX_PICKREGION + clientID.ToString();
			SendMessageToAllClients(message);

			//wait for the picks
			Thread.Sleep(Constants.DELAY_FIRSTROUND_PICKS);

			string[] receivedData = new string[Constants.MAX_PLAYERS];
			Int32 bytes;
			Byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];
			int i = 0;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					receivedData[i] = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine(Constants.SERVER_RECEIVE, receivedData[i]);
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
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
				reg = this._gameInformation.Bases[otherID - 1];
			}
			else
			{
				reg = PicksSecondRound(attackerID);
			}

			//wait 3s and then send the question
			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			string question = PickRandomABCDQuestion();
			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);
			//we have sent the question. Now we have to wait to register all answers...
			string responseData;
			Int32 bytes;
			Byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];

			string[] answers = new string[Constants.MAX_PLAYERS];
			int i = 0;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine(Constants.SERVER_RECEIVE, responseData);
					string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
					answers[i] = splitData[2];
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
					continue;
				}
			}

			DecideSecondRoundWinnerFirstQuestion(answers, question, attackerID, otherID, reg);
		}

		private void DecideSecondRoundWinnerFirstQuestion(string[] answers, string question, int attackerID, int defenderID, Constants.Region attackedRegion)
		{
			//finalanswers_correctANS_P1ANS_P2ANS
			string[] splitQuestion = question.Split(Constants.GLOBAL_DELIMITER);
			string correct = splitQuestion[2];
			string p1Answer = answers[0];
			string p2Answer = answers[1];

			SendMessageToAllClients(Constants.PREFIX_FINALANSWERS + correct + Constants.GLOBAL_DELIMITER + p1Answer + Constants.GLOBAL_DELIMITER + p2Answer);

			//we've just sent the information about the correct answers...
			//we have to calculate the actual winner

			Thread.Sleep(Constants.DELAY_SHOWANSWERS);


			//here the client can receive 3 types of answers!!
			//1) end game
			//2) game info update
			//3) new question (num/ABCD)

			if (correct == answers[attackerID - 1] && correct != answers[defenderID - 1])
			{
				SecondRoundAttackerWin(attackerID, defenderID, attackedRegion);
			}
			else if (correct == answers[attackerID - 1] && correct == answers[defenderID - 1])
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
			if (attackedRegion == this._gameInformation.Bases[defenderID - 1])
			{
				this._gameInformation.decreaseBaseHealth(defenderID);
				if (this._gameInformation.BaseHealths[defenderID - 1] == 0)
				{
					Thread.Sleep(Constants.DELAY_ENDGAME);
					SendMessageToAllClients(GameOverMessage(attackerID));
					var mydelegate = new Action(delegate ()
					{
						this.Reset();
					});
					mydelegate.Invoke();
				}
				SecondRound(attackerID, defenderID, true);
			}
			else
			{
				if (this._gameInformation.HighValueRegions.Contains(attackedRegion))
				{
					this._gameInformation.addPoints(defenderID, -Constants.POINTS_HIGH_VALUE_REGION);
				}
				else
				{
					this._gameInformation.addPoints(defenderID, -Constants.POINTS_BASIC_REGION);
				}
				this._gameInformation.addPoints(attackerID, Constants.POINTS_HIGH_VALUE_REGION);
				this._gameInformation.addRegion(attackerID, attackedRegion);
				this._gameInformation.removeRegion(defenderID, attackedRegion);
				this._gameInformation.addHighValueRegion(attackedRegion);
				SendGameInfoAndCheckDisconnect();
			}
		}

		private void SecondRoundDefenderWin(int defenderID)
		{
			//defender answered ok
			this._gameInformation.addPoints(defenderID, Constants.POINTS_DEFENDER_WIN);
			SendGameInfoAndCheckDisconnect();
		}

		private void SecondRoundNoOneAnsweredCorrectly()
		{
			SendGameInfoAndCheckDisconnect();
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
			Byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];

			int[] answers = new int[Constants.MAX_PLAYERS];
			int[] times = new int[Constants.MAX_PLAYERS];

			int i = 0;

			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					NetworkStream stream = client.GetStream();
					bytes = stream.Read(data, 0, data.Length);
					responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
					Console.WriteLine(Constants.SERVER_RECEIVE, responseData);
					string[] splitData = responseData.Split(Constants.GLOBAL_DELIMITER);
					answers[i] = Int32.Parse(splitData[2]);
					times[i] = Int32.Parse(splitData[3]);
					i++;
				}
				catch (Exception)
				{
					this._playerDisconnected = true;
					continue;
				}
			}

			//now it is time to compare the answers...
			//first compare by the actual answers
			DecideNumberQuestionWinnerAndInform(answers, times, question, SecondRoundWin, attackedRegion, defenderID, attackerID);
		}

		private void SecondRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID, Constants.Region? attackedRegion, int? defenderID, int? attackerID)
		{
			if (attackedRegion == null || defenderID == null || attackerID == null) return;
			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + Constants.GLOBAL_DELIMITER + answers[1] + Constants.GLOBAL_DELIMITER +
				times[0] + Constants.GLOBAL_DELIMITER + times[1] + Constants.GLOBAL_DELIMITER + rightAnswer + Constants.GLOBAL_DELIMITER + winnerID;
			SendMessageToAllClients(message);


			Thread.Sleep(Constants.DELAY_SHOWANSWERS);

			if (winnerID == attackerID)
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
				string[] splitData = current.Split(Constants.GLOBAL_DELIMITER);
				if (splitData[2] != Constants.INVALID_CLIENT_ID.ToString())
				{
					if (Enum.TryParse(splitData[2], out Constants.Region reg))
					{
						return reg;
					}
				}
			}

			return Constants.Region.CZZL; //WILL NOT HAPPEN!
		}

		private void DecideNumberQuestionWinnerAndInform(int[] answers, int[] times, string question,
			Action<int[], int[], int, int, int, Constants.Region?, int?, int?> decideWin,
			Constants.Region? region, int? defenderID, int? attackerID)
		{
			int rightAnswer = Int32.Parse(question.Split(Constants.GLOBAL_DELIMITER)[2]);

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
				}   //consider case where equal - very unlikely but still possible?
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

		private void DecideWinnerBasedOnPoints()
		{
			//this also applies just to two players
			string message;

			if (_gameInformation.Points[0] > _gameInformation.Points[1])
			{
				message = GameOverMessage(1);
			}
			else if (_gameInformation.Points[0] == _gameInformation.Points[1])
			{
				message = GameOverMessage(Constants.INVALID_CLIENT_ID); //tie
			}
			else
			{
				message = GameOverMessage(2);
			}
			Thread.Sleep(Constants.DELAY_ENDGAME);
			SendMessageToAllClients(message);

			var mydelegate = new Action(delegate ()
			{
				this.Reset();
			});
			mydelegate.Invoke();
		}

		private string GameOverMessage(int winnerID)
        {
			return Constants.PREFIX_GAMEOVER + winnerID;
		}

		private string PickRandomABCDQuestion()
        {
			Random rnd = new();
			int r = rnd.Next(_questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONABCD + _questionsABCDWithAnswers[r];
		}

		private string PickRandomNumberQuestion()
        {
			Random rnd = new();
			int r = rnd.Next(_questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONNUMBER + _questionsNumericWithAnswers[r];
		}

		public void Reset()
		{
			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					if(client != null) client.Close();
				}
				catch (Exception)
				{
					//this does not really matter if we're resetting...
					continue;
				}
			}
			ResetInformation();
		}

		public void Stop()
        {
			foreach (TcpClient client in _acceptedClients)
			{
				try
				{
					if (client != null) client.Close();
				}
				catch (Exception)
				{
					//this does not really matter if we're stopping...
					continue;
				}
			}
			_server.Stop();
		}
	}
}
