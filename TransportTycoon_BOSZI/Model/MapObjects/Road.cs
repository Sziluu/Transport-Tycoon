namespace TransportTycoon_BOSZI.Model.MapObjects;

public class Road : MapObject
{
    private int _cost;
    private bool _isPartOfCity;

    public int Cost
    {
        get => _cost;
        set => _cost = value;
    }

    public bool IsPartOfCity
    {
        get => _isPartOfCity;
        set => _isPartOfCity = value;
    }

    public Road()
    {
        _cost = 50;
        _isPartOfCity = false;
    }
}
