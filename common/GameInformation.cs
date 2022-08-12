using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons
{
    /// <summary>
    /// This class contains all needed game information. Both server and client share this state and update it regularly.
    /// </summary>
    public class GameInformation
    {
        public int[] Points { get; set; }

        public List<Constants.Region>[] Regions { get; set; }

        public int[] BaseHealths { get; set; }

        public Constants.Region[] Bases { get; set; }

        public List<Constants.Region> HighValueRegions { get; set; }

        public GameInformation()
        {
            this.Points = new int[Constants.MAX_PLAYERS];
            for (int i = 0; i < Constants.MAX_PLAYERS; i++)
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

        /// <summary>
        /// Method modyfing the Bases property. Sets the player's base region.
        /// </summary>
        /// <param name="playerID">Player identifier.</param>
        /// <param name="region">Region to be set as the base.</param>
        public void SetBase(int playerID, Constants.Region region)
        {
            this.Bases[playerID - 1] = region;
            if (!this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Add(region);
        }

        /// <summary>
        /// Method adding "pts" to the player's Points property.
        /// </summary>
        /// <param name="playerID">Player identifier.</param>
        /// <param name="pts">Number of points.</param>
        public void AddPoints(int playerID, int pts)
        {
            this.Points[playerID - 1] += pts;
        }

        /// <summary>
        /// Method adding the region to the player's regions.
        /// </summary>
        /// <param name="playerID">Player identifier.</param>
        /// <param name="region">Region to be added.</param>
        public void AddRegion(int playerID, Constants.Region region)
        {
            if (!this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Add(region);
        }

        /// <summary>
        /// Method appending the region to the high value regions.
        /// </summary>
        /// <param name="region">Region to be added.</param>
        public void AddHighValueRegion(Constants.Region region)
        {
            if (!this.HighValueRegions.Contains(region)) this.HighValueRegions.Add(region);
        }

        /// <summary>
        /// Method removing the region from player's regions.
        /// </summary>
        /// <param name="playerID">Player identifier.</param>
        /// <param name="region">Region to be removed.</param>
        public void RemoveRegion(int playerID, Constants.Region region)
        {
            if (this.Regions[playerID - 1].Contains(region)) this.Regions[playerID - 1].Remove(region);
        }

        /// <summary>
        /// Method decreasing player's base health by one.
        /// </summary>
        /// <param name="playerID">Player identifier.</param>
        public void DecreaseBaseHealth(int playerID)
        {
            this.BaseHealths[playerID - 1] -= 1;
        }
    }
}
