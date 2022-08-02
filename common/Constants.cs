using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public static class Constants
{
	public enum Region {
		CZJC,
		CZJM,
		CZKA,
		CZKR,

		CZLI,
		CZMO,
		CZOL,
		CZPA,

		CZZL,
		CZPL,
		CZPR,
		CZST,

		CZUS,
		CZVY
	};

	public static Region[][] NEIGHBORING_REGIONS => new Region[14][] { 

		new Region[] { Region.CZPL, Region.CZST, Region.CZVY, Region.CZJM},
		new Region[] { Region.CZJC, Region.CZVY, Region.CZPA, Region.CZOL, Region.CZZL },
		new Region[] { Region.CZPL, Region.CZUS },
		new Region[] { Region.CZPA, Region.CZST, Region.CZLI },

		new Region[] { Region.CZST, Region.CZUS, Region.CZKR },
		new Region[] { Region.CZOL, Region.CZZL },
		new Region[] { Region.CZPA, Region.CZJM, Region.CZZL, Region.CZMO },
		new Region[] { Region.CZKR, Region.CZST, Region.CZVY, Region.CZJM, Region.CZOL},

		new Region[] { Region.CZMO, Region.CZOL, Region.CZJM },
		new Region[] { Region.CZJC, Region.CZST, Region.CZUS, Region.CZKA },
		new Region[] { Region.CZST },
		new Region[] { Region.CZUS, Region.CZLI, Region.CZPR, Region.CZKR, Region.CZPA, Region.CZVY, Region.CZJC, Region.CZPL },

		new Region[] { Region.CZKA, Region.CZLI, Region.CZST, Region.CZPL },
		new Region[] { Region.CZST, Region.CZPA, Region.CZJC, Region.CZJM },

	};

	public static bool DoRegionsNeighbor(Constants.Region region, List<Constants.Region> possibleNeighbors)
	{
		foreach (Constants.Region r in possibleNeighbors)
		{
			if (NEIGHBORING_REGIONS[(int)r].Contains(region))
			{
				return true;
			}
		}
		return false;
	}

	public static Region? PickRandomEnemyRegion(List<Region>[] all, int clientID, bool neighboring)
    {
		var allRegions = Enum.GetValues(typeof(Constants.Region)).Cast<Constants.Region>();

		var enemyRegions = new List<Region>();

		foreach(var region in allRegions)
        {
			if (all[clientID].Contains(region)) continue;
			enemyRegions.Add(region);
        }

		Random rnd = new Random();

		var shuffled = enemyRegions.OrderBy(_ => rnd.Next()).ToList();

		if(neighboring)
        {
			foreach (var region in shuffled)
			{
				if (DoRegionsNeighbor(region, all[clientID])) return region;
			}
		}

		int random = rnd.Next(shuffled.Count());
		return shuffled.ElementAt(random);
	}

	public static Region? PickRandomFreeNeighboringRegion(List<Region> picker, List<Region>[] all, int clientID)
    {
		List<Region> populatedRegions = new List<Region>();
		foreach (var list in all)
        {
			populatedRegions.AddRange(list);
		}
		if (populatedRegions.Count == REGION_COUNT) return null;

		var allRegions = Enum.GetValues(typeof(Constants.Region)).Cast<Constants.Region>();

		var freeRegions = new List<Region>();

		foreach (var region in allRegions)
        {
			if (populatedRegions.Contains(region)) continue;
			freeRegions.Add(region);
        }

		Random rnd = new Random();

		var shuffled = freeRegions.OrderBy(_ => rnd.Next()).ToList();

		foreach (var region in shuffled)
        {
			if (DoRegionsNeighbor(region, all[clientID])) return region;
        }

		int random = rnd.Next(shuffled.Count());
		return shuffled.ElementAt(random);
    }

	public enum GameStatus
    {
		Loading,
		FirstRound,
		SecondRound_FirstVersion,
		SecondRound_SecondVersion,
		GameOver
    }

	public const int MAX_PLAYERS = 2;
	public const int REGION_COUNT = 14;

	public const int FIRST_ROUND_QUESTIONS_COUNT = 4;
	public const int SECOND_ROUND_FIRST_VERSION_QUESTIONS_COUNT = 6;
	public const int SECOND_ROUND_SECOND_VERSION_QUESTIONS_COUNT = 2;

	public const int DEFAULT_BUFFER_SIZE = 1024;

	public const char GLOBAL_DELIMITER = '_';

	public const string PLAYER_PICK_REGION = "Player {0} is supposed to pick a region!";
	public const string SERVER_UPDATE = "Wait for the server to update information!";
	public const string REGION_UNDER_ATTACK = "Region {0} under attack!";
	public const string STARTING_SOON = "The round will start in a few seconds. Get ready to answer!";
	public const string PLAYER_ID_LABEL = "My player ID is {0}";
	public const string PLAYER_PICKED = "You have picked: {0}";

	public const string ERROR_MENU = "An error occured. Try again later or check the connection information. Error message: {0}";

	public const string GAMEOVER_TIE = "Game over! It's a tie!";
	public const string GAMEOVER_WIN = "Congratulations! You have won the game!";
	public const string GAMEOVER_LOSE = "You have lost! Better luck next time!";


	public const string P1CONNECTED = "You have been connected, waiting for player 2.";
	public const string P2CONNECTED = "Both players have connected, game starting soon.";

	public const string PREFIX_ASSIGN = "assign_";
	public const string P1ASSIGN = "assign_p1";
	public const string P2ASSIGN = "assign_p2";

	public const string PREFIX_GAMEUPDATE = "gameupdate_";

	public const string PREFIX_QUESTIONABCD = "questionAW_";
	public const string PREFIX_QUESTIONNUMBER = "questionNUM_";

	public const string PREFIX_ANSWER = "answer_";

	public const string PREFIX_FINALANSWERS = "finalanswers_";

	public const string PREFIX_PICKREGION = "pickregion_";
	public const string PREFIX_PICKED = "picked_";

	public const string PREFIX_ATTACK = "attack_";

	public const string PREFIX_GAMEOVER = "gameover_";

	public const int DELAY_FASTUPDATE_MS = 1000;
	public const int DELAY_FIRSTROUND_FIRSTQUESTION = 3000;
	public const int DELAY_WAITFORANSWERS = 13000;
	public const int DELAY_SHOWANSWERS = 5000;
	public const int DELAY_FIRSTROUND_PICKS = 7000;
	public const int DELAY_WAITFORCLIENTUPDATE = 2000;
	public const int DELAY_ENDGAME = 5000;

	public const int DELAY_BETWEEN_ROUNDS = 4000;

	public const int DELAY_CLIENT_PICK = 5000;
	public const int DELAY_CLIENT_NEXTPICK = 8000;

	public const int LENGTH_FIRSTROUND_TOTAL = 50000;

	public const int POINTS_BASIC_REGION = 200;
	public const int POINTS_HIGH_VALUE_REGION = 400;
	public const int POINTS_DEFENDER_WIN = 100;

	public const string CONFIG_FILENAME = "config.cfg";
	public const string QUESTIONS_ABCD_FILENAME = "questionsABCD.txt";
	public const string QUESTIONS_NUMS_FILENAME = "questionsNumber.txt";

	public const string SERVER_LISTEN = "Server listening at {0}:{1}";
	public const string SERVER_ERROR = "An error occured. Is your config file valid?";
	public const string SERVER_USING_DEFAULT = "Using default settings - Server listening at {0}:{1}";

	public const string DEFAULT_SERVER_HOSTNAME = "127.0.0.1";
	public const int DEFAULT_SERVER_PORT = 13000;

}

