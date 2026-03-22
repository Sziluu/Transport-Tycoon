using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.BridgeTypes
{
    public class SteelBridge : Bridge
    {
        public SteelBridge() : base(600, 12, 200, BridgeKind.best) { }
    }
}
