using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Neto
{
    namespace RouteSearch
    {
        public class Node
        {
            public long id;
            public long parentId;
            public bool isVisited;
            public double distance;
            public Node(long id, long parentId)
            {
                this.id = id;
                this.parentId = parentId;
                this.isVisited = false;
                this.distance = 0;
            }
        }
        public class Tree
        {
            public Dictionary<long, Node> nodes;
            List<Node> openNodes;
            public Tree()
            {
                this.nodes = new();
                this.openNodes = new();
            }

            public List<long> SearchRoute(long origLocationId, long destLocationId, Infrastructure.Network network)
            {
                List<long> result = new();

                Node initNode = new Node(origLocationId, -1);
                this.nodes.Add(initNode.id, initNode);
                this.openNodes.Add(initNode);

                Node currNode = this.openNodes[^1];
                this.openNodes.RemoveAt(this.openNodes.Count - 1);


                while (currNode.id != destLocationId)
                {
                    currNode.isVisited = true;
                    foreach (long neighLocId in network.locations[currNode.id].neighbours)
                    {

                        if (!this.nodes.ContainsKey(neighLocId))
                        {
                            this.nodes.Add(neighLocId, new Node(neighLocId, currNode.id));
                        }

                        if (!this.nodes[neighLocId].isVisited)
                        {

                            this.nodes[neighLocId].distance = currNode.distance + network.tracks[network.locations[neighLocId].trackIds[0]].length;
                            this.openNodes.Add(this.nodes[neighLocId]);
                        }
                    }

                    this.openNodes.Sort((x, y) => y.distance.CompareTo(x.distance));
                    currNode = this.openNodes[^1];
                    this.openNodes.RemoveAt(this.openNodes.Count - 1);
                }

                long currNodeId = destLocationId;

                while (currNodeId != -1)
                {
                    result.Add(currNodeId);
                    currNodeId = this.nodes[currNodeId].parentId;
                }

                result.Reverse();

                return result;
            }
        }
    }
}

