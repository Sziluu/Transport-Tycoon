using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.Vehicles
{
    public class PaperTransporter : Vehicle
    {
        public PaperTransporter(Size size) : base(Speed.fast, size, Material.paper) { }
    }
}
