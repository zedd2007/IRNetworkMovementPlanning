using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neto
{

    public class FinalExport
    {
        public List<ExportOutput> movements;

        public FinalExport()
        {
            this.movements = new();
        }
    }
    public class ExportOutput
    {
        public long id { get; set; }
        public string trainId { get; set; }
        public long scheduleId { get; set; }
        public DateTime movementStartTime { get; set; }
        public List<Segment> segments { get; set; }
        public string trainType { get; set; }
        //public long ScenarioId { get; set; }

        public ExportOutput()
        {

        }
    }

    public class Segment
    {
        public string type { get; set; }
        public List<Movement> path { get; set; }

        public Segment(string type, List<Movement> path)
        {
            this.type = type;
            this.path = path;
        }
    }

    public class Movement
    {
        public long circuitId { get; set; }

        public double offset { get; set; }

        public int timeOffset { get; set; }

        public int sequenceId { get; set; }

        public long divisionId { get; set; }

        public Movement(long circuitId, double offset, int timeOffset, int sequenceId, long divisionId)
        {
            this.circuitId = circuitId;
            this.offset = offset;
            this.timeOffset = timeOffset;
            this.sequenceId = sequenceId;
            this.divisionId = divisionId;
        }
    }
}
