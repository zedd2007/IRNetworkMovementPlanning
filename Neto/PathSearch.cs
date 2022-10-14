using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    namespace PathSearch
    {
        public class Node
        {
            public int id;
            public int parentId;
            public long trackId;
            public string prefDirection;
            public bool isVisited;
            public bool isExpanded;

            public List<int> children;

            public Node(int id, int parentId, long trackId, string prefDirection)
            {
                this.id = id;
                this.parentId = parentId;
                this.trackId = trackId;
                this.prefDirection = prefDirection;
                this.isVisited = false;
                this.isExpanded = false;
                this.children = new();
            }
        }

        public class Tree
        {
            public Dictionary<int, Node> nodes;
            public Tree()
            {
                this.nodes = new();
            }

            public bool IsRouteBlocked(Infrastructure.Network network, List<long> prohibitedTracks)
            {
                bool result = false;

                Dictionary<long, int> locationFreeTracks = new();

                foreach(long prohTrackId in prohibitedTracks)
                {
                    long prohLocId = network.tracks[prohTrackId].locationId;
                    if(!locationFreeTracks.ContainsKey(prohLocId))
                    {
                        locationFreeTracks.Add(prohLocId, network.locations[prohLocId].trackIds.Count);
                    }
                    locationFreeTracks[prohLocId] -= 1;

                    if (locationFreeTracks[prohLocId] == 0)
                    {
                        result = true;
                        break;
                    }
                }              

                

                return result;

            }

            public List<long> SearchPath(long origTrackId, long destTrackId, string direction, Infrastructure.Network network, Dictionary<long, long> pathNextLocation, List<long> prohibitedTracks, List<long> stopLocations)
            {
                List<long> path = new();
                int globalNodeIdCounter = 0;

                if(!prohibitedTracks.Contains(origTrackId))
                {
                    this.nodes.Add(0, new Node(0, -1, origTrackId, network.tracks[origTrackId].prefDirection));


                    Node currNode = this.nodes[0];

                    
                    int iteration = 0;

                    while ((currNode.trackId != destTrackId || network.tracks[currNode.trackId].locationId != stopLocations[^1]) && iteration <= Parameters.Constants.maxPathSearchIterations)
                    {
                        iteration++;
                        currNode.isVisited = true;
                        string currNodeTrackName = network.tracks[currNode.trackId].name;

                        if (!currNode.isExpanded)
                        {
                            long currLocId = network.tracks[currNode.trackId].locationId;
                            long nextLocId = pathNextLocation[currLocId];
                            foreach (KeyValuePair<long, string> neighTrack in network.tracks[currNode.trackId].neighbourTracks)
                            {

                                if ((network.tracks[neighTrack.Key].locationId == nextLocId || network.tracks[neighTrack.Key].locationId == currLocId) && !prohibitedTracks.Contains(neighTrack.Key))
                                {
                                    globalNodeIdCounter += 1;
                                    this.nodes.Add(globalNodeIdCounter, new Node(globalNodeIdCounter, currNode.id, neighTrack.Key, network.tracks[neighTrack.Key].prefDirection));
                                    this.nodes[currNode.id].children.Add(globalNodeIdCounter);
                                }

                            }
                            currNode.isExpanded = true;

                        }

                        List<Node> unvisitedChildren = new();
                        foreach (int childNodeId in currNode.children)
                        {
                            if (!this.nodes[childNodeId].isVisited)
                            {
                                unvisitedChildren.Add(this.nodes[childNodeId]);
                            }
                        }

                        if (unvisitedChildren.Count > 0)
                        {
                            unvisitedChildren.Sort((x, y) => x.prefDirection.CompareTo(y.prefDirection));

                            int nextNodeId;
                            if (direction == "UP")
                            {
                                nextNodeId = unvisitedChildren[^1].id;
                            }
                            else
                            {
                                nextNodeId = unvisitedChildren[0].id;
                            }

                            currNode = this.nodes[nextNodeId];

                        }
                        else
                        {
                            if (currNode.parentId != -1)
                            {
                                currNode = this.nodes[currNode.parentId];
                            }
                            else
                            {
                                break;
                            }

                        }

                    }

                    if (currNode.id != 0)
                    {
                        while (currNode.id != 0)
                        {
                            path.Add(currNode.trackId);
                            currNode = this.nodes[currNode.parentId];
                        }
                        path.Add(currNode.trackId);

                        path.Reverse();
                    }
                }
                
                

                return path;

            }
        
        
        }
    }
}

