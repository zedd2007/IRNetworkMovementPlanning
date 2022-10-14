using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class Train
    {
        public long id;       
        public long origTrackId;
        public long destTrackId;
        public List<long> stopLocations;
        public string direction; 
        public double length;
        public double speed;
        public Dictionary<long, long> nextLocationSequence;
        public double priority;
        public int schedDepTime;
        public bool isLive;

        public Train(long id, long origTrackId, long destTrackId, string direction, double length, double speed, int schedDepTime, List<long> stopLocations)
        {
            this.id = id;
            this.origTrackId = origTrackId;
            this.destTrackId = destTrackId;
            this.stopLocations = stopLocations;
            this.direction = direction;
            this.length = length;
            this.speed = speed;
            this.nextLocationSequence = new();
            this.priority = 1;
            this.schedDepTime = schedDepTime;
            if (this.schedDepTime == 0)
            {
                this.isLive = true;
            }
            else
            {
                this.isLive = false;
            }

            
        }

        public List<long> GetPath(Global glb, List<long> prohibitedTracks)
        {           
            PathSearch.Tree tr = new PathSearch.Tree();
            bool isBlocked = false;
            if (prohibitedTracks.Count > 0)
            {
                isBlocked = tr.IsRouteBlocked(glb.network, prohibitedTracks);

            }
       

            List<long> path = new();
            //isBlocked = false;
            if (!isBlocked)
            {
                path = tr.SearchPath(this.origTrackId, this.destTrackId, this.direction, glb.network, this.nextLocationSequence, prohibitedTracks, this.stopLocations);

            } 
            
            
            return path;
        }

        public List<long> GetRoute(long origLocationId, long destLocationId, Infrastructure.Network network)
        {            
            RouteSearch.Tree tr = new RouteSearch.Tree();
            List<long> route = tr.SearchRoute(origLocationId, destLocationId, network);
            return route;
        }
    }
}
