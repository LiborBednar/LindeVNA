using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noris.WS.ServiceGate;
using Noris.Clients.ServiceGate;
using NLog;


namespace LindeVNA
{
    internal static class Globals
    {
        public static ServiceGateConnector SgConnector;
        public static Logger Logger;
    }
}
