namespace TransportTycoon_BOSZI.Model;

public enum GameSpeed
{
    stopped,
    normal,
    fast,
    veryFast
}

public class Time
{
    private GameSpeed _currentSpeed;
    private int _tickCounter;
    private int _day;

    public GameSpeed CurrentSpeed => _currentSpeed;
    public int TickCounter => _tickCounter;
    public int Day => _day;

    public Time()
    {
        _currentSpeed = GameSpeed.stopped;
        _tickCounter = 0;
        _day = 1;
    }

    public void Stop()
    {
        _currentSpeed = GameSpeed.stopped;
    }

    public void Normal()
    {
        _currentSpeed = GameSpeed.normal;
    }

    public void Fast()
    {
        _currentSpeed = GameSpeed.fast;
    }

    public void VeryFast()
    {
        _currentSpeed = GameSpeed.veryFast;
    }

    public int GetSpeedMultiplier()
    {
        return _currentSpeed switch
        {
            GameSpeed.stopped => 0,
            GameSpeed.normal => 1,
            GameSpeed.fast => 2,
            GameSpeed.veryFast => 4,
            _ => 1
        };
    }

    public void Tick()
    {
        if (_currentSpeed == GameSpeed.stopped) return;

        _tickCounter++;
        if (_tickCounter >= 100) //100 tick = 1 nap
        {
            _tickCounter = 0;
            _day++;
        }
    }
}

