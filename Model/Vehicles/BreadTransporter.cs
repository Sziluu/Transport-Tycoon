using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.Vehicles
{
    public class BreadTransporter : Vehicle
    {
        public BreadTransporter(Size size) : base(Speed.fast, size, Material.bread) { }
    }
}
