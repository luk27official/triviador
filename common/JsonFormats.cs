using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Commons
{
    /// <summary>
    /// This class provides the JSON format for basic messages.
    /// </summary>
    public class BasicMessage
    {
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

        [JsonProperty("answerdetails")]
        public AnswerDetails? AnswerDetails;
    }

    /// <summary>
    /// This class provides the JSON format for details about the answer used in messages.
    /// </summary>
    public class AnswerDetails
    {
        [JsonProperty("correct")]
        public string? Correct;

        [JsonProperty("times")]
        public string?[]? Times;

        [JsonProperty("answers")]
        public string?[]? Answers;
    }

    /// <summary>
    /// This class provides the JSON format for question with 4 options.
    /// </summary>
    public class QuestionABCD
    {
        [JsonProperty("content")]
        public string Content;

        [JsonProperty("correct")]
        public string Correct;

        [JsonProperty("answers")]
        public string[] Answers;
    }

    /// <summary>
    /// This class provides the JSON format for numeric questions.
    /// </summary>
    public class QuestionNumeric
    {
        [JsonProperty("content")]
        public string Content;

        [JsonProperty("correct")]
        public string Correct;
    }
}