using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.BridgeTypes
{
    public class WoodBridge : Bridge
    {
        public WoodBridge() : base(100, 3, 60, BridgeKind.bad) { }
    }
}
