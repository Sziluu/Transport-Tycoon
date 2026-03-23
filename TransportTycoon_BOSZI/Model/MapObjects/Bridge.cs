namespace TransportTycoon_BOSZI.Model.MapObjects;

public abstract class Bridge : MapObject
{
    private int _cost;
    private int _distanceToGoOver;
    private int _speedLimit;
    private BridgeKind _kind;

    public int Cost => _cost;
    public int DistanceToGoOver => _distanceToGoOver;
    public int SpeedLimit => _speedLimit;
    public BridgeKind Kind => _kind;

    protected Bridge(int cost, int distance, int speedLimit, BridgeKind kind)
    {
        _cost = cost;
        _distanceToGoOver = distance;
        _speedLimit = speedLimit;
        _kind = kind;
    }
}
public class SteelBridge : Bridge
{
    public SteelBridge() : base(600, 12, 200, BridgeKind.best) { }
}
public class StoneBridge : Bridge
{
    public StoneBridge() : base(300, 6, 100, BridgeKind.good) { }
}
public class WoodBridge : Bridge
{
    public WoodBridge() : base(100, 3, 60, BridgeKind.bad) { }
}
