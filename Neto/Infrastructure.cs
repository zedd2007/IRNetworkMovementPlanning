using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Neto
{
    namespace Infrastructure
    {
        
        public class Track
        {
            public long id { get; set; }
            public string name { get; set; }            

            public string node1ToNode2Direction { get; set; }            

            public double length { get; set; }

            public double startKmMark { get; set; }

            public double endKmMark { get; set; }


            public double speedLimit { get; set; }

            public string prefDirection { get; set; }
             
            public long blockId { get; set; }

            public long boardId { get; set; }

            public long locationId { get; set; }

            public long divisionId { get; set; }

            public Dictionary<long, string> neighbourTracks { get; set; }

            public int searchNodeId { get; set; }

            public Track(long id, string name, double length, string prefDirection, long locationId, long divisionId, string node1ToNode2Direction)
            {
                this.id = id;
                this.name = name;
                this.length = length;
                this.speedLimit = Parameters.Constants.maxTrackSpeedLimit;
                this.prefDirection = prefDirection;
                this.locationId = locationId;
                this.neighbourTracks = new();
                this.searchNodeId = -1;
                this.divisionId = divisionId;                
                this.node1ToNode2Direction = node1ToNode2Direction;
                

            }
        }

        public class Location
        {
            public long id { get; set; }

            public string name { get; set; }

            public List<long> trackIds { get; set; }

            public double startKmMark { get; set; }

            public double endKmMark { get; set; }

            public List<long> neighbours { get; set; }

            public string type { get; set; }

            public Location(long id, string name, string type)
            {
                this.id = id;
                this.name = name;
                this.trackIds = new();
                this.neighbours = new();
                this.type = type;
            }
        }

        public class Block
        {
            public long id { get; set; }
            public long boardId { get; set; }
            public Block(long id, long boardId)
            {
                this.id = id;
                this.boardId = boardId;
            }
        }
        public class Board
        {
            public long id { get; set; }
            public string name { get; set; }
            public List<long> blockIds { get; set; }

            public Board(long id, string name, List<long> blockIds)
            {
                this.id = id;
                this.name = name;
                this.blockIds = blockIds;
            }
        }

        public class Network
        {
            public Dictionary<long, Track> tracks { get; set; }

            public Dictionary<string, long> trackNames { get; set; }


            public Dictionary<long, Location> locations { get; set; }

            public Dictionary<string, long> locationNames { get; set; }

            public Dictionary<long, Block> blocks { get; set; }

            public Dictionary<long, Board> boards { get; set; }

            public Network()
            {
                this.tracks = new();
                this.trackNames = new();
                this.locations = new();
                this.locationNames = new();
                this.blocks = new();
                this.boards = new();
            }
        }


    }
}

