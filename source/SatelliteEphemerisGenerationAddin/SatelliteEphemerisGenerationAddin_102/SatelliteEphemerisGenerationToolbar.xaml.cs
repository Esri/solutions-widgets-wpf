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
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;

namespace SatelliteEphemerisGenerationAddin_102
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class SatelliteEphemerisGenerationToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.GraphicsLayer _graphicsLayerPoly = null;
        client.GraphicsLayer _graphicsLayerLine = null;
        client.GraphicsLayer _graphicsLayerPoint = null;
        string _serviceURL = null;
        Geoprocessor gp = null;
        string _jobid = "";

        public SatelliteEphemerisGenerationToolbar(MapWidget mapWidget, string serviceURL)
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
                if (_graphicsLayerPoly != null)
                    _graphicsLayerPoly.Graphics.Clear();
                if (_graphicsLayerLine != null)
                    _graphicsLayerLine.Graphics.Clear();
                if (_graphicsLayerPoint != null)
                    _graphicsLayerPoint.Graphics.Clear();
            }
        }

        /// <summary>
        /// Mouse handler that sets the coordinates of the clicked point into text in the toolbar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {
        }
        private void ClickMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick += Map_MouseClick;
            }
            if (_graphicsLayerPoly != null)
                _graphicsLayerPoly.Graphics.Clear();
            if (_graphicsLayerLine != null)
                _graphicsLayerLine.Graphics.Clear();
            if (_graphicsLayerPoint != null)
                _graphicsLayerPoint.Graphics.Clear();


        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Geoprocessor gpSateEphemeris = new Geoprocessor(_serviceURL);
                gpSateEphemeris.JobCompleted += gpSateEphemeris_JobCompleted;
                gpSateEphemeris.Failed += gpSateEphemeris_Failed;

                List<GPParameter> parameters = new List<GPParameter>();
                parameters.Add(new GPString("SscNumber", sscnumber.Text));
                parameters.Add(new GPString("SensorProperties", "{'definition':'rectangular','horizontalHalfAngle':'20','verticalHalfAngle':'85.5'}"));
                parameters.Add(new GPDouble("TimeStepSeconds", System.Convert.ToDouble(timestep.Text)));
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
                if (StartDate.SelectedDate == null)
                    stop = new DateTime(StopDate.DisplayDate.Date.Year, StopDate.DisplayDate.Date.Month, StopDate.DisplayDate.Day, hour, minute, seconds);
                else
                    stop = new DateTime(StopDate.SelectedDate.Value.Date.Year, StopDate.SelectedDate.Value.Date.Month, StopDate.SelectedDate.Value.Date.Day, hour, minute, seconds);

                parameters.Add(new GPDate("StartTimeUtc", start.ToUniversalTime()));
                parameters.Add(new GPDate("StopTimeUtc", stop.ToUniversalTime()));

                gpSateEphemeris.OutputSpatialReference = new SpatialReference(4326);
                gpSateEphemeris.SubmitJobAsync(parameters);

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
            }
            catch
            {
                MessageBox.Show("Error in run");
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (_graphicsLayerPoly != null)
                _graphicsLayerPoly.Graphics.Clear();
            if (_graphicsLayerLine != null)
                _graphicsLayerLine.Graphics.Clear();
            if (_graphicsLayerPoint != null)
                _graphicsLayerPoint.Graphics.Clear();

        }
        private void gpSateEphemeris_JobCompleted(object sender, JobInfoEventArgs e)
        {
            try
            {
                if (_graphicsLayerPoly == null)
                {
                    _graphicsLayerPoly = new client.GraphicsLayer();
                    _graphicsLayerPoly.ID = "SensorFootprints";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayerPoly);
                }
                else
                    _graphicsLayerPoly.Graphics.Clear();
                if (_graphicsLayerLine == null)
                {
                    _graphicsLayerLine = new client.GraphicsLayer();
                    _graphicsLayerLine.ID = "EphemerisLines";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayerLine);
                }
                else
                    _graphicsLayerLine.Graphics.Clear();
                if (_graphicsLayerPoint == null)
                {
                    _graphicsLayerPoint = new client.GraphicsLayer();
                    _graphicsLayerPoint.ID = "EphemerisPoints";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayerPoint);
                }
                else
                    _graphicsLayerPoint.Graphics.Clear();
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/SatelliteEphemerisGenerationAddin_102;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                gp = sender as Geoprocessor;
                _jobid = e.JobInfo.JobId;
                gp.GetResultDataAsync(e.JobInfo.JobId, "SensorFootprints", "Footprints");
                gp.GetResultDataCompleted += gp_GetResultDataCompleted;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        void gpSateEphemeris_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Error executing gp service: " + e.Error.ToString());
        }

        void gp_GetResultDataCompleted(object sender, GPParameterEventArgs e)
        {
            try
            {
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/SatelliteEphemerisGenerationAddin_102;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                GPFeatureRecordSetLayer gpFLayer = e.Parameter as GPFeatureRecordSetLayer;
                if (gpFLayer.FeatureSet.Features.Count > 0)
                {
                    foreach (client.Graphic g in gpFLayer.FeatureSet.Features)
                    {
                        if (e.UserState.ToString() == "Footprints")
                        {
                            g.Symbol = mydictionary["BasicFillSymbol_Yellow_Trans_6"] as client.Symbols.SimpleFillSymbol;
                            _graphicsLayerPoly.Graphics.Add(g);
                        }
                        else if (e.UserState.ToString() == "Lines")
                        {
                            g.Symbol = mydictionary["BasicLineSymbol_Green_3"] as client.Symbols.SimpleLineSymbol;
                            _graphicsLayerLine.Graphics.Add(g);

                        }
                        else if (e.UserState.ToString() == "Points")
                        {
                            g.Symbol = mydictionary["BluePin"] as client.Symbols.MarkerSymbol;
                            _graphicsLayerPoint.Graphics.Add(g);
                        }

                    }
                }
                if (e.UserState.ToString() == "Footprints")
                    gp.GetResultDataAsync(_jobid, "EphemerisLines", "Lines");
                else if (e.UserState.ToString() == "Lines")
                    gp.GetResultDataAsync(_jobid, "EphemerisPoints", "Points");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetResultDataCompleted: " + e.UserState.ToString());
            }
        }
    }
}
