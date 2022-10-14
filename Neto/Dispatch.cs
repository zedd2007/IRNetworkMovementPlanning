using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;

namespace Neto
{
    namespace Dispatch
    {

        
        public class ConflictStatistics
        {
            public Dictionary<long, int> priorityDecisionCount;
            public int firstOccuranceSolutionId;
            public bool isRepeated;

            public ConflictStatistics(int firstOccuranceSolutionId)
            {
                this.priorityDecisionCount = new();
                this.firstOccuranceSolutionId = firstOccuranceSolutionId;
                this.isRepeated = false;
            }
        }
        public class Node
        {
            public int id;
            public int parentId;
            public bool isVisited;
            public double cost;
            public bool isExpanded;
            public bool isUpdated;
            public List<int> childrenIds;
            public long creationPriorityEntityId;
            public long creationSecondaryEntityId;
            public int creationDecisionType; //0 means stopping, 1 means alternate
            public Dictionary<long, List<Occupation>> entityOccupations;
            public Dictionary<long, List<long>> trainProhibitedTracks;                      
            public Conflict firstConflict;
            
            


            public Node()
            {
                
                this.isVisited = false;
                this.cost = 0;
                this.isExpanded = false;
                this.isUpdated = false;

                this.childrenIds = new();
                this.firstConflict = null;
                
                this.trainProhibitedTracks = new();
                
            }

            

            public bool DoesOverlap(Occupation occ1, Occupation occ2)
            {
                
                if(occ1.startTime <= occ2.endTime && occ1.endTime >= occ2.startTime)
                {
                    return true;
                    
                    
                }
                return false;
            }

            public bool VerifyOverlap()
            {
                Dictionary<long, List<Occupation>> trackOccupations = new();
                foreach (KeyValuePair<long, List<Occupation>> entry in this.entityOccupations)
                {
                    foreach (Occupation occ in entry.Value)
                    {
                        if (trackOccupations.ContainsKey(occ.trackId))
                        {
                            foreach(Occupation exOcc in trackOccupations[occ.trackId])
                            {
                                if(this.DoesOverlap(exOcc, occ))
                                {
                                    Console.WriteLine($"Overlap in sol id {this.id} between entities {occ.entityId} and {exOcc.entityId} at track {occ.trackId}");
                                    return true;
                                }
                            }
                            trackOccupations[occ.trackId].Add(occ);
                        }
                        else
                        {
                            trackOccupations.Add(occ.trackId, new List<Occupation> { occ });
                        }
                    }
                }
                return false;
            }

            public List<int> GetAncestry(Dispatch.Tree tree)
            {
                List<int> result = new();

                Dispatch.Node currSol = this;               

                while(currSol.id != 0)
                {
                    currSol =  tree.solutions[currSol.parentId];
                    result.Add(currSol.id);

                }
                
                return result;

            }
            public Node DeepCopy(Global globalObject)
            {
                Node result = new();
                globalObject.maxSolutionId += 1;

                result.id = globalObject.maxSolutionId;
                result.parentId = this.id;
                result.creationPriorityEntityId = this.creationPriorityEntityId;
                result.creationSecondaryEntityId = this.creationSecondaryEntityId;
                result.creationDecisionType = this.creationDecisionType;
                result.entityOccupations = new();



                foreach (KeyValuePair<long, List<Occupation>> kvp in this.entityOccupations)
                {
                    List<Occupation> newList = new();

                    foreach(Occupation occ in kvp.Value)
                    {
                        newList.Add(occ.DeepCopy());
                    }
                    result.entityOccupations.Add(kvp.Key, newList);
                }

                foreach (KeyValuePair<long, List<long>> kvp in this.trainProhibitedTracks)
                {
                    List<long> newList = new List<long>(kvp.Value);
                    
                    result.trainProhibitedTracks.Add(kvp.Key, newList);
                }             

                return result;
            }

            public string Print(Global glb)
            {
                string result;
                string id = this.id.ToString();
                string cost = this.cost.ToString();

                string conflict = "";
                
                if(this.firstConflict != null)
                {
                    conflict = this.firstConflict.Print(glb);
                }
                    

                result = "id: " + id + ",cost: " + cost + ",conflict: " + conflict;

                return result;
            }            

            public ConflictStatistics GetFirstConflictOccuranceSolutionId(Dispatch.Tree tree)
            {               

                ConflictStatistics cflStats = new ConflictStatistics(this.parentId);
                if(this.id > 0)
                {
                    cflStats.priorityDecisionCount.Add(this.firstConflict.overlappingOccupations.Keys.ToList()[0], 0);
                    cflStats.priorityDecisionCount.Add(this.firstConflict.overlappingOccupations.Keys.ToList()[1], 0);
                }
                

                Dispatch.Node currSol = this;
                int childSolId = this.id;

                

                while (currSol.parentId != -1)
                {
                    currSol = tree.solutions[currSol.parentId];
                    if (currSol.firstConflict.trackId == this.firstConflict.trackId && currSol.firstConflict.overlappingOccupations.Keys.ToHashSet().SetEquals(this.firstConflict.overlappingOccupations.Keys.ToHashSet()))
                    {
                        
                        Dispatch.Node childSol = tree.solutions[childSolId];
                        cflStats.priorityDecisionCount[childSol.creationPriorityEntityId] += 1;
                        if (cflStats.priorityDecisionCount[childSol.creationPriorityEntityId] >= Parameters.Constants.maxConflictRepeatationCount)
                        {
                            
                            cflStats.firstOccuranceSolutionId = currSol.id;
                            cflStats.isRepeated = true;
                            break;
                        }
                        
                    }
                    childSolId = currSol.id;
                    

                }

                return cflStats;
            }

            public void GetStoppingMovementIdAndDuration(long priorityEntityId, long secondaryEntityId, Conflict conflict, Global globalObject, out int stoppingDuration, out int secondaryEntityMovementId)
            {                
                List<Occupation> priorityEntityRoute = this.entityOccupations[priorityEntityId];
                List<Occupation> secondaryEntityRoute = this.entityOccupations[secondaryEntityId];
                secondaryEntityMovementId = conflict.overlappingOccupations[secondaryEntityId].movementId;
                int priorityEntityMovementId = conflict.overlappingOccupations[priorityEntityId].movementId;
                stoppingDuration = priorityEntityRoute[priorityEntityMovementId].endTime - secondaryEntityRoute[secondaryEntityMovementId].startTime;

                if (conflict.type != 2)
                {
                    if (stoppingDuration < 0)
                    {
                        Console.WriteLine($"Stopping duration is negative");
                        Environment.Exit(0);
                    }

                    stoppingDuration += Neto.Parameters.Constants.trainClearanceTime;

                    secondaryEntityMovementId -= 1;
                    priorityEntityMovementId += 1;

                    if (conflict.type == 1) //conflict.type == 1
                    {
                        while (secondaryEntityMovementId >= 0 && priorityEntityMovementId < priorityEntityRoute.Count)
                        {
                            //stoppingDuration = Math.Max(0, priorityEntityRoute[priorityEntityMovementId].endTime - secondaryEntityRoute[secondaryEntityMovementId].startTime) + Parameters.Constants.trainClearanceTime;
                            Infrastructure.Track currTrack = globalObject.network.tracks[secondaryEntityRoute[secondaryEntityMovementId].trackId];
                            if (currTrack.length < globalObject.trains[secondaryEntityId].length)
                            {
                                secondaryEntityMovementId -= 1;
                                priorityEntityMovementId += 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        while (secondaryEntityMovementId >= 0)
                        {
                            Infrastructure.Track currTrack = globalObject.network.tracks[secondaryEntityRoute[secondaryEntityMovementId].trackId];
                            if (currTrack.length < globalObject.trains[secondaryEntityId].length)
                            {
                                secondaryEntityMovementId -= 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                else
                {
                    
                    if (stoppingDuration < 0)
                    {
                        Console.WriteLine($"Stopping duration is negative");
                        Environment.Exit(0);
                    }

                    stoppingDuration += Neto.Parameters.Constants.maintenanceClearanceTime;

                    secondaryEntityMovementId -= 1;
                   

                    while (secondaryEntityMovementId >= 0)
                    {
                        Infrastructure.Track currTrack = globalObject.network.tracks[secondaryEntityRoute[secondaryEntityMovementId].trackId];
                        if (currTrack.length < globalObject.trains[secondaryEntityId].length)
                        {
                            secondaryEntityMovementId -= 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                }             
         
            }

            public void UpdateTrainMovementForStoppingSolution(long trainId, int stoppingMovementId, int stoppingDuration)
            {
                List<Occupation> trainRoute = this.entityOccupations[trainId];
                if(stoppingMovementId > -1)
                {
                    trainRoute[stoppingMovementId].endTime += stoppingDuration;
                }
                
                for (int movId = stoppingMovementId + 1; movId < trainRoute.Count; movId += 1)
                {
                    trainRoute[movId].startTime += stoppingDuration;
                    trainRoute[movId].endTime += stoppingDuration;

                    if(trainRoute[movId].startTime < 0 || trainRoute[movId].endTime < 0)
                    {
                        Console.WriteLine($"Exiting due to negative start or end time");
                        Environment.Exit(0);
                    }
                }
            }


            public void UpdateTrainMovementForAlternateRouteSolution(long trainId, List<long> newPath, Global globalObject)
            {
                int movId = 0;
                List<Occupation> oldRoute = this.entityOccupations[trainId];
                List<Occupation> newRoute = new();

                while(movId < oldRoute.Count && oldRoute[movId].trackId == newPath[movId])
                {
                    newRoute.Add(oldRoute[movId].DeepCopy());
                    movId += 1;
                }

                for(int newMovId = movId; newMovId < newPath.Count; newMovId += 1)
                {
                    long trackId = newPath[newMovId];
                    int travelTime = Convert.ToInt32((globalObject.network.tracks[trackId].length / globalObject.trains[trainId].speed) * 3.6);
                    int trainLengthTravelTime = Convert.ToInt32((globalObject.trains[trainId].length/ globalObject.trains[trainId].speed) * 3.6);

                    int startTime = newRoute[^1].endTime - trainLengthTravelTime; // Parameters.Constants.trainClearanceTime;                    

                    int endTime = startTime + travelTime + trainLengthTravelTime; //+ Parameters.Constants.trainClearanceTime;

                    if(startTime < 0 || endTime < 0)
                    {
                        Console.WriteLine("Start time or end time is negative");
                        Environment.Exit(0);
                    }
                    Occupation occ = new Occupation(trainId, trackId, startTime, endTime, travelTime, newMovId);
                    newRoute.Add(occ);
                }
                this.entityOccupations[trainId] = newRoute;
                
            }


            public Dispatch.Node GetTrainStoppingSolution(long priorityEntityId, long secondaryEntityId, Conflict conflict, Global globalObject, Dispatch.Tree tree, bool shouldCreateChild)
            {
                Dispatch.Node result;
                int stoppingMovementId;
                int stoppingDuration;

                this.GetStoppingMovementIdAndDuration(priorityEntityId, secondaryEntityId, conflict, globalObject, out stoppingDuration, out stoppingMovementId);


                bool isLiveTrain = globalObject.trains[secondaryEntityId].isLive;
                if(isLiveTrain && stoppingMovementId < 0)
                {
                    Console.WriteLine($"Stopping solution for parent sol {this.id} is not possible for train {secondaryEntityId} because conflict is happening in its starting track where it can't backtrack");
                    result = null;
                }

                else
                {  
                    if(!isLiveTrain && stoppingDuration > Parameters.Constants.maxDepartureDelayTime)
                    {
                        Console.WriteLine($"Stopping solution for parent sol {this.id} is not possible for train {secondaryEntityId} because its scheduled departure delay is more than threshold");
                        result = null;
                    }
                    else
                    {
                        if (shouldCreateChild)
                        {
                            var newSol = this.DeepCopy(globalObject);

                            newSol.creationPriorityEntityId = priorityEntityId;
                            newSol.creationSecondaryEntityId = secondaryEntityId;
                            newSol.creationDecisionType = 0;

                            newSol.UpdateTrainMovementForStoppingSolution(secondaryEntityId, stoppingMovementId, stoppingDuration);

                            newSol.UpdateCost(globalObject);
                            newSol.UpdateFirstConflict(globalObject, tree);

                            result = newSol;
                        }
                        else
                        {
                            this.UpdateTrainMovementForStoppingSolution(secondaryEntityId, stoppingMovementId, stoppingDuration);

                            this.UpdateCost(globalObject);
                            this.UpdateFirstConflict(globalObject, tree);

                            result = this;
                        }
                    }

                                   

                } 
                
                return result;
            }

            public Dispatch.Node GetTrainAlternateRouteSolution(long priorityEntityId, long secondaryEntityId, Conflict conflict, Global globalObject, Dispatch.Tree tree)
            {
                Dispatch.Node result;

                List<long> prohTrackIds = new();

                if(this.trainProhibitedTracks.ContainsKey(secondaryEntityId))
                {
                    prohTrackIds = new List<long>(this.trainProhibitedTracks[secondaryEntityId]);
                }

                if(conflict.type != 2)
                {
                    if(!prohTrackIds.Contains(conflict.trackId))
                    {
                        prohTrackIds.Add(conflict.trackId);
                    }                    
                }
                else
                {
                    List<long> maintTracks = globalObject.maintenances[priorityEntityId].tracks;
                    prohTrackIds.AddRange(maintTracks);
                }               

                List<long> newPath = globalObject.trains[secondaryEntityId].GetPath(globalObject, prohTrackIds);

                if (newPath.Count > 0)
                {
                    //List<int> nonPrefMovIds = this.GetNonPreferredPathMovementIndexes(newPath, this.entityOccupations[secondaryEntityId]);
                    var newSol = this.DeepCopy(globalObject);
                    newSol.creationPriorityEntityId = priorityEntityId;
                    newSol.creationSecondaryEntityId = secondaryEntityId;
                    newSol.creationDecisionType = 1;
                    newSol.trainProhibitedTracks[secondaryEntityId] = prohTrackIds;
                    newSol.UpdateTrainMovementForAlternateRouteSolution(secondaryEntityId, newPath, globalObject);
                    
                    newSol.UpdateCost(globalObject);
                    newSol.UpdateFirstConflict(globalObject, tree);
                    result = newSol;
                }
                else
                {
                    Console.WriteLine($"Alternate solution for parent solution {this.id} is infeasible due to blocked route for train {secondaryEntityId}");
                    result = null;
                }

                return result;
            }

            public List<int> GetNonPreferredPathMovementIndexes(List<long> newPath, List<Occupation> oldPathOccupations)
            {
                List<int> result = new();

                for(int i=0; i<newPath.Count; i++)
                {
                    if(i < oldPathOccupations.Count)
                    {
                        if (newPath[i] != oldPathOccupations[i].trackId)
                        {
                            result.Add(i);
                        }
                    }
                }

                return result;
            }
                       

            public void CreateChildren(Global globalObject, Dispatch.Tree tree)
            { 
                List<long> entityIds = new List<long>(this.firstConflict.overlappingOccupations.Keys);

                if (this.firstConflict.type == 0)
                {
                    Dispatch.Node childSol;
                    if (this.firstConflict.overlappingOccupations[entityIds[0]].startTime <= this.firstConflict.overlappingOccupations[entityIds[1]].startTime)
                    {
                        childSol = this.GetTrainStoppingSolution(entityIds[0], entityIds[1], this.firstConflict, globalObject, tree, false);
                    }
                    else
                    {
                        childSol = this.GetTrainStoppingSolution(entityIds[1], entityIds[0], this.firstConflict, globalObject, tree, false);
                    }
                    if (childSol != null)
                    {
                        childSol.isUpdated = true;                        
                    }

                }
                else
                {
                    if(this.firstConflict.type == 1)
                    {
                        Dispatch.Node childSol1 = this.GetTrainStoppingSolution(entityIds[0], entityIds[1], this.firstConflict, globalObject, tree, true);

                        Dispatch.Node childSol2 = this.GetTrainAlternateRouteSolution(entityIds[0], entityIds[1], this.firstConflict, globalObject, tree);

                        Dispatch.Node childSol3 = this.GetTrainStoppingSolution(entityIds[1], entityIds[0], this.firstConflict, globalObject, tree, true);

                        Dispatch.Node childSol4 = this.GetTrainAlternateRouteSolution(entityIds[1], entityIds[0], this.firstConflict, globalObject, tree);

                        if (childSol1 != null)
                        {
                            tree.solutions.Add(childSol1.id, childSol1);
                            this.childrenIds.Add(childSol1.id);
                        }

                        if (childSol2 != null)
                        {
                            tree.solutions.Add(childSol2.id, childSol2);
                            this.childrenIds.Add(childSol2.id);
                        }

                        if (childSol3 != null)
                        {
                            tree.solutions.Add(childSol3.id, childSol3);
                            this.childrenIds.Add(childSol3.id);
                        }

                        if (childSol4 != null)
                        {
                            tree.solutions.Add(childSol4.id, childSol4);
                            this.childrenIds.Add(childSol4.id);
                        }
                    }


                    else
                    {
                        long maintId;
                        long trainId;
                        if(globalObject.maintenances.ContainsKey(entityIds[0]))
                        {
                            maintId = entityIds[0];
                            trainId = entityIds[1];
                        }
                        else
                        {
                            maintId = entityIds[1];
                            trainId = entityIds[0];
                        }

                        Dispatch.Node childSol1 = this.GetTrainStoppingSolution(maintId, trainId, this.firstConflict, globalObject, tree, true);

                        Dispatch.Node childSol2 = this.GetTrainAlternateRouteSolution(maintId, trainId, this.firstConflict, globalObject, tree);
                        

                        if (childSol1 != null)
                        {
                            tree.solutions.Add(childSol1.id, childSol1);
                            this.childrenIds.Add(childSol1.id);
                        }

                        if (childSol2 != null)
                        {
                            tree.solutions.Add(childSol2.id, childSol2);
                            this.childrenIds.Add(childSol2.id);
                        }

                        
                    }
                    
                    
                }
            }
                 
            public void UpdateFirstConflict(Global globalObject, Dispatch.Tree tree)
            {
                this.firstConflict = null;
                long minConflictTime = 10000000000000;
                Dictionary<long, List<Occupation>> trackOccupations = new();
                List<Conflict> trackFirstConflictList = new();

                
                foreach (KeyValuePair<long, List<Occupation>> entry in this.entityOccupations)
                {
                    foreach (Occupation occ in entry.Value)
                    {
                        if (trackOccupations.ContainsKey(occ.trackId))
                        {
                            trackOccupations[occ.trackId].Add(occ);
                        }
                        else
                        {
                            trackOccupations.Add(occ.trackId, new List<Occupation> { occ });
                        }
                    }
                }

                //Sort occupations in track
                foreach (KeyValuePair<long, List<Occupation>> entry in trackOccupations)
                {
                    trackOccupations[entry.Key].Sort((x, y) => x.startTime.CompareTo(y.startTime));
                }

                //Find first conflict in each track
                foreach (KeyValuePair<long, List<Occupation>> entry in trackOccupations)
                {
                    List<int> checkTimeList = new();

                    foreach (Occupation occ in entry.Value)
                    {
                        if (occ.endTime <= minConflictTime)
                        {
                            checkTimeList.Add(occ.startTime);
                            checkTimeList.Add(occ.endTime);

                        }
                        else
                        {
                            if (occ.startTime <= minConflictTime)
                            {
                                checkTimeList.Add(occ.startTime);
                            }
                        }


                    }

                    checkTimeList.Sort((x, y) => x.CompareTo(y));
                    foreach (int checkTime in checkTimeList)
                    {
                        List<Occupation> occList = globalObject.GetConflictingOccupations(entry.Key, checkTime, trackOccupations);
                        

                        if (occList.Count > 1)
                        {
                            minConflictTime = Math.Min(minConflictTime, checkTime);

                            Occupation firstEntityOccupation = occList[0];
                            Occupation secondEntityOccupation = occList[1];
                            int conflictType;



                            if (globalObject.trains.ContainsKey(firstEntityOccupation.entityId) && globalObject.trains.ContainsKey(secondEntityOccupation.entityId))
                            {
                                if (globalObject.trains[firstEntityOccupation.entityId].direction == globalObject.trains[secondEntityOccupation.entityId].direction)
                                {
                                    conflictType = 0;
                                }
                                else
                                {
                                    conflictType = 1;
                                }
                            }
                            else
                            {
                                conflictType = 2;
                            }


                            Dictionary<long, Occupation> dic = new Dictionary<long, Occupation> { };
                            dic.Add(firstEntityOccupation.entityId, firstEntityOccupation);
                            dic.Add(secondEntityOccupation.entityId, secondEntityOccupation);                            

                            trackFirstConflictList.Add(new Conflict(-1,dic,entry.Key,checkTime,conflictType));

                            break;

                        }

                    }
                }

                if (trackFirstConflictList.Count > 0)
                {
                    trackFirstConflictList.Sort((x, y) => x.time.CompareTo(y.time));

                    this.firstConflict = trackFirstConflictList[0];


                }


            }

            public void UpdateCost(Global globalObject)
            {
                this.cost = 0;
                foreach (KeyValuePair<long, List<Occupation>> entry in this.entityOccupations)
                {
                    if (globalObject.trains.ContainsKey(entry.Key))
                    {
                        for (int movId = 0; movId < entry.Value.Count - 1; movId += 1)
                        {
                            int dwellDuration = entry.Value[movId + 1].startTime - entry.Value[movId].startTime - entry.Value[movId].trainTravelTime;
                            if (dwellDuration < 0)
                            {
                                Console.WriteLine("Dwell duration is negative");
                                Environment.Exit(0);
                            }                            
                            this.cost += (Math.Pow(dwellDuration, 1));

                        }
                    }
                }
            }

            public void WriteOutputToCsv(string filePath, Global glb)
            {
                using (var w = new StreamWriter(filePath))
                {
                    string line = "TRAIN_ID" + "," + "MOV_ID" + "," + "TRACK_ID" + "," + "START_TIME" + "," + "END_TIME" + "," + "DWELL" + "," + "TRAVEL";
                    w.WriteLine(line);
                    foreach (KeyValuePair<long, List<Occupation>> kvp in this.entityOccupations)
                    {
                        if(glb.trains.ContainsKey(kvp.Key))
                        {
                            for (int i = 0; i < kvp.Value.Count; i++)
                            {
                                Occupation occ = kvp.Value[i];

                                int dwellTime = 0;
                                if (i < kvp.Value.Count - 1)
                                {
                                    Occupation nextOcc = kvp.Value[i + 1];
                                    dwellTime = nextOcc.startTime - occ.startTime - occ.trainTravelTime;
                                }
                                string rowLine = occ.entityId.ToString() + "," + occ.movementId.ToString() + "," + glb.network.tracks[occ.trackId].name + "," + occ.startTime.ToString() + "," + occ.endTime.ToString() + "," + dwellTime.ToString() + "," + occ.trainTravelTime.ToString();
                                w.WriteLine(rowLine);
                                w.Flush();
                            }
                        }                        
                        
                    }
                }
            }

            public void WriteOutputDecisionHistoryToCsv(string filePath, Global glb, Tree tree)
            {
                using (var w = new StreamWriter(filePath))
                {
                    string line = "SOLUTION_ID" + "," + "DEC_TYPE" + "," + "PRIORITY_ENTITY_ID" + "," + "SECONDARY_ENTITY_ID" + "," + "CONFLICT";
                    w.WriteLine(line);
                    int currSolId = this.parentId;
                    while (currSolId != -1)
                    {
                        Node sol = tree.solutions[currSolId];                        
                        
                        string rowLine = sol.id.ToString() + "," + sol.creationDecisionType + "," + sol.creationPriorityEntityId.ToString() + "," + sol.creationSecondaryEntityId.ToString() + "," + sol.firstConflict.Print(glb);
                        w.WriteLine(rowLine);
                        w.Flush();
                        currSolId = tree.solutions[currSolId].parentId;
                    }
                }
            }



            public void WriteOutputToJson(Global glb)
            {
                FinalExport finalResult = new FinalExport();
                foreach (KeyValuePair<long, List<Occupation>> kvp in this.entityOccupations)
                {                    
                    if (glb.trains.ContainsKey(kvp.Key))
                    {
                        ExportOutput trainPathExport = new ExportOutput();
                        trainPathExport.trainId = kvp.Key.ToString();
                        trainPathExport.id = kvp.Key;
                        trainPathExport.scheduleId = kvp.Key;
                        trainPathExport.movementStartTime = new DateTime(2022,06,01,03,00,00);//DateTime.Now;
                        trainPathExport.trainType = "Others";
                        //long divisionId = 736028141618176;
                        //trainPathExport.ScenarioId = 1550732562054144;
                        trainPathExport.segments = new List<Segment> { new Segment("NonIndicative", new List<Movement>()) };

                        int seqId = 0;
                        for (int i = 0; i < kvp.Value.Count; i++)
                        {
                            seqId += 1;
                            Occupation occ = kvp.Value[i];

                            int dwellTime = 0;
                            double offset = 0;
                            if (i < kvp.Value.Count - 1)
                            {
                                Occupation nextOcc = kvp.Value[i + 1];
                                dwellTime = nextOcc.startTime - occ.startTime - occ.trainTravelTime;
                                
                            }

                            if (i > 0)
                            {
                                Occupation prevOcc = kvp.Value[i - 1];
                                if (glb.network.tracks[prevOcc.trackId].neighbourTracks[occ.trackId] == glb.network.tracks[occ.trackId].node1ToNode2Direction)
                                {
                                    offset = 0;
                                }
                                else
                                {
                                    offset = 1;
                                }
                            }
                            else
                            {
                                Train train = glb.trains[kvp.Key];
                                if (train.direction == glb.network.tracks[train.origTrackId].node1ToNode2Direction)
                                {
                                    offset = 0;
                                }
                                else
                                {
                                    offset = 1;
                                }

                            }

                            trainPathExport.segments[0].path.Add(new Movement(occ.trackId, offset, occ.startTime, seqId, glb.network.tracks[occ.trackId].divisionId));

                            if(dwellTime > 0)
                            {
                                seqId += 1;
                                Occupation nextOcc = kvp.Value[i + 1];

                                if (glb.network.tracks[occ.trackId].neighbourTracks[nextOcc.trackId] == glb.network.tracks[nextOcc.trackId].node1ToNode2Direction)
                                {
                                    offset = 0;
                                }
                                else
                                {
                                    offset = 1;
                                }

                                trainPathExport.segments[0].path.Add(new Movement(nextOcc.trackId, offset, nextOcc.startTime - dwellTime, seqId, glb.network.tracks[nextOcc.trackId].divisionId));
                            }
                            
                        }

                        finalResult.movements.Add(trainPathExport);
                    }
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(finalResult);
            }


           
            public class TgMovData
            {
                public string stationName { get; set; }
                public int time { get; set; }

                public TgMovData(string stationName, int time)
                {
                    this.stationName = stationName;
                    this.time = time;
                }
            }
            public void WriteOutputToCsvForPlotly(string filePath, Global glb)
            {
                using (var w = new StreamWriter(filePath))
                {
                    string line = "TRAIN_ID" + "," + "STATION_ID" + "," + "TIME";
                    w.WriteLine(line);
                    foreach (KeyValuePair<long, List<Occupation>> kvp in this.entityOccupations)
                    {
                        if (glb.trains.ContainsKey(kvp.Key))
                        {
                            List<TgMovData> movData = new List<TgMovData>();

                            Occupation initOcc = kvp.Value[0];

                            string initLocType = glb.network.locations[glb.network.tracks[initOcc.trackId].locationId].type;
                            string initLocName = glb.network.locations[glb.network.tracks[initOcc.trackId].locationId].name;

                            if (initLocType == "Station")
                            {
                                movData.Add(new TgMovData(initLocName, 0));
                            }
                            else
                            {
                                if(kvp.Value.Count > 1)
                                {
                                    Occupation secondOcc = kvp.Value[1];
                                    string secondLocName = glb.network.locations[glb.network.tracks[secondOcc.trackId].locationId].name;
                                    string[] stations = initLocName.Split("-");
                                    if (stations[0] != secondLocName)
                                    {
                                        movData.Add(new TgMovData(stations[0], 0));
                                    }
                                    else
                                    {
                                        movData.Add(new TgMovData(stations[1], 0));
                                    }
                                }
                            }

                            string initRowLine = kvp.Key.ToString() + "," + movData[^1].stationName + "," + movData[^1].time.ToString();
                            w.WriteLine(initRowLine);
                            w.Flush();

                            for (int i = 0; i < kvp.Value.Count; i++)
                            {
                                Occupation currOcc = kvp.Value[i];

                                string currLocType = glb.network.locations[glb.network.tracks[currOcc.trackId].locationId].type;
                                string currLocName = glb.network.locations[glb.network.tracks[currOcc.trackId].locationId].name;

                                int currTrackTravelTime = currOcc.trainTravelTime;

                                int currTrackDwellTime = 0;
                                if (i < kvp.Value.Count - 1)
                                {
                                    Occupation nextOcc = kvp.Value[i + 1];
                                    currTrackDwellTime = nextOcc.startTime - currOcc.startTime - currOcc.trainTravelTime;
                                }

                                if (currLocType == "BlockSection")
                                {
                                    string[] stations = currLocName.Split("-");
                                    if (stations[0] != movData[^1].stationName)
                                    {
                                        movData.Add(new TgMovData(stations[0], movData[^1].time + currTrackTravelTime + currTrackDwellTime));
                                    }
                                    else
                                    {
                                        movData.Add(new TgMovData(stations[1], movData[^1].time + currTrackTravelTime + currTrackDwellTime));
                                    }
                                }
                                else
                                {
                                    movData.Add(new TgMovData(currLocName, movData[^1].time + currTrackTravelTime + currTrackDwellTime));
                                }

                                string rowLine = kvp.Key.ToString() + "," + movData[^1].stationName + "," + movData[^1].time.ToString();
                                w.WriteLine(rowLine);
                                w.Flush();
                            }
                        }

                    }
                }

            }
        }
        public class NodeAlias
        {
            public int id;
            public int conflictTime;
            public double cost;

            public NodeAlias(int id, int conflictTime, double cost)
            {
                this.id = id;
                this.conflictTime = conflictTime;
                this.cost = cost;
            }
        }
                
        public class Tree
        {
            public Dictionary<int, Dispatch.Node> solutions;            
            
            public List<NodeAlias> feasibleSolutionIds;

            public Tree()
            {
                this.solutions = new();                
                
                this.feasibleSolutionIds = new();

            }

            public void SearchSolution(Dispatch.Node rootSol, Global globalObject)
            {
                int iterationCount = 0;
                this.solutions.Add(rootSol.id, rootSol);
                
                if(rootSol.firstConflict == null)
                {                    
                
                    this.feasibleSolutionIds.Add(new NodeAlias(rootSol.id, -1, rootSol.cost));
                    
                }
                
               

                Dispatch.Node currSol = rootSol;
                Dispatch.Node nextSol = new();
                Dispatch.Node prevSol = new();
                prevSol.id = -1;

                

                List<int> anchorSolIds = new();

                double minCost = Parameters.Constants.maxCost;

                while (this.feasibleSolutionIds.Count < Parameters.Constants.maxCountOfFeasibleSolutions && iterationCount < Parameters.Constants.maxIterations)
                {
                    iterationCount += 1;

                    if (currSol.firstConflict != null)
                    {
                        if (currSol.cost < minCost)
                        {
                            if (currSol.isVisited == false || currSol.id == prevSol.id)
                            {
                                currSol.isVisited = true;
                                if (currSol.parentId != -1)
                                {
                                    Dispatch.Node parentSol = this.solutions[currSol.parentId];
                                    Console.WriteLine($"curr sol {currSol.Print(globalObject)} having parent sol {parentSol.Print(globalObject)} with priority ent {currSol.creationPriorityEntityId} with creation decision type {currSol.creationDecisionType}");

                                }
                                else
                                {
                                    Console.WriteLine($"curr sol {currSol.Print(globalObject)} having parent sol -1 with priority ent {currSol.creationPriorityEntityId}");

                                }

                                ConflictStatistics cfl = currSol.GetFirstConflictOccuranceSolutionId(this);

                                if (!cfl.isRepeated)
                                {
                                    currSol.CreateChildren(globalObject, this);


                                    currSol.isExpanded = true;

                                    if (currSol.childrenIds.Count > 0)
                                    {
                                        List<Dispatch.Node> childrenList = new();

                                        foreach (int childId in currSol.childrenIds)
                                        {
                                            childrenList.Add(this.solutions[childId]);

                                        }
                                        childrenList.Sort((x, y) => x.cost.CompareTo(y.cost));
                                        nextSol = this.solutions[childrenList[0].id];


                                    }
                                    else
                                    {
                                        if(currSol.isUpdated)
                                        {
                                            currSol.isUpdated = false;
                                            nextSol = currSol;
                                        }
                                        else
                                        {
                                            if (currSol.parentId != -1)
                                            {
                                                nextSol = this.solutions[currSol.parentId];
                                                Console.WriteLine($"Jumped from {currSol.id} to immediate parent {nextSol.id} due to dead-end due to same directional conflict");
                                            }
                                            else
                                            {
                                                if (anchorSolIds.Count > 0)
                                                {
                                                    nextSol = this.solutions[anchorSolIds[^1]];
                                                    anchorSolIds.RemoveAt(anchorSolIds.Count - 1);
                                                    Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to reaching dead-end due to same directional conflict");
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Stopped search due to no anchor solutions remaining after jumping from dead-end due to same directional conflict");
                                                    break;
                                                }
                                            }
                                        }
                                        

                                    }

                                }
                                else
                                {
                                    if (currSol.parentId != -1)
                                    {
                                        anchorSolIds.Insert(0, currSol.parentId);
                                    }

                                    if (cfl.firstOccuranceSolutionId != currSol.parentId)
                                    {
                                        nextSol = this.solutions[cfl.firstOccuranceSolutionId];
                                        Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to repeatation of conflict");
                                    }
                                    else
                                    {
                                        if (anchorSolIds.Count > 0)
                                        {
                                            nextSol = this.solutions[anchorSolIds[^1]];
                                            anchorSolIds.RemoveAt(anchorSolIds.Count - 1);
                                            Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to repeatation of conflict");
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                                                        
                                }
                            }
                            else
                            {
                                List<Dispatch.Node> unvisitedChildren = new();
                                foreach (int childId in currSol.childrenIds)
                                {

                                    if (this.solutions[childId].isVisited == false)
                                    {
                                        unvisitedChildren.Add(this.solutions[childId]);
                                    }

                                }

                                if (unvisitedChildren.Count > 0)
                                {
                                    unvisitedChildren.Sort((x, y) => x.cost.CompareTo(y.cost));
                                    nextSol = this.solutions[unvisitedChildren[0].id];
                                }
                                else
                                {
                                    if (currSol.parentId != -1)
                                    {
                                        nextSol = this.solutions[currSol.parentId];
                                        Console.WriteLine($"Jumped from {currSol.id} to immediate parent {nextSol.id} due to all child nodes being visited");

                                    }
                                    else
                                    {
                                        if (anchorSolIds.Count > 0)
                                        {
                                            nextSol = this.solutions[anchorSolIds[^1]];
                                            anchorSolIds.RemoveAt(anchorSolIds.Count - 1);
                                            Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to reaching root node");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Stopped search due to no anchor solutions remaining after reaching root node");
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                        else
                        {
                            currSol.isVisited = true;
                            currSol.isExpanded = false;

                            if (currSol.parentId != -1)
                            {
                                anchorSolIds.Insert(0, currSol.parentId);
                            }
                            if (anchorSolIds.Count > 0)
                            {
                                nextSol = this.solutions[anchorSolIds[^1]];
                                anchorSolIds.RemoveAt(anchorSolIds.Count - 1);
                                Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to reaching high cost node");
                            }
                            else
                            {
                                Console.WriteLine($"Stopped search due to no anchor solutions remaining after jumping from high cost node");
                                break;
                            }
                        }

                    }
                    else
                    {
                        currSol.isVisited = true;
                        currSol.isExpanded = false;
                        this.feasibleSolutionIds.Add(new NodeAlias(currSol.id, -1, currSol.cost));

                        Console.WriteLine($"Found feasible sol {currSol.id} with cost {currSol.cost}");

                        minCost = Math.Min(minCost, currSol.cost);
                        List<int> currSolAncestry = currSol.GetAncestry(this);
                        currSolAncestry.Reverse();
                        bool toExit = false;
                        int jumpSolId = currSol.parentId;

                        foreach (int anc in currSolAncestry)
                        {
                            if (!toExit)
                            {

                                foreach (int chId in this.solutions[anc].childrenIds)
                                {
                                    if (!this.solutions[chId].isVisited)
                                    {
                                        jumpSolId = anc;
                                        toExit = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (currSol.parentId != -1)
                        {                            
                            anchorSolIds.Insert(0, currSol.parentId);   
                        }

                        if (jumpSolId != currSol.parentId)
                        {
                            nextSol = this.solutions[jumpSolId];
                            Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to reaching feasible node");
                        }
                        else
                        {
                            if (anchorSolIds.Count > 0)
                            {
                                nextSol = this.solutions[anchorSolIds[^1]];
                                anchorSolIds.RemoveAt(anchorSolIds.Count - 1);
                                Console.WriteLine($"Jumped from {currSol.id} to {nextSol.id} due to reaching feasible node");
                            }
                            else
                            {
                                Console.WriteLine($"Stopped search due to no anchor solutions remaining after jumping from feasible solution");
                                break;
                            }
                        }
                    }

                    prevSol = currSol;
                    currSol = nextSol;

                }

                this.feasibleSolutionIds.Sort((x, y) => x.cost.CompareTo(y.cost));
                

            }

        }
    }
}
