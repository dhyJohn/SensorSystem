using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorTCPServer
{
    public class TankData
    {
        public int tank { get; set; }
        public string name { get; set; }
        public float volume { get; set; }
        public float capacity { get; set; }
        public float temp { get; set; }
        public float water { get; set; }

    }
}
