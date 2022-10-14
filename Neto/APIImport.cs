using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    namespace APIImport
    {
        public class Node
        {
            public long id { get; set; }

            public string name { get; set; }

            public string type { get; set; }           


        }

        public class TrackUI
        {
            public long id { get; set; }

            public string type { get; set; }
            public string startKmMark { get; set; }
            public string endKmMark { get; set; }   
        }
        public class Track
        {
            public long id { get; set; }
            public string name { set; get; }
            public long node1Id { get; set; }
            public long node2Id { get; set; }
            public string node1ToNode2Direction { get; set; }
            public double length { get; set; }
            public long locationId { get; set; }
            public long divisionId { get; set; }
            public string trackType { get; set; }
            public string trackDirectionPreference { get; set; }
            public double speed { get; set; }


        }

        public class Circuit
        {
            public long id { get; set; }
            //public long blockLocationId { get; set; }
            public string trackType { get; set; }

            public string km1Label1 { get; set; }

            public string km1Label2 { get; set; }

            public long blockId { get; set; }

        }

        public class Location
        {
            public long id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }

        public class Block
        {
            public long id { get; set; }
            public string name { get; set; }

            public List<Circuit> circuits { get; set; }

        }

        public class Board
        {
            public long id { get; set; }
            public string viewName { get; set; }

            public List<ShortBlock> blocks { get; set; }

        }

        public class NeighbouringLocation
        {
            public long id { get; set; }
            public List<ShortLocation> neighboringLocations { get; set; }

        }


        public class ShortBlock
        {
            public long blockId { get; set; }
        }


        public class ShortLocation
        {
            public long locationId { get; set; }
            public int distance { get; set; }

            public string direction { get; set; }

        }

        public class ShortTrack
        {
            public long trackId { get; set; }            

            public string direction { get; set; }

        }

        public class NeighbourTrackArcDTO
        {
            public ShortTrack destinationTrackArc { get; set; }
        }
        public class TrackArcNeighbour
        {
            public ShortTrack originTrackArc { get; set; }
            public List<NeighbourTrackArcDTO> neighborTrackArcDtos { get; set;} 
        }

        public class Generic
        {
            public Generic()
            {

            }
        }

        



    }
   
}
