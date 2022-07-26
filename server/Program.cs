using System.Net;
using System.Net.Sockets;

namespace server
{
    static class Program
    {
        static void Main(string[] args)
        {
            Server s = new Server();
            s.Start();
        }
    }
}