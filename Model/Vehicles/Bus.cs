using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.Vehicles
{
    public class Bus : Vehicle
    {
        public Bus(Speed speed, Size size) : base(speed, size, Material.nothing) { }
    }
}
