using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GameInformation
{
    public int[] Points { get; private set; }

    public List<Constants.Region>[] Regions { get; private set; }

    public int[] BaseHealths { get; private set; }

    public Constants.Region[] Bases { get; private set; }

    public List<Constants.Region> HighValueRegions { get; private set; }

    public GameInformation()
    {
        this.Points = new int[Constants.MAX_PLAYERS];
        this.Points[0] = 1000;
        this.Points[1] = 1000;

        this.Regions = new List<Constants.Region>[Constants.MAX_PLAYERS];
        this.Regions[0] = new List<Constants.Region>();
        this.Regions[1] = new List<Constants.Region>();

        this.BaseHealths = new int[Constants.MAX_PLAYERS];
        this.BaseHealths[0] = 3;
        this.BaseHealths[1] = 3;

        this.Bases = new Constants.Region[Constants.MAX_PLAYERS];

        this.HighValueRegions = new List<Constants.Region>();
    }

    public void setBase(int playerID, Constants.Region region)
    { 
        this.Bases[playerID - 1] = region;
        if(!this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Add(region);
    }

    public void addPoints(int playerID, int pts)
    {
        this.Points[playerID - 1] += pts;
    }

    public void addRegion(int playerID, Constants.Region region)
    {
        if(!this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Add(region);
    }

    public void addHighValueRegion(Constants.Region region)
    {
        if (!this.HighValueRegions.Contains(region)) this.HighValueRegions.Add(region);
    }

    public void removeRegion(int playerID, Constants.Region region)
    {
        if (this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Remove(region);
    }

    public void decreaseBaseHealth(int playerID)
    {
        this.BaseHealths[playerID - 1] -= 1;
    }

    public string EncodeInformationToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Constants.PREFIX_GAMEUPDATE);
        //it could look something like this:
        //gameupdate_P1pts_P2pts_P1Health_P2Health_P1Base_P2Base_P1Regions_P2Regions_HighValueRegions
        //gameupdate_ 1500 _ 1700 _ 3 _ 2 _ 13 _ 9 _ 13,4,5, _ 9,6,7, _ 6,4,
        sb.Append(Points[0]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        sb.Append(Points[1]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        sb.Append(BaseHealths[0]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        sb.Append(BaseHealths[1]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        sb.Append(Bases[0]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        sb.Append(Bases[1]);
        sb.Append(Constants.GLOBAL_DELIMITER);

        foreach(Constants.Region region in Regions[0])
        {
            sb.Append(region);
            sb.Append(',');
        }
        sb.Append(Constants.GLOBAL_DELIMITER);

        foreach (Constants.Region region in Regions[1])
        {
            sb.Append(region);
            sb.Append(',');
        }
        sb.Append(Constants.GLOBAL_DELIMITER);

        foreach (Constants.Region region in HighValueRegions)
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
        string[] data = message.Split(Constants.GLOBAL_DELIMITER);

        this.Points[0] = Int32.Parse(data[1]);
        this.Points[1] = Int32.Parse(data[2]);

        this.BaseHealths[0] = Int32.Parse(data[3]);
        this.BaseHealths[1] = Int32.Parse(data[4]);

        Enum.TryParse(data[5], out Constants.Region p1base);
        this.Bases[0] = p1base;

        Enum.TryParse(data[6], out Constants.Region p2base);
        this.Bases[1] = p2base;

        string[] p1regions = data[7].Split(',');

        List<Constants.Region> p1regionsNew = new List<Constants.Region>();

        foreach(string s in p1regions)
        {
            if(Enum.TryParse(s, out Constants.Region reg))
            {
                p1regionsNew.Add(reg);
            }
        }
        this.Regions[0] = p1regionsNew;

        string[] p2regions = data[8].Split(',');

        List<Constants.Region> p2regionsNew = new List<Constants.Region>();

        foreach (string s in p2regions)
        {
            if (Enum.TryParse(s, out Constants.Region reg))
            {
                p2regionsNew.Add(reg);
            }
        }
        this.Regions[1] = p2regionsNew;

        string[] hvregions = data[9].Split(',');
        foreach (string s in hvregions)
        {
            if (Enum.TryParse(s, out Constants.Region reg))
            {
                if (!this.HighValueRegions.Contains(reg)) this.HighValueRegions.Add(reg);
            }
        }
    }
}
