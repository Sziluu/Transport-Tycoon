using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.Industrial
{
    public class Farm : industrial_MapObject
    {
        private Material _kind;
        public Material Kind => _kind;

        public Farm()
        {
            _kind = Material.wheat;
        }

        public Farm(Material kind)
        {
            _kind = kind;
        }
    }
}
