using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.Vehicles
{
    public class IronOreTransporter : Vehicle
    {
        public IronOreTransporter(Size size) : base(Speed.fast, size, Material.ironOre) { }
    }
}
