using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GameInformation
{
    public int P1Points { get; private set; }
    public int P2Points { get; private set; }

    public List<Constants.Regions> P1Regions { get; private set; }
    public List<Constants.Regions> P2Regions { get; private set; }

    public int P1BaseHealth { get; private set; }
    public int P2BaseHealth { get; private set; }

    public Constants.Regions P1Base { get; private set; }
    public Constants.Regions P2Base { get; private set; }

    public List<Constants.Regions> HighValueRegions { get; private set; }

    public GameInformation()
    {
        this.P1Points = 1000;
        this.P2Points = 1000;
        this.P1Regions = new List<Constants.Regions>();
        this.P2Regions = new List<Constants.Regions>();
        this.HighValueRegions = new List<Constants.Regions>();
        this.P1BaseHealth = 3;
        this.P2BaseHealth = 3;
    }

    public void setBase(int playerID, Constants.Regions region)
    {
        if (playerID == 1)
        {
            this.P1Base = region;
            this.P1Regions.Add(region);
        }
        else if (playerID == 2)
        {
            this.P2Base = region;
            this.P2Regions.Add(region);
        }
    }

    public string EncodeInformationToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Constants.PREFIX_GAMEUPDATE);
        //it could look something like this:
        //gameupdate_P1pts_P2pts_P1Health_P2Health_P1Base_P2Base_P1Regions_P2Regions_HighValueRegions
        //gameupdate_ 1500 _ 1700 _ 3 _ 2 _ 13 _ 9 _ 13,4,5, _ 9,6,7, _ 6,4,
        sb.Append(P1Points);
        sb.Append('_');

        sb.Append(P2Points);
        sb.Append('_');

        sb.Append(P1BaseHealth);
        sb.Append('_');

        sb.Append(P2BaseHealth);
        sb.Append('_');

        sb.Append(P1Base);
        sb.Append('_');

        sb.Append(P2Base);
        sb.Append('_');

        foreach(Constants.Regions region in P1Regions)
        {
            sb.Append(region);
            sb.Append(',');
        }
        sb.Append('_');

        foreach (Constants.Regions region in P2Regions)
        {
            sb.Append(region);
            sb.Append(',');
        }
        sb.Append('_');

        foreach (Constants.Regions region in HighValueRegions)
        {
            sb.Append(region);
            sb.Append(',');
        }

        return sb.ToString();
    }

    //method used by client to update their information
    public void UpdateGameInformationFromMessage(string message)
    {
        //it could look something like this:
        //gameupdate_P1pts_P2pts_P1Health_P2Health_P1Base_P2Base_P1Regions_P2Regions_HighValueRegions
        //gameupdate_ 1500 _ 1700 _ 3 _ 2 _ 13 _ 9 _ 13,4,5, _ 9,6,7, _ 6,4,
        string[] data = message.Split('_');

        this.P1Points = Int32.Parse(data[1]);
        this.P2Points = Int32.Parse(data[2]);

        this.P1BaseHealth = Int32.Parse(data[3]);
        this.P2BaseHealth = Int32.Parse(data[4]);

        Enum.TryParse(data[5], out Constants.Regions p1base);
        this.P1Base = p1base;

        Enum.TryParse(data[6], out Constants.Regions p2base);
        this.P2Base = p2base;

        string[] p1regions = data[7].Split(',');
        foreach(string s in p1regions)
        {
            if(Enum.TryParse(s, out Constants.Regions reg))
            {
                this.P1Regions.Add(reg);
            }
        }

        string[] p2regions = data[8].Split(',');
        foreach (string s in p2regions)
        {
            if (Enum.TryParse(s, out Constants.Regions reg))
            {
                this.P2Regions.Add(reg);
            }
        }

        string[] hvregions = data[9].Split(',');
        foreach (string s in hvregions)
        {
            if (Enum.TryParse(s, out Constants.Regions reg))
            {
                this.HighValueRegions.Add(reg);
            }
        }
    }
}
