using System;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.OperationsDashboard;
using System.ComponentModel;

namespace FindClosestResource
{
    /// <summary>
    /// Interaction logic for FindCloseFacilityResultView.xaml
    /// </summary>
    public partial class FindCloseFacilityResultView : UserControl, IMapToolbar, INotifyPropertyChanged
    {
        private MapWidget _mapWidget = null;

        readonly FindClosestResourceToolbar _closestFacilityToolbar;

        private DirectionSummary _dirSummary = null;
        private ObservableCollection<DirectionSummary> _directions = new ObservableCollection<DirectionSummary>();

        private DirectionsResultView _directionsResultView = null; 
        
        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
          
        }
        
        public ObservableCollection<DirectionSummary> Directions
        {
            get { return _directions; }
            set
            {
                _directions = value;
                RaisePropertyChanged("Directions");
            }
        }

        // ***********************************************************************************
        // * Initialize Find Closest Facility Result View 
        // ***********************************************************************************
        public FindCloseFacilityResultView(FindClosestResourceToolbar findClosestToolbar, RouteResult[] routeResults, MapWidget mapWidget)
        {
            InitializeComponent();
            
            base.DataContext = this;
        
            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;

            _dirSummary = new DirectionSummary();
            _dirSummary.From = "From";
            _dirSummary.To = "To"; 
            _dirSummary.FieldType = "FieldType";
            _dirSummary.Rank = "Rank";
            _dirSummary.TotalTime = "Total Time";
            _dirSummary.TotalLength = "Total Length";
            _directions.Add(_dirSummary);
            
            _closestFacilityToolbar = findClosestToolbar;

            // add each route result to the result dialog. 
            foreach(RouteResult routeResult in routeResults)
            {
                DirectionsFeatureSet directionsFS = routeResult.Directions;
                string routeName = directionsFS.RouteName;
                int j = routeName.IndexOf("-");

                _dirSummary = new DirectionSummary();
                _dirSummary.From = routeName.Substring(0, j - 1);
                _dirSummary.To =  routeName.Substring(j + 2);
                _dirSummary.FieldType = _closestFacilityToolbar.FacilityType;
                _dirSummary.Rank = directionsFS.RouteID.ToString();
                _dirSummary.TotalTime = directionsFS.TotalDriveTime.ToString("0.0") + " minutes";
                _dirSummary.TotalLength = directionsFS.TotalLength.ToString("0.0") + " miles";
                _dirSummary.SelectedRoute = routeResult; 
                _directions.Add(_dirSummary);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            _mapWidget.SetToolbar(_closestFacilityToolbar);
        }

        private void lbDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbDirections.SelectedIndex == 0)
                return; 

            DirectionSummary direction = (DirectionSummary)lbDirections.SelectedItem;
            _directionsResultView = new DirectionsResultView(_mapWidget, this, direction.SelectedRoute, _closestFacilityToolbar);

            _mapWidget.Map.ZoomTo(direction.SelectedRoute.Route.Geometry.Extent.Expand(1.2));
            _mapWidget.SetToolbar(_directionsResultView); 
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion     


        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            _closestFacilityToolbar.clearTheMap();

            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
                _mapWidget.SetToolbar(null);
        }

        private double _height = 0;
        private double _width = 0;
        private void btnMinMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.ActualWidth > 32)
            {
                _height = this.ActualHeight;
                _width = this.ActualWidth;
                this.Height = 32;
                this.Width = 32;
                maximizeButton.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.Height = _height;
                this.Width = _width;
                maximizeButton.Visibility = System.Windows.Visibility.Hidden;
            }

        }
    }

    public class DirectionSummary
    {
        public DirectionSummary()
        { 
        }
        public string From { get; set; }
        public string To { get; set; }
        public string FieldType { get; set; }
        public string Rank { get; set; }
        public string TotalTime { get; set; }
        public string TotalLength { get; set; }
        public RouteResult SelectedRoute { get; set; }
    }
}
