using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.BridgeTypes
{
    public class StoneBridge : Bridge
    {
        public StoneBridge() : base(300, 6, 100, BridgeKind.good) { }
    }
}
