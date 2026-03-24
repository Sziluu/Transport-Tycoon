using System;
using System.Collections.Generic;
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
namespace transport_tycoon_viewlayer.View
{
    /// <summary>
    /// Interaction logic for VehicleShopWindow.xaml
    /// </summary>
    public partial class VehicleShopWindow : Window
    {
        private GameViewModel _viewModel;
        private string? _selectedType;

        public VehicleShopWindow(GameViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            UpdateBalance();
        }

        private size GetSelectedSize()
        {
            if (SizeSmall.IsChecked == true) return size.Small;
            if (SizeBig.IsChecked == true) return size.Big;
            return size.Medium;
        }

        private void VehicleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VehicleList.SelectedItem is ListBoxItem item)
            {
                _selectedType = item.Tag?.ToString();
                UpdateDetails();
            }
        }

        private void Size_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectedType != null)
                UpdateDetails();
        }

        private void UpdateDetails()
        {
            if (_selectedType == null) return;

            var size = GetSelectedSize();
            int price = _viewModel.Model.GetVehiclePrice(_selectedType, size);

            string speedText = _selectedType == "Bus" ? "150 (mid)" : "200 (fast)";
            int capacity = size switch
            {
                size.Small => 50,
                size.Medium => 100,
                size.Big => 200,
                _ => 100
            };

            string material = _selectedType switch
            {
                "Bus" => "Passengers",
                "IronTransporter" => "Iron",
                "IronOreTransporter" => "Iron Ore",
                "CoalTransporter" => "Coal",
                "WoodTransporter" => "Wood",
                "PaperTransporter" => "Paper",
                "WheatTransporter" => "Wheat",
                "BreadTransporter" => "Bread",
                _ => "-"
            };

            //vehicle color preview
            Color vColor = _selectedType switch
            {
                "Bus" => Color.FromRgb(255, 200, 0),
                "IronTransporter" or "IronOreTransporter" => Color.FromRgb(60, 60, 60),
                "CoalTransporter" => Color.FromRgb(40, 40, 40),
                "WoodTransporter" or "PaperTransporter" => Color.FromRgb(50, 160, 50),
                "WheatTransporter" or "BreadTransporter" => Color.FromRgb(200, 60, 60),
                _ => Color.FromRgb(60, 120, 200)
            };

            VehicleColorBox.Fill = new SolidColorBrush(vColor);
            VehicleTypeLabel.Text = _selectedType;

            SpeedInfoLabel.Text = $"Speed: {speedText}";
            CapacityInfoLabel.Text = $"Capacity: {capacity}";
            MaterialInfoLabel.Text = $"Material: {material}";
            PriceInfoLabel.Text = $"Price: {price:N0} $";
            UpdateBalance();
        }

        private void UpdateBalance()
        {
            BalanceInfoLabel.Text = $"Balance: {_viewModel.Model.Money:N0} $";
        }

        private void Buy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedType == null)
            {
                MessageBox.Show("Select a vehicle type first!", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var size = GetSelectedSize();
            int price = _viewModel.Model.GetVehiclePrice(_selectedType, size);

            if (_viewModel.Model.Money < price)
            {
                MessageBox.Show("Not enough money!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _viewModel.BuyAndPlaceVehicle(_selectedType, size);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
