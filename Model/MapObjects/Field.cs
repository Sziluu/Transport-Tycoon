using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model.MapObjects
{
    public class Field : MapObject
    {
        private int _numberOfTrees;

        public int NumberOfTrees
        {
            get => _numberOfTrees;
            set => _numberOfTrees = value;
        }

        public void InfectOtherSquare() //erdő terjeszkedés
        {
            // majd implementálni
        }
    }

}
