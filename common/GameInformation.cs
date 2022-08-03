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
        for(int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            this.Points[i] = Constants.POINTS_START;
        }

        this.Regions = new List<Constants.Region>[Constants.MAX_PLAYERS];
        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            this.Regions[i] = new List<Constants.Region>();
        }

        this.BaseHealths = new int[Constants.MAX_PLAYERS];
        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            this.BaseHealths[i] = Constants.BASE_HP;
        }

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

        for(int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            sb.Append(Points[i]);
            sb.Append(Constants.GLOBAL_DELIMITER);
        }

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            sb.Append(BaseHealths[i]);
            sb.Append(Constants.GLOBAL_DELIMITER);
        }

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            sb.Append(Bases[i]);
            sb.Append(Constants.GLOBAL_DELIMITER);
        }

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

        int dataIndex = 1;

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            this.Points[i] = Int32.Parse(data[dataIndex]);
            dataIndex++;
        }

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            this.BaseHealths[i] = Int32.Parse(data[dataIndex]);
            dataIndex++;
        }

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            Enum.TryParse(data[dataIndex], out Constants.Region regBase);
            this.Bases[i] = regBase;
            dataIndex++;
        }

        for (int i = 0; i < Constants.MAX_PLAYERS; i++)
        {
            string[] regions = data[dataIndex].Split(',');

            List<Constants.Region> regionList = new List<Constants.Region>();

            foreach (string s in regions)
            {
                if (Enum.TryParse(s, out Constants.Region reg))
                {
                    regionList.Add(reg);
                }
            }

            this.Regions[i] = regionList;
            dataIndex++;
        }

        string[] hvregions = data[dataIndex].Split(',');
        foreach (string s in hvregions)
        {
            if (Enum.TryParse(s, out Constants.Region reg))
            {
                if (!this.HighValueRegions.Contains(reg)) this.HighValueRegions.Add(reg);
            }
        }
        dataIndex++;
    }
}
