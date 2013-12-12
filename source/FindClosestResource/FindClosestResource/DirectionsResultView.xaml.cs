/* Copyright 2013 Esri
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace FindClosestResource
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class DirectionsResultView : UserControl, IMapToolbar, INotifyPropertyChanged
    {
        private MapWidget _mapWidget = null;

        readonly FindCloseFacilityResultView _closestFaculityResult;
        readonly FindClosestResourceToolbar _findClosestFacilityToolbar; 
        

        private ManeuverViewModel _currentManeuver = null;
        string _name;
        string _summary;

        public DirectionsResultView(MapWidget mapWidget, FindCloseFacilityResultView fcfResultView, RouteResult routeResult, FindClosestResourceToolbar fcrToolbar)
        {
            InitializeComponent();
            base.DataContext = this;

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;

            _closestFaculityResult = fcfResultView;
            _findClosestFacilityToolbar = fcrToolbar; 

         
            RouteName = routeResult.Directions.RouteName;
            Summary = string.Format("{0:F1} {1}, {2}", routeResult.Directions.TotalLength, "miles", FormatTime(routeResult.Directions.TotalTime));

            List<Graphic> features = new List<Graphic>(routeResult.Directions.Features);
            features.RemoveAt(0);

            List<ManeuverViewModel> directionElements = new List<ManeuverViewModel>();
            Graphic previous = null;
            int i = 1;

            foreach (var next in features)
            {
                ManeuverViewModel maneuver = new ManeuverViewModel(previous, next, i++);
                maneuver.Graphic.MouseLeftButtonDown += Graphic_MouseLeftButtonDown;

                directionElements.Add(maneuver);
                previous = next;
            }

            Maneuvers = directionElements;

        }
        private double _height = 0;
        private double _width = 0;
        private void btnMinMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.Width > 32)
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
        public ManeuverViewModel CurrentManeuver
        {
            get { return _currentManeuver; }
            set
            {
                if (_currentManeuver != null)
                    _currentManeuver.Selected = false;


                if (_currentManeuver == null)
                    return;

                _currentManeuver.Selected = true;
                    _currentManeuver.ZoomTo(_mapWidget.Map);
            }
        }

        void Graphic_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var graphic = sender as Graphic;
            foreach (var m in Maneuvers)
                if (m.Graphic == graphic)
                {
                    CurrentManeuver = m;
                    return;
                }
        }

       

        public string RouteName
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("RouteName");
            }
        }

        public string Summary
        {
            get { return _summary; }
            set
            {
                _summary = value;
                RaisePropertyChanged("Summary");
            }
        }

        public List<ManeuverViewModel> Maneuvers { get; private set; }


        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
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

        internal static string FormatTime(double minutes)
        {
            TimeSpan time = TimeSpan.FromMinutes(minutes);
            string result = "";
            int hours = (int)Math.Floor(time.TotalHours);
            if (hours > 1)
                result = hours.ToString() + " hours "; 
            else if (hours == 1)
                result = "1 hour ";  
            if (time.Minutes > 1)
                result += time.Minutes.ToString() + " min";
            else if (time.Minutes == 1)
                result += " 1 min";
            return result;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            _mapWidget.SetToolbar(_closestFaculityResult);
        }

        private void lbManuever_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _findClosestFacilityToolbar.HiglightRouteLayer.Graphics.Clear(); 
            ManeuverViewModel selectedManuever = (ManeuverViewModel) lbManuever.SelectedItems[0];
            _findClosestFacilityToolbar._mapWidget.Map.ZoomTo(selectedManuever.SegmentGraphic.Geometry.Extent.Expand(1.2));

            _findClosestFacilityToolbar.HiglightRouteLayer.Graphics.Add(selectedManuever.SegmentGraphic);
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            _findClosestFacilityToolbar.clearTheMap();

            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
                _mapWidget.SetToolbar(null);
        }

    }


    public class ManeuverViewModel
    {
        public ManeuverViewModel(Graphic fromGraphic, Graphic toGraphic, int number)
        {
            if (number == 1)
                Label = "A"; 
            else if (toGraphic.Attributes["maneuverType"].ToString() == "esriDMTStop")
                Label = "B";
            else
                Label = number.ToString();

            SetupGraphic(toGraphic, fromGraphic);

            SegmentGraphic = toGraphic;
            SegmentGraphic.Symbol = new SimpleLineSymbol()
            {
                Style = SimpleLineSymbol.LineStyle.Solid,
                Color = new SolidColorBrush(Color.FromRgb(204, 204, 0)),
                Width = 7
            };

            Directions = string.Format("{0}.  {1}", number, toGraphic.Attributes["text"]);

            if (fromGraphic != null)
            {
                var distance = Math.Round(Convert.ToDouble(fromGraphic.Attributes["length"]), 1);
                if (distance != 0)
                    Distance = string.Format("{0} {1}", distance, "miles");
            }

            _ManeuverMarkerSymbol = new SimpleMarkerSymbol()
            {
                Size = 20,
                Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle
            };
        }

        void SetupGraphic(Graphic graphic, Graphic previous)
        {
            if (graphic == null || graphic.Geometry == null)
                return;

            var ext = graphic.Geometry.Extent;
            if (ext.Width == 0 && ext.Height == 0 && previous != null)  // the destination maneuver is just a point, so you use the extent of the previous
                ext = previous.Geometry.Extent;

            ext = ext.Expand(1.2);
            var path = ((ESRI.ArcGIS.Client.Geometry.Polyline)graphic.Geometry).Paths[0];
            var firstPt = path[0];
            ext = new Envelope(firstPt.X - ext.Width / 2, firstPt.Y - ext.Height / 2, firstPt.X + ext.Width / 2, firstPt.Y + ext.Height / 2);

            Graphic = new Graphic() 
            { 
                Geometry = firstPt,
                Symbol = _ManeuverMarkerSymbol
            };
            Graphic.Attributes.Add("label", Label);

            Extent = ext;
        }

        private SimpleMarkerSymbol _ManeuverMarkerSymbol; 
        public string Label { get; private set; }
        public Envelope Extent { get; private set; }
        public Graphic Graphic { get; private set; }
        public Graphic SegmentGraphic { get; private set; }
        public string Distance { get; private set; }
        public string Directions { get; private set; }
        public System.Windows.Media.Geometry Glyph { get; private set; }

        internal void ZoomTo(Map map)
        {
            if (Extent == null)
                return;

            if (Extent.Width == 0 && Extent.Height == 0)
                map.PanTo(Extent);
            else
                map.ZoomTo(Extent);
        }

        public bool Selected
        {
            get
            {
                return Graphic == null ? false : Graphic.Selected;
            }
            set
            {
                if (Graphic == null)
                    return;

                if (value)
                    Graphic.Select();
                else
                    Graphic.UnSelect();
            }
        }
    }
}
