using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Commons
{
    public class BasicMessage
    {
        /*
        {
            "type": string
            "playerid": string|null
        }
        */
        //used for game over, disconnect, attack, picked, pickregion, assign

        /*
		public const string PREFIX_ASSIGN = "assign_";
        assign_p1

		public const string PREFIX_PICKREGION = "pickregion_";
        pickregion_2

		public const string PREFIX_PICKED = "picked_";
        picked_1_CZST

		public const string PREFIX_ATTACK = "attack_";
        attack_CZST

		public const string PREFIX_DISCONNECTED = "disconnected_";
		disconnected_-1
        
        public const string PREFIX_GAMEOVER = "gameover_";
        gameover_2

        public const string P1CONNECTED = "You have been connected, waiting for player 2.";
		public const string P2CONNECTED = "Both players have connected, game starting soon.";

        public const string PREFIX_GAMEUPDATE = "gameupdate_";
        gameupdate_...gameInformation

        public const string PREFIX_QUESTIONABCD = "questionAW_";
        questionAW_...question

        public const string PREFIX_QUESTIONNUMBER = "questionNUM_";
        questionNUM_...question

        public const string PREFIX_ANSWER = "answer_";
        answer_1_tip_time

        public const string PREFIX_FINALANSWERS = "finalanswers_";
        finalanswers_p1ans_p2ans_p1time_p2time_correct_playerID
        */

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("playerid")]
        public string? PlayerID;

        [JsonProperty("region")]
        public Constants.Region? Region;

        [JsonProperty("gameinformation")]
        public GameInformation? GameInformation;

        [JsonProperty("questionabcd")]
        public QuestionABCD? QuestionABCD;

        [JsonProperty("questionnumeric")]
        public QuestionNumeric? QuestionNumeric;

        [JsonProperty("correct")]
        public string? Correct;

        [JsonProperty("p1time")]
        public string? P1Time;

        [JsonProperty("p2time")]
        public string? P2Time;

        [JsonProperty("p1ans")]
        public string? P1Ans;

        [JsonProperty("p2ans")]
        public string? P2Ans;
    }

    public class QuestionABCD
    {
        [JsonProperty("content")]
        public string Content;

        [JsonProperty("correct")]
        public string Correct;

        [JsonProperty("answers")]
        public string[] Answers;
    }

    public class QuestionNumeric
    {
        [JsonProperty("content")]
        public string Content;

        [JsonProperty("correct")]
        public string Correct;
    }
}