using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects
{
    public class Stop : MapObject
    {
        private Material _acceptedMaterial;

        public Material AcceptedMaterial
        {
            get => _acceptedMaterial;
            set => _acceptedMaterial = value;
        }
        public Stop()
        {
            _acceptedMaterial = Material.nothing;
        }

        public Stop(Material material)
        {
            _acceptedMaterial = material;
        }
    }
}
