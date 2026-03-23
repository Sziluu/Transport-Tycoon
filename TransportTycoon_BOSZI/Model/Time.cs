namespace TransportTycoon_BOSZI.Model;

public enum GameSpeed
{
    Stopped,
    Normal,
    Fast,
    VeryFast
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
        _currentSpeed = GameSpeed.Stopped;
        _tickCounter = 0;
        _day = 1;
    }

    public void Stop()
    {
        _currentSpeed = GameSpeed.Stopped;
    }

    public void Normal()
    {
        _currentSpeed = GameSpeed.Normal;
    }

    public void Fast()
    {
        _currentSpeed = GameSpeed.Fast;
    }

    public void VeryFast()
    {
        _currentSpeed = GameSpeed.VeryFast;
    }

    public int GetSpeedMultiplier()
    {
        return _currentSpeed switch
        {
            GameSpeed.Stopped => 0,
            GameSpeed.Normal => 1,
            GameSpeed.Fast => 2,
            GameSpeed.VeryFast => 4,
            _ => 1
        };
    }

    public void Tick()
    {
        if (_currentSpeed == GameSpeed.Stopped) return;

        _tickCounter++;
        if (_tickCounter >= 100) //100 tick = 1 nap
        {
            _tickCounter = 0;
            _day++;
        }
    }
}

