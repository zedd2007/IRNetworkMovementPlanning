using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class Occupation
    {
        public long entityId;
        public long trackId;
        public int startTime;
        public int endTime;
        public int trainTravelTime;
        public int movementId;

        public Occupation(long entityId, long trackId, int startTime, int endTime, int trainTravelTime, int movementId)
        {
            this.entityId = entityId;
            this.trackId = trackId;
            this.startTime = startTime;
            this.endTime = endTime;
            this.trainTravelTime = trainTravelTime;
            this.movementId = movementId;
        }

        public Occupation DeepCopy()
        {
            Occupation copyOcc = (Occupation)this.MemberwiseClone();
            return copyOcc;
        }

        public string Print()
        {
            string result = this.entityId.ToString() + "_" + this.startTime.ToString() + "_" + this.endTime.ToString();
            return result;
        }
    }
}
