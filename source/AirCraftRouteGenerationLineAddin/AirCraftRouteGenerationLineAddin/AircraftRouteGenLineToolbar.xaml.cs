/* Copyright 2013 Esri
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.Client.Geometry;

namespace AirCraftRouteGenerationLineAddin
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class AircraftRouteGenLineToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.GraphicsLayer _graphicsLayer = null;
        string _serviceURL = null;

        public AircraftRouteGenLineToolbar(MapWidget mapWidget, string serviceURL)
        {
            InitializeComponent();

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;
            _serviceURL = serviceURL;
        }

        /// <summary>
        /// OnActivated is called when the toolbar is installed into the map widget.
        /// </summary>
        public void OnActivated()
        {
            
        }

        /// <summary>
        ///  OnDeactivated is called before the toolbar is uninstalled from the map widget. 
        /// </summary>
        public void OnDeactivated()
        {
            // Add any code that cleans up actions taken when activating the toolbar. 
            // For example, ensure any mouse handlers are removed.
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick -= Map_MouseClick;
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick -= Map_MouseClick;
                _mapWidget.SetToolbar(null);
                if (_graphicsLayer != null)
                    _graphicsLayer.Graphics.Clear();
                client.GraphicsLayer gLayer = _mapWidget.Map.Layers["ComputedLine"] as client.GraphicsLayer;
                if (gLayer != null)
                {
                    gLayer.Graphics.Clear();
                }
                RunButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Mouse handler that sets the coordinates of the clicked point into text in the toolbar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {
            try
            {
                client.Geometry.MapPoint clickPoint = e.MapPoint;
                WebMercator mercator = new ESRI.ArcGIS.Client.Projection.WebMercator();
                client.Geometry.MapPoint pt = null;
                pt = mercator.ToGeographic(clickPoint) as client.Geometry.MapPoint;
                if (_graphicsLayer == null)
                {
                    _graphicsLayer = new client.GraphicsLayer();
                    _graphicsLayer.ID = "ComputedPoints";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayer);
                }
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/AirCraftRouteGenerationLineAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
                client.Graphic graphic = new client.Graphic();
                graphic.Geometry = pt;
                graphic.Symbol = mydictionary["RedPin"] as client.Symbols.MarkerSymbol;
                _graphicsLayer.Graphics.Add(graphic);
                RunButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in map mouseclick: " + ex.Message);
            }
        }

        private void ClickMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick += Map_MouseClick;
            }
            if (_graphicsLayer != null)
                _graphicsLayer.Graphics.Clear();


        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_graphicsLayer.Graphics.Count > 0)
                {
                    Geoprocessor gpAircraftRouteGen = new Geoprocessor(_serviceURL);
                    gpAircraftRouteGen.JobCompleted += gpAircraftRouteGen_JobCompleted;
                    gpAircraftRouteGen.Failed += gpAircraftRouteGen_Failed;
                    List<GPParameter> parameters = new List<GPParameter>();
                    FeatureSet pFeatures = new FeatureSet(_graphicsLayer.Graphics);
                    parameters.Add(new GPFeatureRecordSetLayer("Route_Definition", pFeatures));
                    parameters.Add(new GPString("Aircraft_Model", aircraftmodel.Text));
                    parameters.Add(new GPDouble("Time_Step__s_", System.Convert.ToDouble(timestep.Text)));
                    string[] theTimevals = startTime.Text.Split(' ');
                    string[] theTime = theTimevals[0].Split(':');
                    int hour = 0;
                    int minute = 0;
                    int seconds = 0;
                    if (theTimevals[1] == "PM")
                        hour = System.Convert.ToInt16(theTime[0]) + 12;
                    else
                        hour = System.Convert.ToInt16(theTime[0]);
                    minute = System.Convert.ToInt16(theTime[1]);
                    seconds = System.Convert.ToInt16(theTime[2]);
                    DateTime start;
                    if (StartDate.SelectedDate == null)
                        start = new DateTime(StartDate.DisplayDate.Date.Year, StartDate.DisplayDate.Date.Month, StartDate.DisplayDate.Date.Day, hour, minute, seconds);
                    else
                        start = new DateTime(StartDate.SelectedDate.Value.Date.Year, StartDate.SelectedDate.Value.Date.Month, StartDate.SelectedDate.Value.Date.Day, hour, minute, seconds);

                    string[] theTimevalsStop = stopTime.Text.Split(' ');
                    string[] theTimeStop = theTimevalsStop[0].Split(':');
                    if (theTimevalsStop[1] == "PM")
                        hour = System.Convert.ToInt16(theTimeStop[0]) + 12;
                    else
                        hour = System.Convert.ToInt16(theTimeStop[0]);
                    minute = System.Convert.ToInt16(theTimeStop[1]);
                    seconds = System.Convert.ToInt16(theTimeStop[2]);

                    DateTime stop;
                    if (StopDate.SelectedDate == null)
                        stop = new DateTime(StopDate.DisplayDate.Date.Year, StopDate.DisplayDate.Date.Month, StopDate.DisplayDate.Date.Day, hour, minute, seconds);
                    else
                        stop = new DateTime(StopDate.SelectedDate.Value.Date.Year, StopDate.SelectedDate.Value.Date.Month, StopDate.SelectedDate.Value.Date.Day, hour, minute, seconds);


                    parameters.Add(new GPDate("Start_Time__UTC_", start.ToUniversalTime()));
                    parameters.Add(new GPDate("Stop_Time__UTC_", stop.ToUniversalTime()));

                    //gpAircraftRouteGen.OutputSpatialReference = new SpatialReference(102100);
                    gpAircraftRouteGen.OutputSpatialReference = new SpatialReference(4326);
                    gpAircraftRouteGen.SubmitJobAsync(parameters);

                    if (_mapWidget != null)
                    {
                        _mapWidget.Map.MouseClick -= Map_MouseClick;
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("error");
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (_graphicsLayer != null)
                _graphicsLayer.Graphics.Clear();
            client.GraphicsLayer gLayer = _mapWidget.Map.Layers["ComputedLine"] as client.GraphicsLayer;
            if (gLayer != null)
            {
                gLayer.Graphics.Clear();
            }
            RunButton.IsEnabled = false;
        }
        private void gpAircraftRouteGen_JobCompleted(object sender, JobInfoEventArgs e)
        {
            try
            {
                Geoprocessor gpAircraftRouteGen = sender as Geoprocessor;
                gpAircraftRouteGen.GetResultDataCompleted += gp_GetResultDataCompleted;
                gpAircraftRouteGen.GetResultDataAsync(e.JobInfo.JobId, "Computed_Points");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        void gpAircraftRouteGen_Failed(object sender, TaskFailedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Error.ToString());
        }

        void gp_GetResultDataCompleted(object sender, GPParameterEventArgs e)
        {
            _graphicsLayer.Graphics.Clear();
            ResourceDictionary mydictionary = new ResourceDictionary();
            mydictionary.Source = new Uri("/AirCraftRouteGenerationLineAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
            //client.SimpleRenderer simRen = mydictionary["RouteRenderer"] as client.SimpleRenderer;

            client.GraphicsLayer gLayer = _mapWidget.Map.Layers["ComputedLine"] as client.GraphicsLayer;
            if (gLayer == null)
            {
                gLayer = new client.GraphicsLayer();
                gLayer.ID = "ComputedLine";
                //gLayer.Renderer = simRen;
                //client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                //if (aclyrs.Count() > 0)
                _mapWidget.Map.Layers.Add(gLayer);
            }
            GPFeatureRecordSetLayer gpFLayer = e.Parameter as GPFeatureRecordSetLayer;
            if (gpFLayer.FeatureSet.Features.Count > 0)
            {
                foreach (client.Graphic g in gpFLayer.FeatureSet.Features)
                {
                    g.Symbol = mydictionary["BasicLineSymbol_Green_3"] as client.Symbols.LineSymbol;
                    gLayer.Graphics.Add(g);
                }
            }

        }
    }
}
