using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Commons
{
    /// <summary>
    /// Class which encapsulates encoding of messages to JSON format and receiving messages.
    /// </summary>
    public static class MessageController
    {
        public const int FIXED_PREFIX_SIZE = 8;

        /// <summary>
        /// Method which encodes the information provided via the function parameters to JSON.
        /// </summary>
        /// <param name="type">Message type.</param>
        /// <param name="playerID">Player/winner/loser identificator. May be used in more ways.</param>
        /// <param name="region">Attacked/picked region. May be used in more ways.</param>
        /// <param name="gameInformation">Game information, if available..</param>
        /// <param name="questionABCD">Question with 4 options, if available.</param>
        /// <param name="questionNumeric">Question with numeric answer, if available.</param>
        /// <param name="correct">Correct answer for the question.</param>
        /// <param name="p1time">Player 1 answer time.</param>
        /// <param name="p2time">Player 2 answer time.</param>
        /// <param name="p1ans">Player 1 answer.</param>
        /// <param name="p2ans">Player 2 answer.</param>
        /// <returns>Encoded message in JSON format with a length prefix of 8 bytes.</returns>
        public static string EncodeMessageIntoJSONWithPrefix(string type, string? playerID = null, Constants.Region? region = null, GameInformation? gameInformation = null,
            QuestionABCD? questionABCD = null, QuestionNumeric? questionNumeric = null, string? correct = null, string? p1time = null, string? p2time = null, string? p1ans = null, string? p2ans = null)
        {
            BasicMessage message = new BasicMessage
            {
                Type = type,
                PlayerID = playerID,
                Region = region,
                GameInformation = gameInformation,
                QuestionABCD = questionABCD,
                QuestionNumeric = questionNumeric,
                AnswerDetails = new AnswerDetails
                {
                    Correct = correct,
                    Answers = new string?[]
                    {
                        p1ans,
                        p2ans
                    },
                    Times = new string?[]
                    { 
                        p1time,
                        p2time
                    }
                }
            };

            string converted = JsonConvert.SerializeObject(message, Formatting.Indented);
            int finalLength = FIXED_PREFIX_SIZE + converted.Length; //fixed format of 8 bytes
            string finalMessage = converted.Insert(0, String.Format("{0:D8}", finalLength)); 

            return finalMessage;
        }


        /// <summary>
        /// Method which handles receiving messages.
        /// </summary>
        public static string ReceiveMessage(NetworkStream stream)
        {
            string response = "";

            byte[] data = new byte[Constants.DEFAULT_BUFFER_SIZE];
            using (MemoryStream ms = new MemoryStream())
            {
                int numBytesRead;

                int size = stream.Read(data, 0, FIXED_PREFIX_SIZE); //read first 8 bytes to know the size of the message
                string supposedBytes = Encoding.ASCII.GetString(data, 0, size);

                int totalRead = FIXED_PREFIX_SIZE;

                if (Int32.TryParse(supposedBytes, out int supBytes))
                {
                    do
                    {
                        if (data.Length > (supBytes - totalRead))
                        {
                            numBytesRead = stream.Read(data, 0, (supBytes - totalRead));
                            ms.Write(data, 0, numBytesRead);
                            totalRead += numBytesRead;
                            break;
                        }
                        else
                        {
                            numBytesRead = stream.Read(data, 0, data.Length);
                            ms.Write(data, 0, numBytesRead);
                            totalRead += numBytesRead;
                        }
                    } while (stream.DataAvailable || supBytes > totalRead);
                    response = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                }
            }
            return response;
        }
    }
}
