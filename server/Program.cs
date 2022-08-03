using System.Net;
using System.Net.Sockets;

namespace server
{
    static class Program
    {
        static void CreateServer()
        {
            Server s;
            s = new Server();
            try
            {
                s.Start();
            }
            catch (Exception e)
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

        static void Main(string[] args)
        {
            CreateServer();
        }
    }
}