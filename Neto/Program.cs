using System;
using System.Collections.Generic;
using Newtonsoft;



namespace Neto
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //string nodeUrl = "https://dev.railmax.io/Network/Nodes";
            //string trackUrl = "https://dev.railmax.io/Network/Engine/Tracks";
            //string locationUrl = "https://dev.railmax.io/Network/Engine/Locations";
            //string neighbouringLocationUrl = "https://dev.railmax.io/Network/Engine/NeighbouringLocations";
            //string neighbouringTrackUrl = "https://dev.railmax.io/Network/Engine/TrackArcNeighbours";

            //using var client = new HttpClient();

            //var nodData = await client.GetAsync(nodeUrl);
            //var nodes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Node>>(nodData.Content.ReadAsStringAsync().Result);

            //var traData = await client.GetAsync(trackUrl);
            //var tracks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Track>>(traData.Content.ReadAsStringAsync().Result);

            //var locData = await client.GetAsync(locationUrl);
            //var locations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.Location>>(locData.Content.ReadAsStringAsync().Result);

            //var neighLocData = await client.GetAsync(neighbouringLocationUrl);
            //var neighbouringLocations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.NeighbouringLocation>>(neighLocData.Content.ReadAsStringAsync().Result);

            //var neighTrkData = await client.GetAsync(neighbouringTrackUrl);
            //var neighbouringTracks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<APIImport.TrackArcNeighbours>>(neighTrkData.Content.ReadAsStringAsync().Result);

            


           

            string newNetworkDataCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\network.csv";
            string tsrDataCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\speedrestriction_data.csv";
            string trainDataCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\train_data.csv";
            string maintDataCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\maintenance_data.csv";
            string movementOutputCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\output.csv";
            string tgOutputCSVFilePath = @"C:\Users\ashwin\source\repos\Neto\Neto\tg_output.csv";
            string boardToLocationListMappingData = @"C:\Users\ashwin\source\repos\Neto\Neto\board_location_mapping.csv";

            Infrastructure.Network railNetwork = new();
            Dictionary<int, NetworkSpeedRestriction> tsrs = new();
            Dictionary<long, Train> trains = new();
            Dictionary<long, NetworkMaintenance> maintenances = new();
           

            DataImporter data_initialiser = new();
            await data_initialiser.ImportNetworkFromAPI(railNetwork, "DEV");

            Console.WriteLine("");

            //data_initialiser.InitialiseInfrastructureData(newNetworkDataCSVFilePath, railNetwork);
            //data_initialiser.InitialiseSpeedRestrictionData(tsrDataCSVFilePath, tsrs);
            data_initialiser.InitialiseTrainScheduleData(trainDataCSVFilePath, trains, railNetwork);
            data_initialiser.InitialiseMaintenanceScheduleData(maintDataCSVFilePath, maintenances, railNetwork);

            Global glbVar = new Global(railNetwork, tsrs, trains, maintenances);

            Dictionary<int, NetworkSpeedRestriction> tsrSnapshot = tsrs;
            Dictionary<long, Train> trainSnapshot = trains;
            Dictionary<long, NetworkMaintenance> maintSnapshot = maintenances;

            for (int j=0; j<= 0; j++)
            {
                Dispatch.Node res = glbVar.RunDispatcher(glbVar, trainSnapshot, maintSnapshot);             

                if (res != null)
                {
                    res.WriteOutputToCsv(movementOutputCSVFilePath, glbVar);
                    res.WriteOutputToCsvForPlotly(tgOutputCSVFilePath, glbVar);
                    res.WriteOutputToJson(glbVar);
                    //trainSnapshot = glbVar.GetState(Parameters.Constants.freezeDuration, res);
                    glbVar.trains = trainSnapshot;
                    glbVar.maxSolutionId = 0;
                    
                }
                else
                {
                    Console.WriteLine($"Failed to produce schedule for horizon {j}");
                    break;
                }
            }      

        }






    }
}
