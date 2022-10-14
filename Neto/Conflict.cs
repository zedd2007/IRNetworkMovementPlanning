using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class Conflict
    {
        public int id;
        public Dictionary<long, Neto.Occupation> overlappingOccupations;
        public long trackId;
        public int time;
        public int type; // 0 means Same, 1 means Opposite, 2 means Obstruction

        
        public Conflict(int id, Dictionary<long, Neto.Occupation> overlappingOccupations, long trackId, int time, int type)
        {
            this.id = id;
            this.overlappingOccupations = overlappingOccupations;
            this.trackId = trackId;
            this.time = time;
            this.type = type;
        }

        
        public string Print(Global glb)
        {
            string result;

            //string entities = string.Join(";", this.overlappingOccupations.Keys.ToList());
            string zone = glb.network.tracks[this.trackId].name; 
            string time = this.time.ToString();
            string type;
            if (this.type == 0)
            {
                type = "same";
            }
            else
            {
                if(this.type == 1)
                {
                    type = "opp";
                }
                else
                {
                    type = "maint";
                }
                
            }
            string conflictPrint = "";
            foreach (KeyValuePair<long, Occupation> kvp in this.overlappingOccupations)
            {
                conflictPrint += kvp.Value.Print() + ";";
            }

            result = conflictPrint + "|" + zone + "|" + time + "|" + type;
            return result;
        }
    }
}
