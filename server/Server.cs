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
	/// <summary>
	/// This class contains all of the server logic.
	/// </summary>
	internal class Server
	{
		bool _running;
		TcpListener _server;
		TcpClient[] _acceptedClients;
		int _acceptedClientsIndex;					//holds the number of already connected people
		GameInformation _gameInformation;
		string[] _questionsABCDWithAnswers;			//holds all of the questions with options
		string[] _questionsNumericWithAnswers;		//holds all of the numeric questions
		bool _playerDisconnected;					//did at least one player disconnect?

		public Server()
		{
			SetupServer();
		}

		/// <summary>
		/// Method called for initializing the server.
		/// </summary>
		public void SetupServer()
        {
			try
            {
                using StreamReader reader = new(Constants.CONFIG_FILENAME);

                reader.ReadLine();	//we do not need the first line, its there just for reference
                string? line2 = reader.ReadLine(); //from the second line parse ip and port

				if (line2 != null)
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

		/// <summary>
		/// Method which resets the server information, so new players can connect.
		/// </summary>
		public void ResetInformation()
        {
			_acceptedClients = new TcpClient[Constants.MAX_PLAYERS];
			_acceptedClientsIndex = 0;
			_gameInformation = new GameInformation();
            Console.WriteLine(Constants.SERVER_RESET);
			_playerDisconnected = false;
		}

		/// <summary>
		/// Starts the server. Reads the questions from specified files and handles client connections.
		/// </summary>
		/// <exception cref="Constants.DisconnectException">Exception thrown when some of the players disconnect.</exception>
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

		/// <summary>
		/// Handles client connection. If there is a free spot for the player, the client is assigned an ID and receives an according message.
		/// </summary>
		/// <param name="currentClient">TcpClient used to communicate with the client.</param>
		private void SaveClient(TcpClient currentClient)
		{
			if (_acceptedClientsIndex > Constants.MAX_PLAYERS - 1) //more users try to connect later in game, do not do anything
			{
				return;
			}

			lock (_acceptedClients) //prevent from more clients accessing the array at once, register them and send the information about connection
			{
				EndPoint? ep1 = currentClient.Client.RemoteEndPoint;
				IPEndPoint ep2;
				if (ep1 == null)
                {
					ep2 = new IPEndPoint(IPAddress.Any, Constants.DEFAULT_SERVER_PORT);
                }
				else
                {
					ep2 = (IPEndPoint)ep1;
                }
				Console.WriteLine(Constants.SERVER_ACCEPT, (_acceptedClientsIndex + 1).ToString(), ep2.Address.ToString());
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

			if (_acceptedClientsIndex == Constants.MAX_PLAYERS)
				InformPlayersAboutConnection();
		}

		/// <summary>
		/// Method sending all players info about the start of the game.
		/// </summary>
		private void InformPlayersAboutConnection() {

			SendMessageToAllClients(Constants.P2CONNECTED);

			Thread.Sleep(Constants.DELAY_FASTUPDATE_MS);

			AssignPlayerIDs();

			Play();
		}

		/// <summary>
		/// Method assigning ID numbers to all clients.
		/// </summary>
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

		/// <summary>
		/// Method assigning base regions to the players.
		/// </summary>
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

			_gameInformation.SetBase(1, player1Base);
			_gameInformation.SetBase(2, player2Base);

			SendGameInfoAndCheckDisconnect();
		}

		/// <summary>
		/// Method which checks if player/s disconnected and according to that sends a message to the clients.
		/// </summary>
		private void SendGameInfoAndCheckDisconnect()
        {
			//basically just check if someone disconnected... if yes, inform the client
			if (this._playerDisconnected)
			{
				SendMessageToAllClients(Constants.PREFIX_DISCONNECTED + Constants.INVALID_CLIENT_ID);
			}
			else SendMessageToAllClients(_gameInformation.EncodeInformationToString());
        }

		/// <summary>
		/// Method which controls the game. All rounds are defined here.
		/// </summary>
		private void Play()
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
			
			//no one has won till now -> decide the winner based on points
			DecideWinnerBasedOnPoints();
		}

		/// <summary>
		/// Method which controls the first round, picks question and sends it to clients and then waits for answers.
		/// </summary>
		private void FirstRound()
        {
			string question = PickRandomNumericQuestion();

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

		/// <summary>
		/// Method which sends the correct answers and information to the clients. Then sends three instructions to pick a region.
		/// </summary>
		/// <param name="answers">Player answers.</param>
		/// <param name="times">Player answer times.</param>
		/// <param name="rightAnswer">Right answer to the question.</param>
		/// <param name="winnerID">Winner client identifier.</param>
		/// <param name="loserID">Loser client identifier.</param>
		/// <param name="reg">Set to null.</param>
		/// <param name="x">Set to null.</param>
		/// <param name="y">Set to null.</param>
		private void FirstRoundWin(int[] answers, int[] times, int rightAnswer, int winnerID, int loserID, Constants.Region? reg, int? x, int? y)
        {
			//we dont need the reg, x and y here, its there just for compatibility with a delegate

			//send the players the info about their answers
			string message = Constants.PREFIX_FINALANSWERS + answers[0] + Constants.GLOBAL_DELIMITER + answers[1] + Constants.GLOBAL_DELIMITER + 
				times[0] + Constants.GLOBAL_DELIMITER + times[1] + Constants.GLOBAL_DELIMITER + rightAnswer + Constants.GLOBAL_DELIMITER + winnerID;
			SendMessageToAllClients(message);

            Thread.Sleep(Constants.DELAY_SHOWANSWERS);

			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(winnerID);
			SendFirstRoundPickAnnouncement(loserID);
		}

		/// <summary>
		/// Method which sends the client an instruction to pick a region on the map.
		/// </summary>
		/// <param name="clientID">Client identifier.</param>
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

		/// <summary>
		/// Method updating the server's game information based on the pick from the player.
		/// </summary>
		/// <param name="data">Client instruction with data about the picked region.</param>
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
						this._gameInformation.AddPoints(player, Constants.POINTS_BASIC_REGION);
						this._gameInformation.AddRegion(player, reg);
						SendGameInfoAndCheckDisconnect();
						break;
					}
				}
			}
		}

		/// <summary>
		/// A method which sends a message to all connected clients.
		/// </summary>
		/// <param name="message">Message to be sent.</param>
		/// <exception cref="Constants.DisconnectException">Exception thrown when some of the players disconnect.</exception>
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

		/// <summary>
		/// A method which sends an instruction for the attacker to pick a region to attack.
		/// </summary>
		/// <param name="attackerID">Attacker client identifier.</param>
		/// <returns>An attacked region.</returns>
		private Constants.Region PicksSecondRound(int attackerID)
		{
			string message = Constants.PREFIX_PICKREGION + attackerID.ToString();
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
			#pragma warning disable CS8629 // Nullable value type may be null.
				//the return value should be never null as one of the players always picks
				Constants.Region attackedRegion = (Constants.Region) SecondRoundGetPickedRegion(receivedData);
			#pragma warning restore CS8629 // Nullable value type may be null.
			string attackMessage = Constants.PREFIX_ATTACK + attackedRegion.ToString();
			SendMessageToAllClients(attackMessage);
			return attackedRegion;
		}

		/// <summary>
		/// Method which controls the second round, sends a pick request, then sends question and waits for answers.
		/// </summary>
		/// <param name="attackerID">Attacker client identifier.</param>
		/// <param name="otherID">Other client identifier.</param>
		/// <param name="repeatedBaseAttack">True, if it is a repeated attack (first one succeeded).</param>
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

			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			string question = PickRandomABCDQuestion();
			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);

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

		/// <summary>
		/// Method which decides the winner of the question with options in the second round.
		/// Sends the correct answer, player information and then the continuation of the game.
		/// </summary>
		/// <param name="answers">Player answers.</param>
		/// <param name="question">Current question with answer.</param>
		/// <param name="attackerID">Attacker client identifier.</param>
		/// <param name="defenderID">Defender client identifier.</param>
		/// <param name="attackedRegion">Region which is being attacked.</param>
		private void DecideSecondRoundWinnerFirstQuestion(string[] answers, string question, int attackerID, int defenderID, Constants.Region attackedRegion)
		{
			//finalanswers_correctANS_P1ANS_P2ANS
			string[] splitQuestion = question.Split(Constants.GLOBAL_DELIMITER);
			string correct = splitQuestion[2];
			string p1Answer = answers[0];
			string p2Answer = answers[1];

			SendMessageToAllClients(Constants.PREFIX_FINALANSWERS + correct + Constants.GLOBAL_DELIMITER + p1Answer + Constants.GLOBAL_DELIMITER + p2Answer);

			Thread.Sleep(Constants.DELAY_SHOWANSWERS);

			//here the client can receive more types of answers!!
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
				SecondRoundNoOneAnsweredCorrectly();
			}
		}

		/// <summary>
		/// This method handles the win of attacking player in the second round. 
		/// If the player attacked other player's base, it sends a new question or ends the game.
		/// </summary>
		/// <param name="attackerID">Attacker client identifier.</param>
		/// <param name="defenderID">Defender client identifier.</param>
		/// <param name="attackedRegion">Region which is being attacked.</param>
		private void SecondRoundAttackerWin(int attackerID, int defenderID, Constants.Region attackedRegion)
		{
			//only the attacker answered correctly
			if (attackedRegion == this._gameInformation.Bases[defenderID - 1])
			{
				this._gameInformation.DecreaseBaseHealth(defenderID);
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
					this._gameInformation.AddPoints(defenderID, -Constants.POINTS_HIGH_VALUE_REGION);
				}
				else
				{
					this._gameInformation.AddPoints(defenderID, -Constants.POINTS_BASIC_REGION);
				}
				this._gameInformation.AddPoints(attackerID, Constants.POINTS_HIGH_VALUE_REGION);
				this._gameInformation.AddRegion(attackerID, attackedRegion);
				this._gameInformation.RemoveRegion(defenderID, attackedRegion);
				this._gameInformation.AddHighValueRegion(attackedRegion);
				SendGameInfoAndCheckDisconnect();
			}
		}

		/// <summary>
		/// Method handling win of the defender in the second round.
		/// </summary>
		/// <param name="defenderID">Defender client identifier.</param>
		private void SecondRoundDefenderWin(int defenderID)
		{
			this._gameInformation.AddPoints(defenderID, Constants.POINTS_DEFENDER_WIN);
			SendGameInfoAndCheckDisconnect();
		}

		/// <summary>
		/// Method handling wrong answersin the second round.
		/// </summary>
		private void SecondRoundNoOneAnsweredCorrectly()
		{
			SendGameInfoAndCheckDisconnect();
		}

		/// <summary>
		/// Method called when the question with options had a winning tie.
		/// A new numeric question is being sent to the players and handled properly.
		/// </summary>
		/// <param name="attackerID">Attacker client identifier.</param>
		/// <param name="defenderID">Defender client identifier.</param>
		/// <param name="attackedRegion">Region which is being attacked.</param>
		private void SecondRoundAnotherQuestion(int attackerID, int defenderID, Constants.Region attackedRegion)
		{
			Thread.Sleep(Constants.DELAY_BETWEEN_ROUNDS);
			string question = PickRandomNumericQuestion();
			SendMessageToAllClients(question);

			Thread.Sleep(Constants.DELAY_WAITFORANSWERS);

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

			DecideNumberQuestionWinnerAndInform(answers, times, question, SecondRoundWin, attackedRegion, defenderID, attackerID);
		}

		/// <summary>
		/// Method which sends the correct answers and information to the clients. Then sends three instructions to pick a region.
		/// </summary>
		/// <param name="answers">Player answers.</param>
		/// <param name="times">Player answer times.</param>
		/// <param name="rightAnswer">Right answer to the question.</param>
		/// <param name="winnerID">Winner client identifier.</param>
		/// <param name="loserID">Loser client identifier.</param>
		/// <param name="attackedRegion">Attacked region.</param>
		/// <param name="defenderID">Defender client identifier.</param>
		/// <param name="attackerID">Attacker client identifier.</param>
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

		/// <summary>
		/// Method parsing the picked region in the second round from the attacker.
		/// </summary>
		/// <param name="data">Array with data from the clients.</param>
		/// <returns>Region chosen by a client to be attacked.</returns>
		private Constants.Region? SecondRoundGetPickedRegion(string[] data)
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

			return null; //WILL NOT HAPPEN!
		}

		/// <summary>
		/// Method which decides who the winner of the numeric question is and calls the responsible "decideWin" method.
		/// </summary>
		/// <param name="answers">Player answers.</param>
		/// <param name="times">Player answer times.</param>
		/// <param name="question">Question with the correct answer.</param>
		/// <param name="decideWin">Delegate to be called by this method.</param>
		/// <param name="region">Attacked region, if available.</param>
		/// <param name="defenderID">Defender client identifier, if available.</param>
		/// <param name="attackerID">Attacker client identifier, if available.</param>
		private void DecideNumberQuestionWinnerAndInform(int[] answers, int[] times, string question,
			Action<int[], int[], int, int, int, Constants.Region?, int?, int?> decideWin,
			Constants.Region? region, int? defenderID, int? attackerID)
		{
			int rightAnswer = Int32.Parse(question.Split(Constants.GLOBAL_DELIMITER)[2]);

			if (Math.Abs(answers[0] - rightAnswer) < Math.Abs(answers[1] - rightAnswer))
			{
				//then this means that the player 1 was closer, thus the winner
				decideWin(answers, times, rightAnswer, 1, 2, region, defenderID, attackerID);
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
			else
			{
				decideWin(answers, times, rightAnswer, 2, 1, region, defenderID, attackerID);
			}
		}

		/// <summary>
		/// This method decides the winner of the game based on points, because no base was destroyed.
		/// </summary>
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

		/// <summary>
		/// Method returning game over message for the clients.
		/// </summary>
		/// <param name="winnerID">Game winner identifier.</param>
		/// <returns>Game over message for the clients.</returns>
		private string GameOverMessage(int winnerID)
        {
			return Constants.PREFIX_GAMEOVER + winnerID;
		}

		/// <summary>
		/// Picks a random question from the loaded questions.
		/// </summary>
		/// <returns>Random question with options.</returns>
		private string PickRandomABCDQuestion()
        {
			Random rnd = new();
			int r = rnd.Next(_questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONABCD + _questionsABCDWithAnswers[r];
		}

		/// <summary>
		/// Picks a random numeric question from the loaded questions.
		/// </summary>
		/// <returns>Random numeric question.</returns>
		private string PickRandomNumericQuestion()
        {
			Random rnd = new();
			int r = rnd.Next(_questionsABCDWithAnswers.Length - 1);
			return Constants.PREFIX_QUESTIONNUMBER + _questionsNumericWithAnswers[r];
		}

		/// <summary>
		/// Method called to reset the server.
		/// </summary>
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

		/// <summary>
		/// Method called to stop the server.
		/// </summary>
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
