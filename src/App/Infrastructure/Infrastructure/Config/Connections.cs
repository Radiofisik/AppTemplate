using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Config
{
    public class Connections
    {
        public string DBConnectionString { get; set; }
        public string DBSchema { get; set; }

        public string RabbitConnectionString { get; set; }
        public string Queue { get; set; }

        public string ElasticSearchConnectionString { get; set; }
    }
}
