using System.ComponentModel.DataAnnotations;
using TransportTycoon_BOSZI.Model;

namespace TransportTycoon_BOSZI;

public abstract class MapObject
{
    private coord _coord;
    public coord Coord { get { return _coord; } set { _coord = value; } }
    private int _weight = 5; 
    public int Weight{ get { return _weight; } set { _weight = value; } }

}

public class Stop: MapObject
{
    
}
public class Empty : MapObject
{

}
public class Field : MapObject
{
    private int _numberOfTrees;
}

public class House : MapObject
{
    
}

public class Road : MapObject
{
    private int cost;
}

public class Water : MapObject
{
    
}

public class Garage : MapObject
{
    
}
public abstract class Bridge : MapObject
{
    private int _cost;
    private int _distanceToGoOver;
    private BridgeKind Kind;
}
