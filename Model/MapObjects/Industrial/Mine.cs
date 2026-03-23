using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.Industrial
{
    public class Mine : industrial_MapObject
    {
        private Material _kind;
        public Material Kind => _kind;

        public Mine()
        {
            _kind = Material.ironOre;
        }

        public Mine(Material kind)
        {
            _kind = kind;
        }
    }
}
