using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{
    namespace Parameters
    {
        public class Constants
        {            
            public static int trainClearanceTime = 10; //unit = seconds
            public static int maintenanceClearanceTime = 10;//unit = seconds
            public static double trainLength = 90; //unit = meters
            public static double maxTrackSpeedLimit = 100; //unit = km/hr
            public static double upTrainSpeed = 80; //unit = km/hr
            public static double downTrainSpeed = 80; //unit = km/hr
            public static double maxCost = 100000000000;
            public static int maxConflictRepeatationCount = 10;
            public static int maxCountOfFeasibleSolutions = 50;
            public static int maxIterations = 10000;
            public static int maxPathSearchIterations = 10000;
            public static int freezeDuration = 3600;
            public static int maxDepartureDelayTime = 7200;


        }
    }
}
