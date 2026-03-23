namespace TransportTycoon_BOSZI.Model.MapObjects;

public class House : MapObject
{
    private bool _isPartOfCity;

    public bool IsPartOfCity
    {
        get => _isPartOfCity;
        set => _isPartOfCity = value;
    }
}
