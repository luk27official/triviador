using System;
using System.Collections.Generic;
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
		List<Region> populatedRegions = new List<Region>();
		foreach (var list in all)
		{
			populatedRegions.AddRange(list);
		}
		if (populatedRegions.Count == REGION_COUNT) return null;

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

	public const int MAX_PLAYERS = 2;
	public const int REGION_COUNT = 14;

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

	public const int DELAY_BETWEEN_ROUNDS = 3000;

	public const int DELAY_CLIENT_PICK = 5000;
	public const int DELAY_CLIENT_NEXTPICK = 8000;

	public const int LENGTH_FIRSTROUND_TOTAL = 50000;

	public const int POINTS_FIRSTROUND = 200;
}

