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
using System.Net.Http;
using System.Xml.Linq;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace GroundCommunicationCoverageAddin_102
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class GroundCommCovToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.GraphicsLayer _graphicsLayer = null;
        string _serviceURL = null;
        LegendDialog pWin = null;
        public ObservableCollection<legend> _dtLegends = new ObservableCollection<legend>();
        string _baseURL = null;

        public GroundCommCovToolbar(MapWidget mapWidget, string serviceURL)
        {
            InitializeComponent();

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;
            _serviceURL = serviceURL;
            int pos = _serviceURL.IndexOf("GPServer");
            _baseURL = _serviceURL.Substring(0, pos);

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
            try
            {
                RunButton.IsEnabled = false;

                // When the user is finished with the toolbar, revert to the configured toolbar.
                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                    _mapWidget.SetToolbar(null);

                }
                int id = -1;
                int id2 = -1;
                for (int i = 0; i < _mapWidget.Map.Layers.Count; i++)
                {

                    client.Layer player = _mapWidget.Map.Layers[i];

                    if (player.ID == "GroundCommunicationCoverageMap")
                    {
                        id = i;
                    }
                    if (player.ID == "GroundCommunicationGraphics")
                        id2 = i;
                }
                if (id != -1)
                    _mapWidget.Map.Layers.RemoveAt(id);

                if (id2 != -1)
                    _mapWidget.Map.Layers.RemoveAt(id2 - 1);

                if (_graphicsLayer != null)
                    _graphicsLayer.Graphics.Clear();

                if (pWin != null)
                    pWin.Close();

                if (_dtLegends != null)
                    _dtLegends.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
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
                //WebMercator mercator = new ESRI.ArcGIS.Client.Projection.WebMercator();
                ///client.Geometry.MapPoint pt = null;
                //pt = mercator.ToGeographic(clickPoint) as client.Geometry.MapPoint;
                if (_graphicsLayer == null)
                {
                    _graphicsLayer = new client.GraphicsLayer();

                    _graphicsLayer.ID = "GroundCommunicationGraphics";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayer);
                }
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/GroundCommunicationCoverageAddin_102;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
                client.Graphic graphic = new client.Graphic();
                //graphic.Geometry = pt;
                graphic.Geometry = clickPoint;
                graphic.Symbol = mydictionary["RedPin"] as client.Symbols.MarkerSymbol;
                _graphicsLayer.Graphics.Add(graphic);

                if (_graphicsLayer != null)
                    if (_graphicsLayer.Graphics.Count() > 0)
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
                Geoprocessor gpGroundComCov = new Geoprocessor(_serviceURL);
                gpGroundComCov.JobCompleted += gpGroundComCov_JobCompleted;
                gpGroundComCov.Failed += gpGroundComCov_Failed;

                List<GPParameter> parameters = new List<GPParameter>();
                FeatureSet pFeatures = new FeatureSet(_graphicsLayer.Graphics);
                parameters.Add(new GPFeatureRecordSetLayer("FOBPoint", pFeatures));

                gpGroundComCov.ProcessSpatialReference = new SpatialReference(4326);
                gpGroundComCov.SubmitJobAsync(parameters);

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during run: " + ex.Message);
            }
        }

        void gpGroundComCov_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("There was an error executing the geoprocessing service:  " + e.Error.ToString());
        }

        async void gpGroundComCov_JobCompleted(object sender, JobInfoEventArgs e)
        {
            try
            {
                Geoprocessor gpGroundComCov = sender as Geoprocessor;
                client.ArcGISDynamicMapServiceLayer gpLayer = gpGroundComCov.GetResultMapServiceLayer(e.JobInfo.JobId);
                gpLayer.ID = "GroundCommunicationCoverageMap";
                gpLayer.Opacity = .50;

                _mapWidget.Map.Layers.Add(gpLayer);

                _mapWidget.Map.Layers.Add(_graphicsLayer);

                //get legend
                HttpClient client = new HttpClient();
                string response =
                    await client.GetStringAsync(_baseURL + "MapServer/legend?f=pjson");

                XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(response);
                XmlNodeList xmlnode = doc.GetElementsByTagName("legend");
                List<legend> pLegends = new List<legend>();
                _dtLegends.Clear();

                foreach (XmlNode node in xmlnode)
                {
                    legend pLegend = new legend();
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (child.Name == "label")
                            pLegend.label = child.InnerText;
                        if (child.Name == "url")
                            pLegend.url = _baseURL + "MapServer/1/images/" + child.InnerText;
                    }
                    _dtLegends.Add(pLegend);
                }

                if (pWin == null)
                    pWin = new LegendDialog();
                pWin.ListView.DataContext = _dtLegends;
                pWin.Closed += pWin_Closed;
                pWin.Show();
                pWin.Topmost = true;
            }
            catch (Exception ex)
            {

            }
        }
        void pWin_Closed(object sender, EventArgs e)
        {
            try
            {
                pWin = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int id = -1;
                int id2 = -1;
                for (int i = 0; i < _mapWidget.Map.Layers.Count; i++)
                {

                    client.Layer player = _mapWidget.Map.Layers[i];

                    if (player.ID == "GroundCommunicationCoverageMap")
                    {
                        id = i;
                    }
                    if (player.ID == "GroundCommunicationGraphics")
                        id2 = i;
                }
                if (id != -1)
                    _mapWidget.Map.Layers.RemoveAt(id);

                if (id2 != -1)
                    _mapWidget.Map.Layers.RemoveAt(id2 - 1);

                if (_graphicsLayer != null)
                    _graphicsLayer.Graphics.Clear();

                if (pWin != null)
                    pWin.Close();

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
                RunButton.IsEnabled = false;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }
    }
    public class legend
    {
        public string label { get; set; }
        public string url { get; set; }

    }
}
