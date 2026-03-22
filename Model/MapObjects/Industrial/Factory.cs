using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects.Industrial
{
    public class Factory : industrial_MapObject
    {
        private Material _using; //amit felhasznál
        private Material _kind;  //amit termel

        public Material Using => _using;
        public Material Kind => _kind;

        public Factory()
        {
            _using = Material.ironOre;
            _kind = Material.iron;
        }

        public Factory(Material usingMat, Material producing)
        {
            _using = usingMat;
            _kind = producing;
        }
    }

}
