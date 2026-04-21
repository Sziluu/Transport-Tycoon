namespace TransportTycoon_BOSZI.Model.MapObjects;

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

public class Mine : industrial_MapObject
{
    private Material _kind;
    public Material Kind => _kind;

    public Mine()
    {
        _kind = Material.ironOre;
    }

    public Mine(Material kind)
    {
        _kind = kind;
    }
}

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

public class Farm : industrial_MapObject
{
    private Material _kind;
    public Material Kind => _kind;

    public Farm()
    {
        _kind = Material.wheat;
    }

    public Farm(Material kind)
    {
        _kind = kind;
    }
}
