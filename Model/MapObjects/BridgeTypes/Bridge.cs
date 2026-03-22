using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.BridgeTypes
{
    public abstract class Bridge : MapObject
    {
        private int _cost;
        private int _distanceToGoOver;
        private int _speedLimit;
        private BridgeKind _kind;

        public int Cost => _cost;
        public int DistanceToGoOver => _distanceToGoOver;
        public int SpeedLimit => _speedLimit;
        public BridgeKind Kind => _kind;

        protected Bridge(int cost, int distance, int speedLimit, BridgeKind kind)
        {
            _cost = cost;
            _distanceToGoOver = distance;
            _speedLimit = speedLimit;
            _kind = kind;
        }
    }

}
