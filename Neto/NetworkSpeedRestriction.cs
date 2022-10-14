using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class NetworkSpeedRestriction
    {
        public int id;
        public double speedLimit;
        public int startTime;
        public int endTime;
        public List<int> trackIds;

        public NetworkSpeedRestriction(int id, double speedLimit, int startTime, int endTime, List<int> trackIds)
        {
            this.id = id;
            this.speedLimit = speedLimit;
            this.startTime = startTime;
            this.endTime = endTime;
            this.trackIds = trackIds;
        }
    }

    public class Extent
    {
        public int trackId;
        public double startKM;
        public double endKM;

        public Extent(int trackId, double startKM, double endKM)
        {
            this.trackId = trackId;
            this.startKM = startKM;
            this.endKM = endKM;
        }
    }
}
