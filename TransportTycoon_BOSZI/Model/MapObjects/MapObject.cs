using System.ComponentModel.DataAnnotations;
using TransportTycoon_BOSZI.Model;

namespace TransportTycoon_BOSZI.Model;

public abstract class MapObject
{
    private coord _coord;

    public coord Coord
    {
        get => _coord;
        set => _coord = value;
    }



}