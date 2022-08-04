using System.Net;
using System.Net.Sockets;
using Commons;

namespace server
{
    static class Program
    {
        /// <summary>
        /// Static method to create a new instance of the server. Handles disconnect and other possible problems.
        /// </summary>
        static void CreateServer()
        {
            Server s;
            s = new Server();
            try
            {
                s.Start();
            }
            catch (Exception)
            {
                //a player disconnected... it is better to create a new server.
            }
            finally
            {
                s.Stop();
                Console.WriteLine(Constants.SERVER_RESET_DISCONNECT);
                CreateServer();
            }
        }

        /// <summary>
        /// Main entry point to the server.
        /// </summary>
        static void Main()
        {
            CreateServer();
        }
    }
}