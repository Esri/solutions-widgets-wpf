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
using ESRI.ArcGIS.Client.Symbols;
using System.Net.Http;
using System.Xml.Linq;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace FarthestOnCircleAddin
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class FOCToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.GraphicsLayer _graphicsLayer = null;
        public ObservableCollection<legend> _dtLegends = new ObservableCollection<legend>();
        LegendDialog pWin = null;
        string _serviceURL = null;
        string _baseURL = null;

        public FOCToolbar(MapWidget mapWidget, string serviceURL)
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
            // Add any code that applies to the entire toolbar. For example, add mouse handlers
            // to the Map of the map widget.
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick -= Map_MouseClick;
            }
            
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

            }
            int id = -1;
            for (int i = 0; i < _mapWidget.Map.Layers.Count; i++)
            {

                client.Layer player = _mapWidget.Map.Layers[i];

                if (player.ID == "Farthest On Circle")
                {
                    id = i;
                }
            }

            if(id != -1)
                _mapWidget.Map.Layers.RemoveAt(id);

            if (_graphicsLayer != null)
                _graphicsLayer.Graphics.Clear();

            if (pWin != null)
                pWin.Close();
        }

        /// <summary>
        /// Mouse handler that sets the coordinates of the clicked point into text in the toolbar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {
            client.Geometry.MapPoint clickPoint = e.MapPoint;
            if (_graphicsLayer == null)
            {
                _graphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer();
                _graphicsLayer.ID = "FarthestOnCircleGraphics";
                client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                if (aclyrs.Count() > 0)
                {
                    aclyrs.ChildLayers.Add(_graphicsLayer);
                }
            }
            ResourceDictionary mydictionary = new ResourceDictionary();
            mydictionary.Source = new Uri("/FarthestOnCircleAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
            client.Graphic graphic = new client.Graphic();
            graphic.Geometry = clickPoint;
            graphic.Symbol = mydictionary["RedPin"] as client.Symbols.MarkerSymbol;
            _graphicsLayer.Graphics.Add(graphic);

        }

        async void gpFarthest_JobCompleted(object sender, JobInfoEventArgs e)
        {
            Geoprocessor gpFOC = sender as Geoprocessor;
            client.ArcGISDynamicMapServiceLayer gpLayer =  gpFOC.GetResultMapServiceLayer(e.JobInfo.JobId);
            gpLayer.ID = "Farthest On Circle";
            gpLayer.Opacity = .65;

            _mapWidget.Map.Layers.Add(gpLayer);
            
            //get legend
            HttpClient client = new HttpClient();
            string response =
                await client.GetStringAsync(_baseURL + "MapServer/legend?f=pjson");

            XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(response);
            XmlNodeList xmlnode = doc.GetElementsByTagName("legend");
            List<legend> pLegends = new List<legend>();
            int count = 0;
            double test = System.Convert.ToInt16(Range.Text) / System.Convert.ToInt16(Speed.Text);
            int theval = System.Convert.ToInt16(test);
            //decimal numRings = Math.Truncate(System.Convert.ToDecimal(test));
            foreach (XmlNode node in xmlnode)
            {
                legend pLegend = new legend();
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "label")
                        pLegend.label = child.InnerText + " Hours of Transit";
                    if (child.Name == "url")
                        pLegend.url = _baseURL + "MapServer/1/images/" + child.InnerText;
                }
                if (count <= theval && count < 24)
                    _dtLegends.Add(pLegend);

                count++;
            }
            

            if (pWin == null)
                pWin = new LegendDialog();
            pWin.ListView.DataContext = _dtLegends;
            pWin.Closed += pWin_Closed;
            // Provide feature action implementation.
            //_dtLegends.Clear();
            pWin.Show();
            pWin.Topmost = true;
        }
        void pWin_Closed(object sender, EventArgs e)
        {
            try
            {
                //if (_graphicsLayer != null)
                   // _graphicsLayer.Graphics.Clear();
                pWin = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        void gpFarthest_Failed(object sender, TaskFailedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Error.ToString());
        }

        private void ClearButton_Click_1(object sender, RoutedEventArgs e)
        {
            int id = -1;
            for(int i = 0; i < _mapWidget.Map.Layers.Count; i++)
            {

                client.Layer player = _mapWidget.Map.Layers[i];
                
                if (player.ID == "Farthest On Circle")
                {
                    id = i;
                }
            }
            if (id != -1)
                _mapWidget.Map.Layers.RemoveAt(id);
            if (_graphicsLayer != null)
                _graphicsLayer.Graphics.Clear();

            if (pWin != null)
                pWin.Close();
            
        }

        private void ClickMapButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick += Map_MouseClick;
            }
            if (_graphicsLayer != null)
                _graphicsLayer.Graphics.Clear();
            int id = -1;
            for (int i = 0; i < _mapWidget.Map.Layers.Count; i++)
            {

                client.Layer player = _mapWidget.Map.Layers[i];

                if (player.ID == "Farthest On Circle")
                {
                    id = i;
                }
            }
            if (id != -1)
                _mapWidget.Map.Layers.RemoveAt(id);

        }

        private void RunButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_graphicsLayer.Graphics.Count > 0)
                {
                    //Geoprocessor gpFarthest = new Geoprocessor("http://dtcrugmo01.esri.com:6080/arcgis/rest/services/Tasks/FarthestOnCircle/GPServer/Farthest%20On%20Circle");
                    Geoprocessor gpFarthest = new Geoprocessor(_serviceURL);
                    gpFarthest.JobCompleted += gpFarthest_JobCompleted;
                    gpFarthest.Failed += gpFarthest_Failed;
                    List<GPParameter> parameters = new List<GPParameter>();
                    FeatureSet pFeatures = new FeatureSet(_graphicsLayer.Graphics);
                    parameters.Add(new GPFeatureRecordSetLayer("Position_Last_Seen", pFeatures));
                    parameters.Add(new GPString("Range_for_Analysis_in_Nautical_Miles", Range.Text));
                    parameters.Add(new GPString("Average_Speed_in_Knots__kts__for_Analysis", Speed.Text));
                    gpFarthest.OutputSpatialReference = new SpatialReference(102100);

                    gpFarthest.SubmitJobAsync(parameters);

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

        private void Speed_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

    }
    public class Layer
    {
        int layerid;
        string layername;
        string layertype;
        int minscale;
        int maxscale;

        List<legend> legend;
        public int Layerid { get; set; }
        public string Layername { get; set; }
        public int Minscale { get; set; }
        public int Maxscale { get; set; }
        public List<legend> Legend { get; set; }
    }
    
    public class legend
    {
        public string label { get; set; }
        public string url { get; set; }

    }
}
