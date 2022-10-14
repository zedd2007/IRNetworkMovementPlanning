using Neto.APIImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    public class DataImporter
    {
        public async Task ImportNetworkFromAPI(Infrastructure.Network network, string environment)
        {
            string environmentUrl = "";

            switch(environment)
            {
                case "DEV":
                    environmentUrl = "https://dev.railmax.io";
                    break;

                case "QA":
                    environmentUrl = "https://qa.railmax.io";
                    break;

            }
            string nodeUrl = environmentUrl + "/Network/Nodes";
            string trackUIUrl = environmentUrl + "/Network/Tracks?";
            string trackUrl = environmentUrl + "/Network/Engine/Tracks";
            string locationUrl = environmentUrl + "/Network/Engine/Locations";
            string neighbouringLocationUrl = environmentUrl + "/Network/Engine/NeighbouringLocations";
            string neighbouringTrackUrl = environmentUrl + "/Network/Engine/TrackArcNeighbours";
            string blockUrl = environmentUrl + "/Network/Block";
            string boardUrl = environmentUrl + "/Network/BlockView";

            using var client = new HttpClient();

            var nodData = await client.GetAsync(nodeUrl);
            var nodes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Node>>(nodData.Content.ReadAsStringAsync().Result);

            var traUIData = await client.GetAsync(trackUIUrl);
            var tracksUI = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.TrackUI>>(traUIData.Content.ReadAsStringAsync().Result);

            var traData = await client.GetAsync(trackUrl);
            var tracks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Track>>(traData.Content.ReadAsStringAsync().Result);

            var locData = await client.GetAsync(locationUrl);
            var locations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Location>>(locData.Content.ReadAsStringAsync().Result);

            var neighLocData = await client.GetAsync(neighbouringLocationUrl);
            var neighLocs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.NeighbouringLocation>>(neighLocData.Content.ReadAsStringAsync().Result);

            var neighTrkData = await client.GetAsync(neighbouringTrackUrl);
            var neighTrks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.TrackArcNeighbour>>(neighTrkData.Content.ReadAsStringAsync().Result);

            var blkData = await client.GetAsync(blockUrl);
            var blks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Block>>(blkData.Content.ReadAsStringAsync().Result);

            var brdData = await client.GetAsync(boardUrl);
            var brds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Board>>(brdData.Content.ReadAsStringAsync().Result);


            Dictionary<long, APIImport.Track> allTracks = new();

            

            foreach(APIImport.Location loc in locations)
            {               
                network.locations.Add(loc.id, new Infrastructure.Location(loc.id, loc.name, loc.type));
                network.locationNames.Add(loc.name, loc.id);
            }

            foreach (APIImport.NeighbouringLocation loc in neighLocs)
            {
                foreach(APIImport.ShortLocation neighb in loc.neighboringLocations)
                {
                    network.locations[loc.id].neighbours.Add(neighb.locationId);
                }                
            }

            foreach(APIImport.Track trk in tracks)
            {
                allTracks.Add(trk.id, trk);
                if(trk.trackType != "SwitchEdge")
                {
                    network.tracks.Add(trk.id, new Infrastructure.Track(trk.id, trk.name, trk.length, trk.trackDirectionPreference, trk.locationId, trk.divisionId, trk.node1ToNode2Direction));
                    network.locations[trk.locationId].trackIds.Add(trk.id);
                    network.trackNames.Add(trk.name, trk.id);
                }
                
            }

            foreach(APIImport.TrackArcNeighbour trk in neighTrks)
            {
                foreach(APIImport.NeighbourTrackArcDTO neigh in trk.neighborTrackArcDtos)
                {
                    if(allTracks[trk.originTrackArc.trackId].trackType != "SwitchEdge" && allTracks[neigh.destinationTrackArc.trackId].trackType != "SwitchEdge")
                    {
                        network.tracks[trk.originTrackArc.trackId].neighbourTracks.Add(neigh.destinationTrackArc.trackId, neigh.destinationTrackArc.direction);
                    }
                    
                }
                
            }

            foreach (APIImport.Board brd in brds)
            {
                List<long> blockIds = new();


                foreach (APIImport.ShortBlock bl in brd.blocks)
                {
                    if(!network.blocks.ContainsKey(bl.blockId))
                    {
                        network.blocks.Add(bl.blockId, new Infrastructure.Block(bl.blockId, brd.id));

                    }
                    blockIds.Add(bl.blockId);
                    
                }
                network.boards.Add(brd.id, new Infrastructure.Board(brd.id, brd.viewName, blockIds));
            }

            Dictionary<long, List<string>> blockLocations = new();

            foreach (APIImport.Block blk in blks)
            {
                List<string> locs = new(); 
                foreach (APIImport.Circuit cir in blk.circuits)
                {
                    if (network.tracks.ContainsKey(cir.id))
                    {
                        long locId = network.tracks[cir.id].locationId;
                        string locName = network.locations[locId].name;

                        if(!locs.Contains(locName))
                        {
                            locs.Add(locName);
                        }
                    }
                }
                blockLocations.Add(blk.id, locs);
            }


            using (var w = new StreamWriter(@"C:\Users\ashwin\source\repos\Neto\Neto\board_location_mapping.csv"))
            {
                string firstLine = "BOARD_NAME" + "," + "LOCATION";
                w.WriteLine(firstLine);

                foreach (APIImport.Board brd in brds)
                {
                    List<string> locs = new();
                    foreach (ShortBlock shBlk in brd.blocks)
                    {
                        List<string> shBlkLocs = blockLocations[shBlk.blockId];
                        locs = locs.Union(shBlkLocs).ToList();
                    }
                    
                    foreach(string loc in locs)
                    {
                        string line = brd.viewName + "," + loc;
                        w.WriteLine(line);
                        w.Flush();
                    }
                    
                }

                
            }
            



                int count = 0;
            foreach (APIImport.TrackUI trUI in tracksUI)
            {
                if (trUI.type != "SwitchEdge")
                {
                    network.tracks[trUI.id].startKmMark = Convert.ToDouble(trUI.startKmMark);
                    network.tracks[trUI.id].endKmMark = Convert.ToDouble(trUI.endKmMark);
                    count += 1;
                }

            }            
            


        }
        

       

        public void InitialiseSpeedRestrictionData(string csvFilePath, Dictionary<int, NetworkSpeedRestriction> tsrs)
        {
            string[] csvLines = System.IO.File.ReadAllLines(csvFilePath);

            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] rowData = csvLines[i].Split(',');


                int tsrId = Convert.ToInt32(rowData[0]);
                double speedLimit = Convert.ToDouble(rowData[1]);
                int startTime = Convert.ToInt32(rowData[2]);
                int endTime = Convert.ToInt32(rowData[3]);
                List<int> tracks = new();

                foreach (string trackId in rowData[4].Split(";"))
                {
                    tracks.Add(Convert.ToInt32(trackId));
                }

                tsrs.Add(tsrId, new NetworkSpeedRestriction(tsrId,speedLimit, startTime, endTime, tracks));

            }

        }
        public void InitialiseTrainScheduleData(string csvFilePath, Dictionary<long, Train> trains, Infrastructure.Network network)
        {
            string[] csvLines = System.IO.File.ReadAllLines(csvFilePath);

            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] rowData = csvLines[i].Split(',');


                int trainId = Convert.ToInt32(rowData[0]);
                long origTrackId = network.trackNames[rowData[1]];//Convert.ToInt64(rowData[1]);                
                long destTrackId = network.trackNames[rowData[2]];//Convert.ToInt64(rowData[2]);
                List<long> stopLocations = new();
                foreach(string locName in rowData[5].Split(";"))
                {
                    stopLocations.Add(network.locationNames[locName]);
                }
                string direction = rowData[3];
                int depTime = Convert.ToInt32(rowData[4]);
                Dictionary<long, long> nextLocationSequence = new();           
                double length = Parameters.Constants.trainLength;
                double speed;              

                if(direction == "UP")
                {
                    speed = Parameters.Constants.upTrainSpeed;
                }
                else
                {
                    speed = Parameters.Constants.downTrainSpeed;
                }

                
                trains[trainId] = new Train(trainId, origTrackId, destTrackId, direction, length, speed, depTime, stopLocations);

                
                long origLocId = network.tracks[origTrackId].locationId;
                List<long> fullRoute = new List<long> { origLocId };
                foreach (long locId in trains[trainId].stopLocations)
                {
                    
                    List<long> route = trains[trainId].GetRoute(origLocId, locId, network);
                    for(int j = 1; j < route.Count; j++)
                    {
                        fullRoute.Add(route[j]);
                    }
                    origLocId = locId;

                }

                for (int k = 0; k < fullRoute.Count - 1; k++)
                {
                    nextLocationSequence.Add(fullRoute[k], fullRoute[k + 1]);
                }
                nextLocationSequence.Add(fullRoute[^1], -1);

                trains[trainId].nextLocationSequence = nextLocationSequence;
            }

        }

        public void InitialiseMaintenanceScheduleData(string csvFilePath, Dictionary<long, NetworkMaintenance> maintenances, Infrastructure.Network network)
        {
            string[] csvLines = System.IO.File.ReadAllLines(csvFilePath);

            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] rowData = csvLines[i].Split(',');


                long maintId = Convert.ToInt64(rowData[0]);
                int startTime = Convert.ToInt32(rowData[1]);
                int endTime = Convert.ToInt32(rowData[2]);
                List<long> tracks = new();

                foreach(string trackName in rowData[3].Split(";"))
                {
                    
                    tracks.Add(network.trackNames[trackName]);
                }

                maintenances.Add(maintId, new NetworkMaintenance(maintId, startTime, endTime, tracks));
                
            }

        }
    }
}
