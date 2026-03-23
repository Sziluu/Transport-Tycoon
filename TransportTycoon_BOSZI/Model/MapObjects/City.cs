using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTycoon_BOSZI.Model
{
    public class City
    {
        private int _size;
        private List<MapObject> _fields;
        private string _name;
        private coord _topLeft; //bal felső sarok a térképen
        private int _population;
        private int _passengerDemand;

        public int Size => _size;
        public List<MapObject> Fields => _fields;
        public string Name => _name;
        public coord TopLeft => _topLeft;

        public int Population
        {
            get => _population;
            set => _population = value;
        }

        public int PassengerDemand
        {
            get => _passengerDemand;
            set => _passengerDemand = value;
        }

        public City()
        {
            _size = 3;
            _fields = new List<MapObject>();
            _name = "Város";
            _topLeft = new coord(0, 0);
            _population = 100;
            _passengerDemand = 5;
        }

        public City(string name, coord topLeft, int size)
        {
            _name = name;
            _topLeft = topLeft;
            _size = size;
            _fields = new List<MapObject>();
            _population = size * size * 20;
            _passengerDemand = _population / 20;
        }

        public void Grow()
        {
            _population += 5;
            _passengerDemand = _population / 20;
        }
    }

}
