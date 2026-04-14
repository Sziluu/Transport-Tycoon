using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using transport_tycoon_viewlayer.ViewModel;
using TransportTycoon_BOSZI;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.Vehicles;
using TransportTycoon_BOSZI.Persistance;

namespace transport_tycoon_viewlayer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameViewModel _viewModel;

        private const double TILE_W = 100, TILE_H = 65;
        private const double TW2 = 50, TH2 = 32;

        // camera
        private double _camX, _camY; // world offset
        private double _zoom = 0.6;
        private bool _dragging;
        private Point _dragStart;
        private double _dragCamX, _dragCamY;

        // sprite cache
        private Dictionary<string, BitmapImage?> _spriteCache = new();
        private string _assetsBase = "";

        // minimap
        private WriteableBitmap? _minimapBitmap; //pixelként rajzolja a minimapot, szín = tiel típus
        private Image? _minimapImage;
        private bool _mapDirty = true;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new GameViewModel();
            _assetsBase = System.IO.Path.GetFullPath(
                          System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets"));

            _viewModel.RequestMapRedraw += () => Dispatcher.Invoke(() => { _mapDirty = true; RenderViewport(); });
            _viewModel.RequestVehicleRedraw += () => Dispatcher.Invoke(RenderViewport);
            _viewModel.RequestPauseMenu += () => PauseOverlay.Visibility = Visibility.Visible;
            _viewModel.RequestOpenVehicleShop += OpenVehicleShop;

            _viewModel.PropertyChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(_viewModel.MoneyText)) MoneyLabel.Text = _viewModel.MoneyText;
                if (e.PropertyName == nameof(_viewModel.DayText)) DayLabel.Text = _viewModel.DayText;
                if (e.PropertyName == nameof(_viewModel.SpeedText)) SpeedLabel.Text = _viewModel.SpeedText;
                if (e.PropertyName == nameof(_viewModel.StatusText))
                {
                    StatusLabel.Text = _viewModel.StatusText;
                    int u = _viewModel.UnplacedVehicles.Count;
                    UnplacedLabel.Text = u > 0 ? $"Unplaced: {u}" : "";
                }
            });
        }

        private void StartNewGame_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            _viewModel.StartNewGame();
            int sz = _viewModel.Model.Map.Size;
            _camX = sz * TW2;
            _camY = sz * TH2 / 2;
            _mapDirty = true;
            _minimapImage = null;
            _minimapBitmap = null;
            RenderViewport();
        }
        private async void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            
            string savesDir = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Saves");
            Directory.CreateDirectory(savesDir);

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Game",
                Filter = "Transport Tycoon Save (*.ttsave)|*.ttsave",
                InitialDirectory = savesDir,
                FileName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.ttsave"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var dataAccess = new GameDataAccess(_viewModel.Model);
                await dataAccess.SaveGameAsync(dialog.FileName);
                StatusLabel.Text = $"Game saved: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save game:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load Game",
                Filter = "Transport Tycoon Save (*.ttsave)|*.ttsave",
                InitialDirectory = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Saves")
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var dataAccess = new GameDataAccess(_viewModel.Model);
                await dataAccess.LoadGameAsync(dialog.FileName);

                _viewModel.SetModel(dataAccess.Model);

                MainMenuPanel.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;

                // reset camera to center of loaded map
                int sz = _viewModel.Model.Map.Size;
                _camX = sz * TW2;
                _camY = sz * TH2 / 2;
                _mapDirty = true;
                _minimapImage = null;
                _minimapBitmap = null;

                RenderViewport();
                _viewModel.ResumeCommand.Execute(null);
                StatusLabel.Text = $"Game loaded: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load game:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void QuitGame_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void QuitToMenu_Click(object sender, RoutedEventArgs e)
        {
            PauseOverlay.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;
        }

        private void BuildRoad_Click(object sender, RoutedEventArgs e) => _viewModel.BuildRoadCommand.Execute(null);
        private void PlaceStop_Click(object sender, RoutedEventArgs e) => _viewModel.PlaceStopCommand.Execute(null);
        private void OpenVehicleShop_Click(object sender, RoutedEventArgs e) => OpenVehicleShop();
        private void CancelTool_Click(object sender, RoutedEventArgs e) => _viewModel.CancelToolCommand.Execute(null);
        private void Pause_Click(object sender, RoutedEventArgs e) => _viewModel.PauseCommand.Execute(null);
        private void Resume_Click(object sender, RoutedEventArgs e)
        { PauseOverlay.Visibility = Visibility.Collapsed; _viewModel.ResumeCommand.Execute(null); }
        private void SpeedNormal_Click(object sender, RoutedEventArgs e) => _viewModel.SpeedNormalCommand.Execute(null);
        private void SpeedFast_Click(object sender, RoutedEventArgs e) => _viewModel.SpeedFastCommand.Execute(null);
        private void SpeedVeryFast_Click(object sender, RoutedEventArgs e) => _viewModel.SpeedVeryFastCommand.Execute(null);

        private void OpenVehicleShop()
        {
            _viewModel.PauseCommand.Execute(null); // pause game during shop!
            PauseOverlay.Visibility = Visibility.Collapsed;
            var shop = new VehicleShopWindow(_viewModel);
            shop.Owner = this;
            shop.ShowDialog();
            _viewModel.ResumeCommand.Execute(null);
            RenderViewport();
        }
        
        private void Canvas_MouseRightDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true;
            _dragStart = e.GetPosition(GameCanvas);
            _dragCamX = _camX; _dragCamY = _camY;
            GameCanvas.CaptureMouse();
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var pos = e.GetPosition(GameCanvas);
            _camX = _dragCamX + (_dragStart.X - pos.X) / _zoom;
            _camY = _dragCamY + (_dragStart.Y - pos.Y) / _zoom;
            RenderViewport();
        }
        private void Canvas_MouseRightUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            GameCanvas.ReleaseMouseCapture();
        }
        
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double oldZoom = _zoom;
            _zoom *= e.Delta > 0 ? 1.15 : 0.87;
            _zoom = Math.Clamp(_zoom, 0.15, 2.0);

            var pos = e.GetPosition(GameCanvas);
            double wx = _camX + (pos.X - GameCanvas.ActualWidth / 2) / oldZoom;
            double wy = _camY + (pos.Y - GameCanvas.ActualHeight / 2) / oldZoom;
            _camX = wx - (pos.X - GameCanvas.ActualWidth / 2) / _zoom;
            _camY = wy - (pos.Y - GameCanvas.ActualHeight / 2) / _zoom;

            RenderViewport();
        }
        
        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(GameCanvas);

            double wx = _camX + (pos.X - GameCanvas.ActualWidth / 2) / _zoom;
            double wy = _camY + (pos.Y - GameCanvas.ActualHeight / 2) / _zoom;
            var (gx, gy) = WorldToGrid(wx, wy);

            int sz = _viewModel.Model.Map.Size;
            if (gx >= 0 && gx < sz && gy >= 0 && gy < sz)
            {
                _viewModel.OnTileClicked(gx, gy);
                RenderViewport();
            }
        }

        private void Minimap_Click(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(MinimapCanvas);
            int sz = _viewModel.Model.Map.Size;
            double sw = MinimapCanvas.ActualWidth > 0 ? MinimapCanvas.ActualWidth : 170;
            double sh = MinimapCanvas.ActualHeight > 0 ? MinimapCanvas.ActualHeight : 130;
            int gx = (int)(pos.X / sw * sz);
            int gy = (int)(pos.Y / sh * sz);
            var (wx, wy) = GridToWorld(gx, gy);
            _camX = wx; _camY = wy;
            RenderViewport();
        }

        
        private (double wx, double wy) GridToWorld(int gx, int gy)
        {
            int sz = _viewModel.Model.Map.Size;
            double ox = sz * TW2;
            return ((gx - gy) * TW2 + ox, (gx + gy) * TH2 + 20);
        }

        private (int gx, int gy) WorldToGrid(double wx, double wy)
        {
            int sz = _viewModel.Model.Map.Size;
            double ox = sz * TW2;
            double ax = wx - ox, ay = wy - 20;
            return ((int)Math.Floor((ax / TW2 + ay / TH2) / 2), (int)Math.Floor((ay / TH2 - ax / TW2) / 2));
        }

        // ===== VIEWPORT RENDERING =====
        private void RenderViewport() //csak a kamerából látható tileokat rajzolja ki koordinata tartomény szűréssel
        {
            GameCanvas.Children.Clear();
            if (_viewModel.Model?.Map?.GetMap == null) return;

            var map = _viewModel.Model.Map;
            int sz = map.Size;
            double cw = GameCanvas.ActualWidth > 0 ? GameCanvas.ActualWidth : 1280;
            double ch = GameCanvas.ActualHeight > 0 ? GameCanvas.ActualHeight : 600;

            
            double halfW = cw / 2 / _zoom;
            double halfH = ch / 2 / _zoom;
            double worldLeft = _camX - halfW - TILE_W;
            double worldRight = _camX + halfW + TILE_W;
            double worldTop = _camY - halfH - TILE_H * 2;
            double worldBot = _camY + halfH + TILE_H * 2;

            
            for (int gx = 0; gx < sz; gx++)
            {
                for (int gy = 0; gy < sz; gy++)
                {
                    var (wx, wy) = GridToWorld(gx, gy);
                    if (wx < worldLeft || wx > worldRight || wy < worldTop || wy > worldBot)
                        continue; 

                    double screenX = (wx - _camX) * _zoom + cw / 2;
                    double screenY = (wy - _camY) * _zoom + ch / 2;
                    double tileW = TILE_W * _zoom;
                    double tileH = TILE_H * _zoom;

                    MapObject tile = map.GetMap[gx, gy];
                    int z = gx + gy;

                    // ground sprite
                    string gndPath = GetGroundSprite(tile, gx, gy, map);
                    var gndImg = LoadImage(gndPath, tileW, tileH);
                    if (gndImg != null)
                    {
                        Canvas.SetLeft(gndImg, screenX - tileW / 2);
                        Canvas.SetTop(gndImg, screenY);
                        Canvas.SetZIndex(gndImg, z * 2);
                        GameCanvas.Children.Add(gndImg);
                    }

                    
                    string? bldPath = GetBuildingSprite(tile);
                    if (bldPath != null)
                    {
                        double bW = 99 * _zoom, bH = 85 * _zoom;
                        if (tile is Factory or Mine) { bW = 133 * _zoom; bH = 127 * _zoom; }

                        var bldImg = LoadImage(bldPath, bW, bH);
                        if (bldImg != null)
                        {
                            Canvas.SetLeft(bldImg, screenX - bW / 2);
                            Canvas.SetTop(bldImg, screenY + TH2 * _zoom - bH + 5 * _zoom);
                            Canvas.SetZIndex(bldImg, z * 2 + 1);
                            GameCanvas.Children.Add(bldImg);
                        }
                    }

                    
                    if (tile is Field f && f.NumberOfTrees > 0)
                        DrawTrees(screenX, screenY, f.NumberOfTrees, z);

                    
                    if (tile is Stop)
                    {
                        bool highlight = _viewModel.CurrentTool == ToolMode.PlaceVehicle;
                        double dotSize = 14 * _zoom;
                        if (highlight)
                        {
                            double ringSize = 28 * _zoom;
                            var ring = new Ellipse { Width = ringSize, Height = ringSize, Fill = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0)), Stroke = Brushes.Lime, StrokeThickness = 2 };
                            Canvas.SetLeft(ring, screenX - ringSize / 2); Canvas.SetTop(ring, screenY + TH2 * _zoom - ringSize / 2);
                            Canvas.SetZIndex(ring, z * 2 + 9); GameCanvas.Children.Add(ring);
                        }
                        var dot = new Ellipse { Width = dotSize, Height = dotSize, Fill = Brushes.OrangeRed, Stroke = Brushes.White, StrokeThickness = 2 * _zoom };
                        Canvas.SetLeft(dot, screenX - dotSize / 2); Canvas.SetTop(dot, screenY + TH2 * _zoom - dotSize / 2);
                        Canvas.SetZIndex(dot, z * 2 + 10); GameCanvas.Children.Add(dot);
                    }
                }
            }
            
            foreach (var v in _viewModel.Model.Vehicles)
            {
                if (v.CurrentPosition == System.Drawing.Point.Empty) continue;
                var (wx, wy) = GridToWorld(v.CurrentPosition.X, v.CurrentPosition.Y);
                double sx = (wx - _camX) * _zoom + cw / 2;
                double sy = (wy - _camY) * _zoom + ch / 2;
                if (sx < -50 || sx > cw + 50 || sy < -50 || sy > ch + 50) continue;

                double vw = 38 * _zoom, vh = 30 * _zoom;
                var vImg = LoadImage(GetVehicleSprite(v), vw, vh);
                if (vImg != null)
                {
                    vImg.Tag = "vehicle";
                    Canvas.SetLeft(vImg, sx - vw / 2); Canvas.SetTop(vImg, sy + TH2 * _zoom - vh / 2);
                    Canvas.SetZIndex(vImg, (v.CurrentPosition.X + v.CurrentPosition.Y) * 2 + 50);
                    GameCanvas.Children.Add(vImg);
                }
            }

            if (_mapDirty)
            {
                RenderMinimap();
                _mapDirty = false;
            }
        }

        private void DrawTrees(double sx, double sy, int count, int z)
        {
            string[] trees = { "kenney_isometric-roads/png/treeTall.png", "kenney_isometric-roads/png/coniferTall.png",
                           "kenney_isometric-roads/png/treeShort.png", "kenney_isometric-roads/png/coniferShort.png" };
            double[] ox = { -12, 12, -5, 10 };
            double[] oy = { -5, 0, 8, 5 };
            for (int i = 0; i < Math.Min(count, 4); i++)
            {
                var img = LoadImage(trees[i], 20 * _zoom, 44 * _zoom);
                if (img != null)
                {
                    Canvas.SetLeft(img, sx + ox[i] * _zoom - 10 * _zoom);
                    Canvas.SetTop(img, sy + oy[i] * _zoom - 10 * _zoom);
                    Canvas.SetZIndex(img, z * 2 + 5);
                    GameCanvas.Children.Add(img);
                }
            }
        }

        private string GetGroundSprite(MapObject tile, int gx, int gy, Map map)
        {
            string d = "kenney_isometric-roads/png/";
            if (tile is Road) return d + GetRoadTile(gx, gy, map);
            if (tile is Water) return d + "water.png";
            if (tile is Bridge) return d + "bridgeEW.png";
            if (tile is Stop) return d + "lotN.png";
            return d + "grass.png";
        }

        private string? GetBuildingSprite(MapObject tile)
        {
            string d = "kenney_isometric-buildings-1/PNG/";
            if (tile is House) return d + "buildingTiles_024.png";
            if (tile is Factory) return d + "buildingTiles_108.png";
            if (tile is Mine) return d + "buildingTiles_100.png";
            if (tile is Farm) return d + "buildingTiles_036.png";
            if (tile is Garage) return d + "buildingTiles_048.png";
            return null;
        }

        private string GetRoadTile(int x, int y, Map map)
        {
            bool n = IsRoadLike(map, x - 1, y); 
            bool s = IsRoadLike(map, x + 1, y); 
            bool e = IsRoadLike(map, x, y - 1); 
            bool w = IsRoadLike(map, x, y + 1); 

            if (n && e && s && w) return "crossroad.png";
            if (n && e && s) return "crossroadNES.png";
            if (e && s && w) return "crossroadESW.png";
            if (n && s && w) return "crossroadNSW.png";
            if (n && e && w) return "crossroadNEW.png";
            if (n && s) return "roadNS.png";
            if (e && w) return "roadEW.png";
            if (n && e) return "roadNE.png";
            if (n && w) return "roadNW.png";
            if (s && e) return "roadES.png";
            if (s && w) return "roadSW.png";
            if (n || s) return "roadNS.png";
            if (e || w) return "roadEW.png";
            return "road.png";
        }

        private bool IsRoadLike(Map map, int x, int y)
        {
            var t = map.GetTile(x, y);
            return t is Road or Stop or House or Bridge or Garage;
        }

        private string GetVehicleSprite(Vehicle v)
        {
            string d = "kenney_isometric-vehicles-1/PNG/";
            if (v is Bus) return d + "Taxi/taxi_NE.png";
            if (v is IronTransporter or IronOreTransporter) return d + "Civilian/Black/Truck 1/carBlack6_002.png";
            if (v is CoalTransporter) return d + "Civilian/Black/Truck 1/carBlack6_006.png";
            if (v is WoodTransporter or PaperTransporter) return d + "Civilian/Green/Truck 1/carGreen6_002.png";
            if (v is WheatTransporter or BreadTransporter) return d + "Civilian/Red/Truck 1/carRed6_002.png";
            return d + "Civilian/Blue/Truck 1/carBlue6_002.png";
        }

        // ===== Minimap =====
        private void RenderMinimap()
        {
            var map = _viewModel.Model.Map;
            int sz = map.Size;

            if (_minimapBitmap == null || _minimapBitmap.PixelWidth != sz)
                _minimapBitmap = new WriteableBitmap(sz, sz, 96, 96, PixelFormats.Bgr32, null);

            int stride = sz * 4;
            byte[] pixels = new byte[sz * sz * 4];
            for (int x = 0; x < sz; x++)
                for (int y = 0; y < sz; y++)
                {
                    Color c = GetMiniColor(map.GetMap[x, y]);
                    int idx = (y * sz + x) * 4;
                    pixels[idx] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                    pixels[idx + 3] = 255;
                }
            _minimapBitmap.WritePixels(new Int32Rect(0, 0, sz, sz), pixels, stride, 0);

            if (_minimapImage == null)
            {
                _minimapImage = new Image { Stretch = Stretch.Fill, IsHitTestVisible = false };
                MinimapCanvas.Children.Add(_minimapImage);
            }
            double cw = MinimapCanvas.ActualWidth > 0 ? MinimapCanvas.ActualWidth : 170;
            double ch = MinimapCanvas.ActualHeight > 0 ? MinimapCanvas.ActualHeight : 130;
            _minimapImage.Width = cw;
            _minimapImage.Height = ch;
            _minimapImage.Source = _minimapBitmap;
        }

        private Color GetMiniColor(MapObject t)
        {
            if (t is House) return Color.FromRgb(200, 180, 160);
            if (t is Road) return Color.FromRgb(80, 80, 80);
            if (t is Water) return Color.FromRgb(55, 140, 210);
            if (t is Mine) return Color.FromRgb(120, 100, 80);
            if (t is Factory) return Color.FromRgb(180, 60, 60);
            if (t is Farm) return Color.FromRgb(200, 180, 50);
            if (t is Stop) return Color.FromRgb(255, 150, 0);
            if (t is Field f && f.NumberOfTrees > 0) return Color.FromRgb(30, 100, 20);
            return Color.FromRgb(60, 140, 30);
        }

        
        private Image? LoadImage(string path, double w, double h)
        {
            var bmp = LoadBitmap(path);
            if (bmp == null) return null;
            return new Image { Source = bmp, Width = w, Height = h, IsHitTestVisible = false };
        }

        private BitmapImage? LoadBitmap(string rel)
        {
            if (_spriteCache.TryGetValue(rel, out var c)) return c;
            string full = System.IO.Path.Combine(_assetsBase, rel);
            if (!System.IO.File.Exists(full)) { _spriteCache[rel] = null; return null; }
            try
            {
                var b = new BitmapImage();
                b.BeginInit(); b.UriSource = new Uri(full, UriKind.Absolute);
                b.CacheOption = BitmapCacheOption.OnLoad; b.EndInit(); b.Freeze();
                _spriteCache[rel] = b; return b;
            }
            catch { _spriteCache[rel] = null; return null; }
        }
    }
}
