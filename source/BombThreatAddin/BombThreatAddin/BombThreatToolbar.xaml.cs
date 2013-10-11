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
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Symbols;

namespace BombThreatAddin
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class BombThreatToolbar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        ESRI.ArcGIS.Client.Geometry.MapPoint location = new MapPoint();
        client.GraphicsLayer _graphicsLayer = null;
        client.Geometry.Polygon outdoorPoly = new client.Geometry.Polygon();
        client.Geometry.Polygon indoorPoly = new client.Geometry.Polygon();
        bool callQuery = false; 

        public BombThreatToolbar(MapWidget mapWidget)
        {
            InitializeComponent();

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;
        }

        /// <summary>
        /// OnActivated is called when the toolbar is installed into the map widget.
        /// </summary>
        public void OnActivated()
        {
            // Add any code that applies to the entire toolbar. For example, add mouse handlers
            // to the Map of the map widget.
            bombType.Items.Add("Pipe bomb");
            bombType.Items.Add("Suicide vest");
            bombType.Items.Add("Briefcase/suitcase bomb");
            bombType.Items.Add("Sedan");
            bombType.Items.Add("SUV/van");
            bombType.Items.Add("Small delivery truck");
            bombType.Items.Add("Container/water truck");
            bombType.Items.Add("Semi-trailer");
            bombType.Text = bombType.Items[0].ToString();
            _mapWidget.Map.ExtentChanged += Map_ExtentChanged;

        }
        async void Map_ExtentChanged(object sender, ExtentEventArgs e)
        {
            try
            {
                if (callQuery)
                {
                    //Select features using polygon
                    IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                    foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                    {
                        //if (d.IsSelectable)
                        // {
                        System.Diagnostics.Debug.WriteLine(d.Name);
                        ESRI.ArcGIS.OperationsDashboard.Query query = new ESRI.ArcGIS.OperationsDashboard.Query();

                        query.SpatialFilter = outdoorPoly;
                        query.ReturnGeometry = true;
                        query.Fields = new string[] { d.ObjectIdFieldName };

                        ESRI.ArcGIS.OperationsDashboard.QueryResult result = await d.ExecuteQueryAsync(query);
                        //System.Diagnostics.Debug.WriteLine("Features : " + result.Features.Count);
                        if (result.Features.Count > 0)
                        {

                            // Get the array of IDs from the query results.
                            var resultOids = from feature in result.Features select System.Convert.ToInt32(feature.Attributes[d.ObjectIdFieldName]);

                            // Find the map layer in the map widget that contains the data source.
                            MapWidget mapW = MapWidget.FindMapWidget(d);
                            if (mapW != null)
                            {
                                // Get the feature layer in the map for the data source.
                                client.FeatureLayer featureL = mapW.FindFeatureLayer(d);

                                // NOTE: Can check here if the feature layer is selectable, using code shown above.
                                // For each result feature, find the corresponding graphic in the map by OID and select it.
                                foreach (client.Graphic feature in featureL.Graphics)
                                {
                                    int featureOid;
                                    int.TryParse(feature.Attributes[d.ObjectIdFieldName].ToString(), out featureOid);
                                    if (resultOids.Contains(featureOid))
                                    {
                                        feature.Select();
                                    }
                                }
                            }
                        }
                    }
                }
                callQuery = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in MapExent Changed: " + ex.Message);
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
                if (_graphicsLayer != null)
                    _graphicsLayer.Graphics.Clear();

                // Find the map layer in the map widget that contains the data source.
                IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                {

                    if (_mapWidget != null)
                    {
                        // Get the feature layer in the map for the data source.
                        client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);

                        //Clear Selection on Feature Layers in map
                        featureL.ClearSelection();

                    }
                }
                location = null;
                RunButton.IsEnabled = false;
                txtAddress.Text = "Enter Address";

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                    _mapWidget.SetToolbar(null);
                }
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
                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
                // Find the map layer in the map widget that contains the data source.
                IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                {

                    if (_mapWidget != null)
                    {
                        // Get the feature layer in the map for the data source.
                        client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);

                        //Clear Selection on Feature Layers in map
                        featureL.ClearSelection();

                    }
                }
                location = e.MapPoint;
                if (_graphicsLayer == null)
                {
                    _graphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer();
                    _graphicsLayer.ID = "BombThreatGraphics";
                    client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    if (aclyrs.Count() > 0)
                    {
                        aclyrs.ChildLayers.Add(_graphicsLayer);
                    }
                }

                _graphicsLayer.ClearGraphics();
                Graphic graphic = new ESRI.ArcGIS.Client.Graphic();
                graphic.Geometry = location;
                graphic.Attributes.Add("Evac", bombType.Text);
                ResourceDictionary mydictionary = new ResourceDictionary();
                mydictionary.Source = new Uri("/BombThreatAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                graphic.Symbol = mydictionary["DefaultClickSymbol"] as client.Symbols.MarkerSymbol;
                _graphicsLayer.MapTip = new ContentControl()
                {
                    ContentTemplate = (DataTemplate)Resources["kymaptip"]
                };
                _graphicsLayer.MapTip.SetBinding(ContentControl.ContentProperty, new Binding());
                graphic.SetZIndex(1);
                _graphicsLayer.Graphics.Add(graphic);

                if (location != null)
                    RunButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in mouseclick: " + ex.Message);
            }
        }
        void locatorTask_Failed(object sender, TaskFailedEventArgs e)
        {
        }
        public void GeometryService_BufferCompleted(object sender, GraphicsEventArgs args)
        {
            try
            {
                IList<Graphic> results = args.Results;
                //GraphicsLayer graphicsLayer = _mapControl.Layers["BombEvacLayer"] as GraphicsLayer;
                SimpleFillSymbol insideSymbol = new SimpleFillSymbol()
                {
                    Fill = new SolidColorBrush(Color.FromArgb(100, (byte)255, (byte)0, (byte)0)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, (byte)255, (byte)0, (byte)0)),
                    BorderThickness = 1
                };
                SimpleFillSymbol outsideSymbol = new SimpleFillSymbol()
                {
                    Fill = new SolidColorBrush(Color.FromArgb(100, (byte)232, (byte)158, (byte)28)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, (byte)232, (byte)158, (byte)28)),
                    BorderThickness = 1
                };
                int count = 0;
                Graphic extentGraphic = null;
                Graphic graphic1 = new Graphic();
                Graphic graphic2 = new Graphic();
                foreach (Graphic graphic in results)
                {
                    if (count == 1)
                    {
                        graphic.Symbol = outsideSymbol;
                        outdoorPoly = graphic.Geometry as client.Geometry.Polygon;
                        graphic.Attributes.Add("Evac", "Shelter-in-Place Zone");
                        graphic1 = graphic;
                    }
                    else
                    {
                        graphic.Symbol = insideSymbol;
                        indoorPoly = graphic.Geometry as client.Geometry.Polygon;
                        graphic.Attributes.Add("Evac", "Mandatory Evacuation Distance");
                        graphic2 = graphic;
                    }
                    count++;
                    extentGraphic = graphic;
                }
                _graphicsLayer.Graphics.Add(graphic1);
                _graphicsLayer.Graphics.Add(graphic2);

                _mapWidget.Map.ZoomTo(extentGraphic.Geometry);
                callQuery = true;

                ////Select features using polygon
                //IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                //foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                //{
                //    //if (d.IsSelectable)
                //   // {
                //        System.Diagnostics.Debug.WriteLine(d.Name);
                //        ESRI.ArcGIS.OperationsDashboard.Query query = new ESRI.ArcGIS.OperationsDashboard.Query();

                //        query.SpatialFilter = outdoorPoly;
                //        query.ReturnGeometry = true;
                //        query.Fields = new string[] { d.ObjectIdFieldName };

                //        QueryResult result = await d.ExecuteQueryAsync(query);
                //        System.Diagnostics.Debug.WriteLine("Features : " + result.Features.Count);
                //        if (result.Features.Count > 0)
                //        {

                //            // Get the array of IDs from the query results.
                //            var resultOids = from feature in result.Features select System.Convert.ToInt32(feature.Attributes[d.ObjectIdFieldName]);

                //            // Find the map layer in the map widget that contains the data source.
                //            MapWidget mapW = MapWidget.FindMapWidget(d);
                //            if (mapW != null)
                //            {
                //                // Get the feature layer in the map for the data source.
                //                client.FeatureLayer featureL = mapW.FindFeatureLayer(d);

                //                // NOTE: Can check here if the feature layer is selectable, using code shown above.
                //                // For each result feature, find the corresponding graphic in the map by OID and select it.
                //                foreach (client.Graphic feature in featureL.Graphics)
                //                {
                //                    int featureOid;
                //                    int.TryParse(feature.Attributes[d.ObjectIdFieldName].ToString(), out featureOid);
                //                    if (resultOids.Contains(featureOid))
                //                    {
                //                        feature.Select();
                //                    }
                //                }
                //            }
                //        //}
                //    }
                //}

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in geometry service complete: " + ex.Message);
            }
        }

        private void GeometryService_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Geometry Service error: " + e.Error);
        }

        private void MapButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick += Map_MouseClick;
            }
        }
        private void txtAddress_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            txtAddress.Text = "";

        }
        private void RunButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtAddress.Text == "Enter Address")
                {
                    int BuildingEvacDistance = 0;
                    int OutdoorEvacDistance = 0;

                    if (bombType.Text == "Pipe bomb")
                    {
                        BuildingEvacDistance = 70;
                        OutdoorEvacDistance = 1200;
                    }
                    else if (bombType.Text == "Suicide vest")
                    {
                        BuildingEvacDistance = 110;
                        OutdoorEvacDistance = 1750;
                    }
                    else if (bombType.Text == "Briefcase/suitcase bomb")
                    {
                        BuildingEvacDistance = 150;
                        OutdoorEvacDistance = 1850;
                    }
                    else if (bombType.Text == "Sedan")
                    {
                        BuildingEvacDistance = 320;
                        OutdoorEvacDistance = 1900;
                    }
                    else if (bombType.Text == "SUV/van")
                    {
                        BuildingEvacDistance = 400;
                        OutdoorEvacDistance = 2400;
                    }
                    else if (bombType.Text == "Small delivery truck")
                    {
                        BuildingEvacDistance = 640;
                        OutdoorEvacDistance = 3800;
                    }
                    else if (bombType.Text == "Container/water truck")
                    {
                        BuildingEvacDistance = 860;
                        OutdoorEvacDistance = 5100;
                    }
                    else if (bombType.Text == "Semi-trailer")
                    {
                        BuildingEvacDistance = 1570;
                        OutdoorEvacDistance = 9300;
                    }
                    if (BuildingEvacDistance == 0 || OutdoorEvacDistance == 0)
                        return;



                    //e.MapPoint.SpatialReference = _mapWidget.Map.SpatialReference;
                    //location = e.MapPoint;
                    if (location == null)
                        return;
                    Graphic graphic = new ESRI.ArcGIS.Client.Graphic();
                    graphic.Geometry = location;

                    GeometryService geometryTask = new GeometryService();
                    geometryTask.Url = "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer";
                    geometryTask.BufferCompleted += GeometryService_BufferCompleted;
                    geometryTask.Failed += GeometryService_Failed;

                    // If buffer spatial reference is GCS and unit is linear, geometry service will do geodesic buffering
                    BufferParameters bufferParams = new BufferParameters()
                    {
                        Unit = LinearUnit.SurveyFoot,
                        BufferSpatialReference = new SpatialReference(102004),
                        OutSpatialReference = _mapWidget.Map.SpatialReference
                    };
                    bufferParams.Features.Add(graphic);
                    double[] theDistances = new double[] { BuildingEvacDistance, OutdoorEvacDistance };
                    bufferParams.Distances.AddRange(theDistances);
                    geometryTask.BufferAsync(bufferParams);
                }
                else
                {
                    findAddress();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("error in run: " + ex.Message);
            }
        }
        private void ClearButton_Click_1(object sender, RoutedEventArgs e)
        {

            try
            {
                if (_graphicsLayer != null)
                    _graphicsLayer.Graphics.Clear();

                // Find the map layer in the map widget that contains the data source.
                IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                {

                    if (_mapWidget != null)
                    {
                        // Get the feature layer in the map for the data source.
                        client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);

                        //Clear Selection on Feature Layers in map
                        featureL.ClearSelection();

                    }
                }
                location = null;
                RunButton.IsEnabled = false;
                txtAddress.Text = "Enter Address";

                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        private void findAddress()
        {
            try
            {
                if (_mapWidget != null)
                {
                    _mapWidget.Map.MouseClick -= Map_MouseClick;
                }
                Locator locatorTask = new Locator("http://tasks.arcgisonline.com/ArcGIS/rest/services/Locators/TA_Address_NA_10/GeocodeServer");
                locatorTask.AddressToLocationsCompleted += new EventHandler<AddressToLocationsEventArgs>(locatorTask_AddressToLocationsCompleted);
                locatorTask.Failed += new EventHandler<TaskFailedEventArgs>(locatorTaskFindAddress_Failed);
                AddressToLocationsParameters addressParams = new AddressToLocationsParameters()
                {
                    OutSpatialReference = _mapWidget.Map.SpatialReference
                };
                Dictionary<string, string> address = addressParams.Address;
                address.Add("SingleLine", txtAddress.Text);
                txtAddress.Text = "Enter Address";
                locatorTask.AddressToLocationsAsync(addressParams);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("error in find address: " + ex.Message);
            }
        }
        void locatorTaskFindAddress_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Error locating address.");
        }
        void locatorTask_AddressToLocationsCompleted(object sender, AddressToLocationsEventArgs e)
        {
            try
            {
                List<AddressCandidate> returnedCandidates = e.Results;
                int BuildingEvacDistance = 0;
                int OutdoorEvacDistance = 0;

                if (bombType.Text == "Pipe bomb")
                {
                    BuildingEvacDistance = 70;
                    OutdoorEvacDistance = 1200;
                }
                else if (bombType.Text == "Suicide vest")
                {
                    BuildingEvacDistance = 110;
                    OutdoorEvacDistance = 1750;
                }
                else if (bombType.Text == "Briefcase/suitcase bomb")
                {
                    BuildingEvacDistance = 150;
                    OutdoorEvacDistance = 1850;
                }
                else if (bombType.Text == "Sedan")
                {
                    BuildingEvacDistance = 320;
                    OutdoorEvacDistance = 1900;
                }
                else if (bombType.Text == "SUV/van")
                {
                    BuildingEvacDistance = 400;
                    OutdoorEvacDistance = 2400;
                }
                else if (bombType.Text == "Small delivery truck")
                {
                    BuildingEvacDistance = 640;
                    OutdoorEvacDistance = 3800;
                }
                else if (bombType.Text == "Container/water truck")
                {
                    BuildingEvacDistance = 860;
                    OutdoorEvacDistance = 5100;
                }
                else if (bombType.Text == "Semi-trailer")
                {
                    BuildingEvacDistance = 1570;
                    OutdoorEvacDistance = 9300;
                }

                if (BuildingEvacDistance == 0 || OutdoorEvacDistance == 0)
                    return;

                if (e.Results.Count > 0)
                {
                    AddressCandidate candidate = returnedCandidates[0];

                    ResourceDictionary mydictionary = new ResourceDictionary();
                    mydictionary.Source = new Uri("/BombThreatAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
                    Graphic graphic = new ESRI.ArcGIS.Client.Graphic();
                    graphic.Geometry = candidate.Location;
                    graphic.Symbol = mydictionary["DefaultClickSymbol"] as client.Symbols.MarkerSymbol;

                    location = candidate.Location;
                    graphic.SetZIndex(1);
                    if (_graphicsLayer == null)
                    {
                        _graphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer();
                        _graphicsLayer.ID = "BombThreatGraphics";
                        client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                        if (aclyrs.Count() > 0)
                        {
                            aclyrs.ChildLayers.Add(_graphicsLayer);
                        }
                    }

                    _graphicsLayer.Graphics.Add(graphic);

                    GeometryService geometryTask = new GeometryService();
                    geometryTask.Url = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/Utilities/Geometry/GeometryServer"; geometryTask.BufferCompleted += GeometryService_BufferCompleted;
                    geometryTask.Failed += GeometryService_Failed;

                    // If buffer spatial reference is GCS and unit is linear, geometry service will do geodesic buffering
                    BufferParameters bufferParams = new BufferParameters()
                    {
                        Unit = LinearUnit.SurveyFoot,
                        BufferSpatialReference = new SpatialReference(102004),
                        OutSpatialReference = _mapWidget.Map.SpatialReference
                    };
                    bufferParams.Features.Add(graphic);
                    double[] theDistances = new double[] { BuildingEvacDistance, OutdoorEvacDistance };
                    bufferParams.Distances.AddRange(theDistances);
                    geometryTask.BufferAsync(bufferParams);

                }
                else
                {
                    MessageBox.Show("No address found.  Example schema: 380 New York Ave., Redlands, CA or click on the map");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Address location complete: " + ex.Message);
            }
        }

        private void txtAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtAddress.Text != "Enter Address")
                {
                    RunButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }


    }
}
