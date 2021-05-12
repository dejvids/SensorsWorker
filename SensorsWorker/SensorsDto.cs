using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorsWorker
{
    public class SensorsDto
    {
        public double WaterTemp { get; set; }
        public double RoomTemp { get; set; }
        public double RoomHumidity { get; set; }
        public double RoomPressure { get; set; }
    }
}
