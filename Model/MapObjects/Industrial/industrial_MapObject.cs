using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;

namespace TransportTycoon_BOSZI.Model.MapObjects.Industrial;

public abstract class industrial_MapObject : MapObject
{
    private (int, int) _size;
    private int _productionRate;
    private int _storedAmount;

    public (int, int) Size => _size;

    public int ProductionRate
    {
        get => _productionRate;
        set => _productionRate = value;
    }

    public int StoredAmount
    {
        get => _storedAmount;
        set => _storedAmount = value;
    }

    protected industrial_MapObject()
    {
        _size = (2, 2);
        _productionRate = 10;
        _storedAmount = 0;
    }

    public virtual void Produce()
    {
        _storedAmount += _productionRate;
        if (_storedAmount > 500) _storedAmount = 500;
    }

    public int TakeGoods(int amount)
    {
        int taken = Math.Min(amount, _storedAmount);
        _storedAmount -= taken;
        return taken;
    }
}
