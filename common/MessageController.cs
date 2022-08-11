using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Commons
{
    public static class MessageController
    {

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
                Correct = correct,
                P1Time = p1time,
                P2Time = p2time,
                P1Ans = p1ans,
                P2Ans = p2ans
            };

            string converted = JsonConvert.SerializeObject(message, Formatting.Indented);
            int finalLength = 8 + converted.Length;
            string finalMessage = converted.Insert(0, String.Format("{0:D8}", finalLength));

            return finalMessage;
        }


        /// <summary>
        /// TODO: edit this
        /// </summary>
        public static string ReceiveMessage(NetworkStream stream)
        {
            string response = "";

            byte[] data = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int numBytesRead;

                int size = stream.Read(data, 0, 8); //read first 8 bytes to know the size of the message
                string supposedBytes = Encoding.ASCII.GetString(data, 0, size);
                //string supposedBytes = "56";

                int totalRead = 8;

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
                    } while (stream.DataAvailable);
                    response = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                }
            }
            //here str is the response
            return response;

            /*
            while (true) //wait for the ID assignment
            {
                bytes = _stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Debug.WriteLine("Received: " + responseData);
                if (responseData.Contains(Constants.PREFIX_DISCONNECTED))
                {
                    ClientCommon.HandleEnemyDisconnect();
                }
                else if (responseData.StartsWith(Constants.PREFIX_ASSIGN)) //got an id assigned
                {
                    char lastChar = responseData[^1];
                    this._clientID = Int32.Parse(lastChar.ToString());
                    this.playerIDLabel.Content = String.Format(Constants.PLAYER_ID_LABEL, lastChar);
                    break;
                }
            }
            */
        }
    }
}
