using System.ComponentModel.DataAnnotations;
using TransportTycoon_BOSZI.Model;

namespace TransportTycoon_BOSZI.Model;

public abstract class MapObject
{
    private Coord _coord;

    public Coord Coord
    {
        get => _coord;
        set => _coord = value;
    }



}