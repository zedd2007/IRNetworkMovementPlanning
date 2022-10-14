using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class NetworkMaintenance
    {
        public long id;
        public int startTime;
        public int endTime;
        public List<long> tracks;

        public NetworkMaintenance(long id, int startTime, int endTime, List<long> tracks)
        {
            this.id = id;
            this.startTime = startTime;
            this.endTime = endTime;
            this.tracks = tracks;
        }
    }
}
