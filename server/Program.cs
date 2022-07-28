using System.Net;
using System.Net.Sockets;

namespace server
{
    static class Program
    {
        static void Main(string[] args)
        {
            /*List<Constants.Region> picker = new List<Constants.Region>() { Constants.Region.CZVY, Constants.Region.CZJC };
            List<Constants.Region> second = new List<Constants.Region>() { Constants.Region.CZST, Constants.Region.CZPR };
            List<Constants.Region>[] all = new List<Constants.Region>[2] { picker, second };

            Console.WriteLine(Constants.PickRandomFreeNeighboringRegion(picker, all, 0));
            */
            Server s = new Server();
            s.Start();
        }
    }
}