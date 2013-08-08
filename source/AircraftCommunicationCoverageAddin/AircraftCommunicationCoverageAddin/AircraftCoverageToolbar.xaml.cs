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


namespace AircraftCommunicationCoverageAddin
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class AircraftCoverageToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.GraphicsLayer _graphicsLayer = null;
        client.GraphicsLayer _graphicsLayerPoly = null;
        private client.Draw targetAreaDraw;
        string _serviceURL = null;
        LegendDialog pWin = null;
        public ObservableCollection<legend> _dtLegends = new ObservableCollection<legend>();
        string _baseURL = null;

        public AircraftCoverageToolbar(MapWidget mapWidget, string serviceURL)
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

                    if (player.ID == "AircraftCommunicationCoverageMap")
                    {
                        id = i;
                    }
                    if (player.ID == "AircraftCommunicationGraphics")
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
            client.Geometry.MapPoint clickPoint = e.MapPoint;
            //WebMercator mercator = new ESRI.ArcGIS.Client.Projection.WebMercator();
            ///client.Geometry.MapPoint pt = null;
            //pt = mercator.ToGeographic(clickPoint) as client.Geometry.MapPoint;
            if (_graphicsLayer == null)
            {
                _graphicsLayer = new client.GraphicsLayer();
                
                _graphicsLayer.ID = "AircraftCommunicationGraphics";
                client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                if (aclyrs.Count() > 0)
                    aclyrs.ChildLayers.Add(_graphicsLayer);
            }
            ResourceDictionary mydictionary = new ResourceDictionary();
            mydictionary.Source = new Uri("/AircraftCommunicationCoverageAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
            client.Graphic graphic = new client.Graphic();
            //graphic.Geometry = pt;
            graphic.Geometry = clickPoint;
            graphic.Symbol = mydictionary["RedPin"] as client.Symbols.MarkerSymbol;
            _graphicsLayer.Graphics.Add(graphic);

            if (_graphicsLayer != null && _graphicsLayerPoly != null)
                if (_graphicsLayer.Graphics.Count() > 0 && _graphicsLayerPoly.Graphics.Count() > 0)
                    RunButton.IsEnabled = true;
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
        private void CreateAreaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mapWidget != null)
                {
                       _mapWidget.Map.MouseClick -= Map_MouseClick;
                }

                targetAreaDraw = new client.Draw(_mapWidget.Map);
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/AircraftCommunicationCoverageAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                targetAreaDraw.FillSymbol = mydictionary["BasicFillSymbol_Yellow_Trans_6"] as client.Symbols.SimpleFillSymbol;
                targetAreaDraw.DrawMode = client.DrawMode.Polygon;
                targetAreaDraw.IsEnabled = true;
                targetAreaDraw.DrawComplete += targetAreaDraw_DrawComplete;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in create area: " + ex.Message);
            }
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Geoprocessor gpAircraftComms = new Geoprocessor(_serviceURL);
                gpAircraftComms.JobCompleted += gpAircraftComms_JobCompleted;
                gpAircraftComms.Failed += gpAircraftComms_Failed;

                List<GPParameter> parameters = new List<GPParameter>();
                FeatureSet pFeatures = new FeatureSet(_graphicsLayer.Graphics);
                parameters.Add(new GPFeatureRecordSetLayer("Waypoints", pFeatures));
                parameters.Add(new GPString("Aircraft_Model", aircraftmodel.Text));
                FeatureSet pPolygon = new FeatureSet(_graphicsLayerPoly.Graphics);
                parameters.Add(new GPFeatureRecordSetLayer("AreaTarget", pPolygon));

                parameters.Add(new GPDouble("Grid_Point_Granularity__deg_", System.Convert.ToDouble(gridgranularity.Text)));
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
                    start = new DateTime(StartDate.DisplayDate.Date.Year, StartDate.DisplayDate.Date.Month, StartDate.DisplayDate.Day, hour, minute, seconds);
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
                    stop = new DateTime(StopDate.DisplayDate.Year, StopDate.DisplayDate.Month, StopDate.DisplayDate.Day, hour, minute, seconds);
                else
                    stop = new DateTime(StopDate.SelectedDate.Value.Date.Year, StopDate.SelectedDate.Value.Date.Month, StopDate.SelectedDate.Value.Date.Day, hour, minute, seconds);

                parameters.Add(new GPDate("Start_Time__UTC_", start.ToUniversalTime()));
                parameters.Add(new GPDate("Stop_Time__UTC_", stop.ToUniversalTime()));

                gpAircraftComms.ProcessSpatialReference = new SpatialReference(4326);
                gpAircraftComms.SubmitJobAsync(parameters);

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in run: " + ex.Message);
            }
        }

        void gpAircraftComms_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("There was an error executing the geoprocessing service:  " + e.Error.ToString());
        }
        
       async void gpAircraftComms_JobCompleted(object sender, JobInfoEventArgs e)
        {
            try
            {
                if (_graphicsLayerPoly != null)
                    _graphicsLayerPoly.Graphics.Clear();

                Geoprocessor gpAircraftComCov = sender as Geoprocessor;
                client.ArcGISDynamicMapServiceLayer gpLayer = gpAircraftComCov.GetResultMapServiceLayer(e.JobInfo.JobId);
                gpLayer.ID = "AircraftCommunicationCoverageMap";
                gpLayer.Opacity = .65;

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
                System.Diagnostics.Debug.WriteLine(ex.Message);
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

                    if (player.ID == "AircraftCommunicationCoverageMap")
                    {
                        id = i;
                    }
                    if (player.ID == "AircraftCommunicationGraphics")
                        id2 = i;
                }
                if (id != -1)
                    _mapWidget.Map.Layers.RemoveAt(id);

                if (id2 != -1)
                    _mapWidget.Map.Layers.RemoveAt(id2-1);

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
        void targetAreaDraw_DrawComplete(object sender, client.DrawEventArgs e)
        {
            try
            {
                if (_graphicsLayerPoly == null)
                {
                    _graphicsLayerPoly = new client.GraphicsLayer();
                    _graphicsLayerPoly.ID = "AircraftCommunicationGraphicsPoly";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                        aclyrs.ChildLayers.Add(_graphicsLayerPoly);
                }
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/AircraftCommunicationCoverageAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                WebMercator mercator = new ESRI.ArcGIS.Client.Projection.WebMercator();
                client.Graphic g = new client.Graphic();
                //g.Geometry = mercator.ToGeographic(e.Geometry) as client.Geometry.Polygon;
                //g.Symbol = mydictionary["BasicFillSymbol_Yellow_Trans_6"] as client.Symbols.SimpleFillSymbol;
                g.Geometry = e.Geometry as client.Geometry.Polygon;
                g.Symbol = mydictionary["BasicFillSymbol_Yellow_Trans_6"] as client.Symbols.SimpleFillSymbol;
                _graphicsLayerPoly.Graphics.Add(g);
                targetAreaDraw.DrawMode = client.DrawMode.None;
                if (_graphicsLayer != null && _graphicsLayerPoly != null)
                    if (_graphicsLayer.Graphics.Count() > 0 && _graphicsLayerPoly.Graphics.Count() > 0)
                        RunButton.IsEnabled = true;

                targetAreaDraw.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in draw complete: " + ex.Message);
            }
        }

    }
    public class legend
    {
        public string label { get; set; }
        public string url { get; set; }

    }

}
