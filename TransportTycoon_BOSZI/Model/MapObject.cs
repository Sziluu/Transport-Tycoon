using TransportTycoon_BOSZI.Model;

namespace TransportTycoon_BOSZI;

public abstract class MapObject
{
    private coord _coord;
}

public class Stop: MapObject
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
