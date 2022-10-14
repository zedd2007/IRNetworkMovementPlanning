using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;



namespace Neto
{
    public class Global
    {
        public int maxSolutionId;
        public Infrastructure.Network network;
        public Dictionary<int, NetworkSpeedRestriction> tsrs;
        public Dictionary<long, Train> trains;
        public Dictionary<long, NetworkMaintenance> maintenances;
        public Dictionary<int, List<NetworkSpeedRestriction>> trackTSR;

        public Global(Infrastructure.Network network, Dictionary<int, NetworkSpeedRestriction> tsrs, Dictionary<long, Train> trainDict, Dictionary<long, NetworkMaintenance> maintenances)
        {
            this.maxSolutionId = 0;
            this.network = network;
            this.trains = trainDict;
            this.maintenances = maintenances;
            this.tsrs = tsrs;
            this.trackTSR = new();
        }

        public List<Occupation> GetConflictingOccupations(long trackId, int time, Dictionary<long, List<Occupation>> trackOccupations)
        {
            List<Occupation> result = new();
            Dictionary<long, Occupation> dic = new();

            if (trackOccupations.ContainsKey(trackId))
            {
                foreach (Occupation occ in trackOccupations[trackId])
                {
                    if (occ.startTime <= time && occ.endTime >= time)
                    {

                        if (dic.ContainsKey(occ.entityId))
                        {
                            dic[occ.entityId].startTime = Math.Min(dic[occ.entityId].startTime, occ.startTime);
                            dic[occ.entityId].endTime = Math.Max(dic[occ.entityId].endTime, occ.endTime);
                        }
                        else
                        {
                            dic.Add(occ.entityId, occ);
                        }
                    }
                }

                foreach (KeyValuePair<long, Occupation> entry in dic)
                {
                    result.Add(entry.Value);
                }
                result.Sort((x, y) => x.startTime.CompareTo(y.startTime));
            }

            return result;
        }

        public Dictionary<long, Train> GetState(int absTime, int relTime, Dispatch.Node sol, out Dictionary<long, Train> trainSchedule)
        {
            trainSchedule = new();

            Dictionary<long, long> trainPosition = new();

            foreach (KeyValuePair<long, List<Occupation>> kvp in sol.entityOccupations)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    Occupation occ = kvp.Value[i];

                    if (occ.startTime < relTime && occ.endTime >= relTime)
                    {
                        trainPosition.Add(occ.entityId, occ.trackId);
                        break;
                    }
                }
            }           

            foreach (KeyValuePair<long, Train> kvp in this.trains)
            {
                Train train = this.trains[kvp.Key];

                if(!trainPosition.ContainsKey(train.id))
                {
                    int trainRelDepTime = sol.entityOccupations[train.id][0].startTime - (absTime + relTime);

                    if(trainRelDepTime > 0)
                    {                       

                        long origLocId = network.tracks[train.origTrackId].locationId;
                        List<long> fullRoute = new List<long> { origLocId };
                        foreach (long locId in train.stopLocations)
                        {

                            List<long> route = train.GetRoute(origLocId, locId, network);
                            for (int j = 1; j < route.Count; j++)
                            {
                                fullRoute.Add(route[j]);
                            }
                            origLocId = locId;

                        }

                        Dictionary<long, long> nextLocationSequence = new();
                        for (int k = 0; k < fullRoute.Count - 1; k++)
                        {
                            nextLocationSequence.Add(fullRoute[k], fullRoute[k + 1]);
                        }
                        nextLocationSequence.Add(fullRoute[^1], -1);

                        
                        trainSchedule.Add(train.id, new Train(train.id, train.origTrackId, train.destTrackId, train.direction, train.length, train.speed, trainRelDepTime, train.stopLocations));
                        trainSchedule[train.id].nextLocationSequence = nextLocationSequence;
                    }
                    
                }
                else
                {
                    long newOrigTrackId = trainPosition[train.id];

                    long origLocId = network.tracks[newOrigTrackId].locationId;
                    List<long> fullRoute = new List<long> { origLocId };

                    foreach (long locId in train.stopLocations)
                    {
                        List<long> route = train.GetRoute(origLocId, locId, network);
                        for (int j = 1; j < route.Count; j++)
                        {
                            fullRoute.Add(route[j]);
                        }
                        origLocId = locId;
                    }

                    Dictionary<long, long> nextLocationSequence = new();
                    for (int k = 0; k < fullRoute.Count - 1; k++)
                    {
                        nextLocationSequence.Add(fullRoute[k], fullRoute[k + 1]);
                    }
                    nextLocationSequence.Add(fullRoute[^1], -1);
                    
                    trainSchedule.Add(train.id, new Train(train.id, newOrigTrackId, train.destTrackId, train.direction, train.length, train.speed, 0, train.stopLocations));
                    trainSchedule[train.id].nextLocationSequence = nextLocationSequence;
                }
                
            }

            return trainSchedule;
        }
        
        
        
        public Dispatch.Node RunDispatcher(Global globalObject, Dictionary<long, Train> trainSnapshot, Dictionary<long, NetworkMaintenance> maintenanceSnapshot)
        {
            Dispatch.Node resSol = null;
            Dictionary<long, List<Occupation>> entityOccupations = new();
            foreach (KeyValuePair<long, Train> kvp in trainSnapshot)
            {
                var train = kvp.Value;
                List<Occupation> trainMovements = new();
                List<long> trainPath = train.GetPath(this, new());

                if(trainPath.Count > 0)
                {
                    for (int movId = 0; movId < trainPath.Count; movId++)
                    {
                        long trackId = trainPath[movId];
                        double speed = Math.Min(train.speed, globalObject.network.tracks[trackId].speedLimit);
                        int travelTime = Convert.ToInt32((this.network.tracks[trackId].length / speed) * 3.6);
                        int trainLengthTravelTime = Convert.ToInt32((train.length / speed) * 3.6);

                        int startTime;

                        if (movId > 0)
                        {
                            startTime = trainMovements[^1].endTime - trainLengthTravelTime;
                        }
                        else
                        {
                            startTime = train.schedDepTime;
                        }

                        int endTime = startTime + travelTime + trainLengthTravelTime;
                        Occupation occ = new Occupation(train.id, trackId, startTime, endTime, travelTime, movId);
                        trainMovements.Add(occ);
                    }
                    entityOccupations.Add(train.id, trainMovements);
                }
                else
                {
                    Console.WriteLine($"There is no initial path for train {train.id} found by path search");
                    Environment.Exit(1);
                }
                
            }

            foreach (KeyValuePair<long, NetworkMaintenance> kvp in maintenanceSnapshot)
            {
                var maintenance = kvp.Value;
                List<Occupation> maintOccupations = new();
                
                for (int occId = 0; occId < maintenance.tracks.Count; occId++)
                {                   
                    Occupation occ = new Occupation(maintenance.id, maintenance.tracks[occId], maintenance.startTime, maintenance.endTime, 0, occId);
                    maintOccupations.Add(occ);
                }
                entityOccupations.Add(maintenance.id, maintOccupations);
            }

            Dispatch.Tree mainProblemTree = new();

            Dispatch.Node initSol = new Dispatch.Node();
            initSol.id = 0;
            initSol.parentId = -1;
            initSol.creationPriorityEntityId = -1;
            initSol.creationSecondaryEntityId = -1;
            initSol.creationDecisionType = 0;
            initSol.entityOccupations = entityOccupations;

            initSol.UpdateCost(this);
            initSol.UpdateFirstConflict(this, mainProblemTree);

            mainProblemTree.SearchSolution(initSol, this);

            if (mainProblemTree.feasibleSolutionIds.Count > 0)
            {

                resSol = mainProblemTree.solutions[mainProblemTree.feasibleSolutionIds[0].id];

                //resSol.WriteOutput(movementOutputCSVFilePath);
                //glbVar.run_cmd();
                Console.WriteLine($"Final sol ID is {resSol.id} and cost is {resSol.cost} constaining {globalObject.trains.Count} trains");


                Console.WriteLine($"Final sol ancestry is {String.Join(",", resSol.GetAncestry(mainProblemTree))}");
                
                
                bool isOverlap = resSol.VerifyOverlap();
                Console.WriteLine($"Does final solution contain overlap of trains/maintenance?: {isOverlap.ToString()}");
                resSol.WriteOutputDecisionHistoryToCsv(@"C:\Users\ashwin\source\repos\Neto\Neto\decision_history_output.csv", globalObject, mainProblemTree);



            }


            return resSol;

        }
        public void run_cmd()
        {

            string fileName = @"C:\Users\ashwin.zade\PycharmProjects\train_graph\new_plotter.py";

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(@"C:\Python39\python.exe", fileName)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            Console.WriteLine(output);            

        }
    }
}
