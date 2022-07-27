using System;

public static class Constants
{
	public enum Regions {
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

	public static Regions[][] NEIGHBORING_REGIONS => new Regions[14][] { 

		new Regions[] { Regions.CZPL, Regions.CZST, Regions.CZVY, Regions.CZJM},
		new Regions[] { Regions.CZJC, Regions.CZVY, Regions.CZPA, Regions.CZOL, Regions.CZZL },
		new Regions[] { Regions.CZPL, Regions.CZUS },
		new Regions[] { Regions.CZPA, Regions.CZST, Regions.CZLI },

		new Regions[] { Regions.CZST, Regions.CZUS, Regions.CZKR },
		new Regions[] { Regions.CZOL, Regions.CZZL },
		new Regions[] { Regions.CZPA, Regions.CZJM, Regions.CZZL, Regions.CZMO },
		new Regions[] { Regions.CZKR, Regions.CZST, Regions.CZVY, Regions.CZJM, Regions.CZOL},

		new Regions[] { Regions.CZMO, Regions.CZOL, Regions.CZJM },
		new Regions[] { Regions.CZJC, Regions.CZST, Regions.CZUS, Regions.CZKA },
		new Regions[] { Regions.CZST },
		new Regions[] { Regions.CZUS, Regions.CZLI, Regions.CZPR, Regions.CZKR, Regions.CZPA, Regions.CZVY, Regions.CZJC, Regions.CZPL },

		new Regions[] { Regions.CZKA, Regions.CZLI, Regions.CZST, Regions.CZPL },
		new Regions[] { Regions.CZST, Regions.CZPA, Regions.CZJC, Regions.CZJM },

	};

	public const int MAX_PLAYERS = 2;

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
}

