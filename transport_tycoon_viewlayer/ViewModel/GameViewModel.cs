using System.Drawing;
using System.Windows.Input;
using System.Windows.Threading;
using TransportTycoon_BOSZI;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;

namespace transport_tycoon_viewlayer.ViewModel
{
    public enum ToolMode
    {
        none,
        buildRoad,
        placeStop,
        placeVehicle,
        selectDestination
    }

    public class GameViewModel : ViewModelBase
    {
        private GameModel _model;
        private DispatcherTimer _gameTimer;

        private ToolMode _currentTool;
        private string _moneyText;
        private string _dayText;
        private string _speedText;
        private string _statusText;
        private bool _isPaused;
        private bool _isGameOver;

        
        private readonly List<Vehicle> _unplacedVehicles;
        private Vehicle? _vehicleToPlace;
        private Point _vehicleStartStop;
        public HashSet<(int x, int y)> ValidStartStops { get; private set; } = new();
        public HashSet<(int x, int y)> ValidDestinationStops { get; private set; } = new();


        private readonly Material _selectedStopMaterial;

        public ToolMode CurrentTool
        {
            get => _currentTool;
            set { SetProperty(ref _currentTool, value); OnPropertyChanged(nameof(ToolInfo)); }
        }

        public string MoneyText
        {
            get => _moneyText;
            set => SetProperty(ref _moneyText, value);
        }

        public string DayText
        {
            get => _dayText;
            set => SetProperty(ref _dayText, value);
        }

        public string SpeedText
        {
            get => _speedText;
            set => SetProperty(ref _speedText, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public bool IsGameOver
        {
            get => _isGameOver;
            set => SetProperty(ref _isGameOver, value);
        }

        public string ToolInfo => _currentTool switch
        {
            ToolMode.buildRoad => "Click a tile to build a road",
            ToolMode.placeStop => "Click next to a road/city to place a stop",
            ToolMode.placeVehicle => "Click a GREEN stop (valid producer) to place the vehicle",
            ToolMode.selectDestination => "Click a BLUE stop (valid consumer) to set the destination",
            _ => ""
        };

        public GameModel Model => _model;
        public List<Vehicle> UnplacedVehicles => _unplacedVehicles;
        public Vehicle? VehicleToPlace => _vehicleToPlace;

        public ICommand BuildRoadCommand { get; }
        public ICommand PlaceStopCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand SpeedNormalCommand { get; }
        public ICommand SpeedFastCommand { get; }
        public ICommand SpeedVeryFastCommand { get; }
        public ICommand CancelToolCommand { get; }

        public event Action? RequestMapRedraw;
        public event Action? RequestVehicleRedraw;
        public event Action? RequestOpenVehicleShop;
        public event Action? RequestPauseMenu;

        public GameViewModel(Material selectedStopMaterial)
        {
            _selectedStopMaterial = selectedStopMaterial;
            _model = new GameModel(100, 20000);
            _moneyText = "";
            _dayText = "";
            _speedText = "";
            _statusText = "";
            _unplacedVehicles = new List<Vehicle>();

            BuildRoadCommand = new RelayCommand(_ => SetTool(ToolMode.buildRoad));
            PlaceStopCommand = new RelayCommand(_ => SetTool(ToolMode.placeStop));
            var relayCommand = new RelayCommand(_ => RequestOpenVehicleShop?.Invoke());
            if (relayCommand == null) throw new ArgumentNullException(nameof(relayCommand));
            PauseCommand = new RelayCommand(_ => TogglePause());
            ResumeCommand = new RelayCommand(_ => Resume());
            SpeedNormalCommand = new RelayCommand(_ => SetSpeed(GameSpeed.normal));
            SpeedFastCommand = new RelayCommand(_ => SetSpeed(GameSpeed.fast));
            SpeedVeryFastCommand = new RelayCommand(_ => SetSpeed(GameSpeed.veryFast));
            CancelToolCommand = new RelayCommand(_ => SetTool(ToolMode.none));

            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(150);
            _gameTimer.Tick += GameTimer_Tick;

            _model.MoneyChanged += (_, _) => UpdateUi();
            _model.MapChanged += (_, _) => RequestMapRedraw?.Invoke();
            _model.VehicleMoved += (_, _) => RequestVehicleRedraw?.Invoke();
            _model.GameOver += (_, _) =>
            {
                IsGameOver = true;
                _gameTimer.Stop();
                StatusText = "BANKRUPT! Game Over!";
            };
        }

        public void StartNewGame()
        {
            _gameTimer.Start();
            _model.StartNewGame();
            _unplacedVehicles.Clear();
            _vehicleToPlace = null;
            _vehicleStartStop = Point.Empty;
            ValidStartStops = new HashSet<(int x, int y)>();
            ValidDestinationStops = new HashSet<(int x, int y)>();
            CurrentTool = ToolMode.none;
            IsPaused = false;
            IsGameOver = false;
            StatusText = "";
            _gameTimer.Start();
            UpdateUi();
            RequestMapRedraw?.Invoke();
        }

        public void OnTileClicked(int gridX, int gridY)
        {
            if (IsPaused || IsGameOver) return;

            switch (_currentTool)
            {
                case ToolMode.buildRoad:
                    bool roadBuilt = _model.BuildRoad(gridX, gridY);
                    if (roadBuilt)
                        StatusText = $"Road built at ({gridX}, {gridY}) [-50$]";
                    else
                        StatusText = "Can't build road here!";
                    break;

                case ToolMode.placeStop:
                    bool stopPlaced = _model.PlaceStop(gridX, gridY, _selectedStopMaterial);
                    if (stopPlaced)
                    {
                        StatusText = $"Stop placed at ({gridX}, {gridY}) [-100$]";
                    }
                    else
                        StatusText = "Can't place stop here! Must be next to a road or city.";
                    break;

                case ToolMode.placeVehicle:
                    if (_vehicleToPlace != null)
                    {
                        if (!ValidStartStops.Contains((gridX, gridY)))
                        {
                            var tile = _model.Map.GetTile(gridX, gridY);
                            StatusText = tile is Stop
                                ? "That stop is not next to a valid producer for this vehicle type!"
                                : "Click a GREEN stop (valid producer) to place the vehicle.";
                            break;
                        }

                        bool placed = _model.PlaceVehicleAtStop(_vehicleToPlace, gridX, gridY);
                        if (placed)
                        {
                            _vehicleStartStop = new Point(gridX, gridY);
                            _unplacedVehicles.Remove(_vehicleToPlace);

                            var destList = _model.Map.GetValidDestinationStops(_vehicleToPlace.CarriedMaterial);
                            ValidDestinationStops = new HashSet<(int x, int y)>(destList);

                            if (ValidDestinationStops.Count == 0)
                            {
                                StatusText = "Vehicle placed, but no valid destination stops exist yet. Build a stop near a consumer first.";
                                _vehicleToPlace = null;
                                SetTool(ToolMode.none);
                            }
                            else
                            {
                                StatusText = $"Vehicle placed at ({gridX}, {gridY})! Now click a BLUE stop as the destination.";
                                SetTool(ToolMode.selectDestination);
                            }

                            RequestVehicleRedraw?.Invoke();
                        }
                        else
                        {
                            StatusText = "Could not place vehicle here!";
                        }
                    }
                    break;

                case ToolMode.selectDestination:
                    if (_vehicleToPlace != null)
                    {
                        if (!ValidDestinationStops.Contains((gridX, gridY)))
                        {
                            var tile = _model.Map.GetTile(gridX, gridY);
                            StatusText = tile is Stop
                                ? "That stop is not next to a valid consumer for this vehicle type!"
                                : "Click a BLUE stop (valid consumer) to set the destination.";
                            break;
                        }

                        var destPoint = new Point(gridX, gridY);
                        var stopList = new List<Point> { _vehicleStartStop, destPoint };
                        bool routeOk = CreateAndAssignRoute(_vehicleToPlace, stopList);

                        if (routeOk)
                        {
                            StatusText = $"Route created: ({_vehicleStartStop.X},{_vehicleStartStop.Y}) → ({gridX},{gridY}). Vehicle is on its way!";
                        }
                        else
                        {
                            StatusText = "Could not create route (no road connection?). Try a different destination.";
                            break;
                        }

                        _vehicleToPlace = null;
                        _vehicleStartStop = Point.Empty;

                        if (_unplacedVehicles.Count > 0)
                        {
                            _vehicleToPlace = _unplacedVehicles[0];
                            var startList = _model.Map.GetValidStartStops(_vehicleToPlace.CarriedMaterial);
                            ValidStartStops = new HashSet<(int x, int y)>(startList);
                            ValidDestinationStops = new HashSet<(int x, int y)>();
                            SetTool(ToolMode.placeVehicle);
                            StatusText += $" ({_unplacedVehicles.Count} more to place)";
                        }
                        else
                        {
                            SetTool(ToolMode.none);
                        }

                        RequestVehicleRedraw?.Invoke();
                    }
                    break;

                default:
                    ShowTileInfo(gridX, gridY);
                    break;
            }
            UpdateUi();
        }

        private void ShowTileInfo(int x, int y)
        {
            var tile = _model.Map.GetTile(x, y);

            string type = tile.GetType().Name;
            StatusText = $"[{x},{y}] {type}";

            if (tile is Stop s)
                StatusText += $" (accepts: {s.AcceptedMaterial})";
            else if (tile is industrial_MapObject ind)
                StatusText += $" (stored: {ind.StoredAmount})";
        }
        public void SetModel(GameModel newModel)
        {
            _model = newModel;

            _model.MoneyChanged += (_, _) => UpdateUi();
            _model.MapChanged += (_, _) => RequestMapRedraw?.Invoke();
            _model.VehicleMoved += (_, _) => RequestVehicleRedraw?.Invoke();
            _model.GameOver += (_, _) =>
            {
                IsGameOver = true;
                StatusText = "BANKRUPT! Game Over!";
            };

            UpdateUi();
            RequestMapRedraw?.Invoke();
        }
        
        public void BuyAndPlaceVehicle(string vehicleType, TransportTycoon_BOSZI.Model.Size size)
        {
            var vehicle = _model.BuyVehicle(vehicleType, size);
            if (vehicle != null)
            {
                _unplacedVehicles.Add(vehicle);
                _vehicleToPlace = vehicle;

                var startList = _model.Map.GetValidStartStops(vehicle.CarriedMaterial);
                ValidStartStops = new HashSet<(int x, int y)>(startList);
                ValidDestinationStops = new HashSet<(int x, int y)>();

                SetTool(ToolMode.placeVehicle);

                if (ValidStartStops.Count == 0)
                    StatusText = $"{vehicleType} purchased! No valid producer stops exist yet — build a stop next to a {vehicle.CarriedMaterial} producer first.";
                else
                    StatusText = $"{vehicleType} purchased! Click a GREEN stop (next to a {vehicle.CarriedMaterial} producer) to place it.";

                RequestMapRedraw?.Invoke();
            }
            else
            {
                StatusText = "Not enough money!";
            }
            UpdateUi();
        }

        public void SelectVehicleToPlace(Vehicle vehicle)
        {
            if (_unplacedVehicles.Contains(vehicle))
            {
                _vehicleToPlace = vehicle;
                var startList = _model.Map.GetValidStartStops(vehicle.CarriedMaterial);
                ValidStartStops = new HashSet<(int x, int y)>(startList);
                ValidDestinationStops = new HashSet<(int x, int y)>();
                SetTool(ToolMode.placeVehicle);
                StatusText = ValidStartStops.Count > 0
                    ? $"Click a GREEN stop (next to a {vehicle.CarriedMaterial} producer) to place the vehicle."
                    : $"No valid producer stops for {vehicle.CarriedMaterial}. Build a stop next to a producer first.";
                RequestMapRedraw?.Invoke();
            }
        }

        public bool CreateAndAssignRoute(Vehicle vehicle, List<Point> stops)
        {
            string routeName = $"Route_{_model.Routes.Count + 1}";
            var route = _model.CreateRoute(routeName, stops);
            if (route == null) return false;

            return _model.AssignRouteToVehicle(vehicle, route);
        }

        private void SetTool(ToolMode tool)
        {
            CurrentTool = tool;
            if (tool == ToolMode.none)
            {
                StatusText = "";
                _vehicleToPlace = null;
                _vehicleStartStop = Point.Empty;
                ValidStartStops = new HashSet<(int x, int y)>();
                ValidDestinationStops = new HashSet<(int x, int y)>();
            }
            RequestMapRedraw?.Invoke();
        }

        private void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
            {
                _model.Time.Stop();
                _gameTimer.Stop();
                IsPaused = true;
                RequestPauseMenu?.Invoke();
            }
            UpdateUi();
        }

        public void StopGame()
        {
            _model.Time.Stop();
            _gameTimer.Stop();
            IsPaused = true;
        }

        private void Resume()
        {
            _model.Time.Normal();
            _gameTimer.Start();
            IsPaused = false;
            UpdateUi();
        }

        private void SetSpeed(GameSpeed speed)
        {
            switch (speed)
            {
                case GameSpeed.normal: _model.Time.Normal(); break;
                case GameSpeed.fast: _model.Time.Fast(); break;
                case GameSpeed.veryFast: _model.Time.VeryFast(); break;
            }
            IsPaused = false;
            if (!_gameTimer.IsEnabled) _gameTimer.Start();
            UpdateUi();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            _model.Tick();
            UpdateUi();
            RequestVehicleRedraw?.Invoke();
        }

        private void UpdateUi()
        {
            MoneyText = $"{_model.Money:N0} $";
            DayText = $"Day: {_model.Time.Day}";
            SpeedText = _model.Time.CurrentSpeed switch
            {
                GameSpeed.stopped => "Paused",
                GameSpeed.normal => "Normal",
                GameSpeed.fast => "Fast",
                GameSpeed.veryFast => "Very Fast",
                _ => ""
            };
        }
    }
}
