using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

namespace BufferMapTool
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class BufferMapToolBar : UserControl, IMapToolbar
    {
        private MapWidget _mapWidget = null;
        client.AcceleratedDisplayLayers _acLayers;
        private client.GraphicsLayer _bufferPointLayer;
        private client.GraphicsLayer _bufferPolygonLayer; 
        private ResourceDictionary _resourceDictionary;
        GeometryService _geometryService;

        private BufferParameters _bufferParams;
        private string _selectedRadioButtonName;

        private string _bufferDataSource; 
        private string _bufferField;

        //****************** Populate Resource Layers, Resource Types and Barriers combo boxes**// 
        //resource or facility type map services list
        public ObservableCollection<BufferLayer> BufferLayers { get; private set; }

        //resource or facility type map services list
        public ObservableCollection<BufferType> BufferTypes { get; private set; }

      
        public BufferMapToolBar(MapWidget mapWidget, String bufferDataSource, String bufferField)
        {
            InitializeComponent();
            this.DataContext = this; 

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;


            // The following parameters are used when user selecting features from a layer. 
            // DataSource and Field names need to be specified in the tool settings. 
            
            BufferLayers = new ObservableCollection<BufferLayer>();
            var selectLyr = new BufferLayer();
           

            if (bufferDataSource == null)
            {
                selectLyr.Name = "Use Settings for buffer layer";
                selectLyr.DataSource = null;
                BufferLayers.Add(selectLyr);
            }
            else
            {
                _bufferDataSource = bufferDataSource;
                _bufferField = bufferField;

                selectLyr.Name = "Select a layer";
                selectLyr.DataSource = null;
                BufferLayers.Add(selectLyr);

                ESRI.ArcGIS.OperationsDashboard.DataSource layerDataSource = null;

                IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                {
                    if (d.Id == _bufferDataSource)
                    {
                        layerDataSource = d;
                        break;
                    }

                }

                BufferTypes = new ObservableCollection<BufferType>();
                var bufferLayer = new BufferLayer();
                bufferLayer.Name = layerDataSource.Name;
                bufferLayer.DataSource = layerDataSource;
                BufferLayers.Add(bufferLayer);
            }
        }

        public void OnActivated()
        {
            // Add any code that applies to the entire toolbar. For example, add mouse handlers
            // to the Map of the map widget.
            if (_mapWidget != null)
            {
                setupGraphicsLayers();

                _resourceDictionary = new ResourceDictionary();
                _resourceDictionary.Source = new Uri("/BufferMapTool;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);

                _geometryService =
                    new GeometryService("http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer");
                _geometryService.BufferCompleted += GeometryService_BufferCompleted;
                _geometryService.Failed += GeometryService_Failed;
            }
        }


        public void OnDeactivated()
        {
            // Add any code that cleans up actions taken when activating the toolbar. 
            // For example, ensure any mouse handlers are removed.
            if (_mapWidget != null)
            {
                _mapWidget.Map.MouseClick -= Map_MouseClick;
            }
        }


        private void setupGraphicsLayers()
        {
            try
            {
                _acLayers = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;

                _bufferPolygonLayer = new GraphicsLayer() { ID = "bufferPolygonLayer" };
                _bufferPointLayer = new ESRI.ArcGIS.Client.GraphicsLayer() { ID = "bufferPointLayer" };
              
                if (_acLayers.Count() > 0)
                {
                    _acLayers.ChildLayers.Add(_bufferPolygonLayer);
                    _acLayers.ChildLayers.Add(_bufferPointLayer);
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }


        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {
            client.Geometry.MapPoint clickPoint = e.MapPoint;

            if (clickPoint != null)
            {
                //_bufferPointLayer.ClearGraphics();

                e.MapPoint.SpatialReference = _mapWidget.Map.SpatialReference;
                Graphic graphic = new Graphic()
                {
                    Geometry = e.MapPoint,
                    Symbol = _resourceDictionary["bufferSymbol"] as client.Symbols.PictureMarkerSymbol     
                };
                
                graphic.SetZIndex(1);
                _bufferPointLayer.Graphics.Add(graphic);
            }
        }

        void GeometryService_BufferCompleted(object sender, GraphicsEventArgs args)
        {
            _bufferPolygonLayer.Graphics.Clear();
            IList<Graphic> results = args.Results;
            foreach (Graphic graphic in results)
            {
                graphic.Symbol = _resourceDictionary["bufferZone"] as client.Symbols.SimpleFillSymbol; 
                _bufferPolygonLayer.Graphics.Add(graphic);
            }

            _mapWidget.Map.Extent = _bufferPolygonLayer.FullExtent.Expand(1.25); 
        }

        private void GeometryService_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Geometry Service error: " + e.Error);
        }

        private LinearUnit currentLinearUnit()
        {
            LinearUnit linearUnit = LinearUnit.StatuteMile; 
            
            var unit = cmbUnits.SelectedValue.ToString();

            if (unit == "Centimeters")
                linearUnit = LinearUnit.Centimeter;
            else if (unit == "Decimal Degrees")
                linearUnit = LinearUnit.Degree;
            else if (unit == "Feet")
                linearUnit = LinearUnit.Foot;
            else if (unit == "Inches")
                linearUnit = LinearUnit.InternationalInch;
            else if (unit == "Kilometers")
                linearUnit = LinearUnit.Kilometer;
            else if (unit == "Meters")
                linearUnit = LinearUnit.Meter;
            else if (unit == "Miles")
                linearUnit = LinearUnit.StatuteMile;
            
            return linearUnit; 
        }

        #region controls

        // ***********************************************************************************
        // * ...Set the map mouseclick event so that user can enter the incident
        // ***********************************************************************************
        private void btnSolve_Click(object sender, RoutedEventArgs e)
        {
            // If buffer spatial reference is GCS and unit is linear, geometry service will do geodesic buffering
            _bufferParams = new BufferParameters(); 
            _bufferParams.Unit = currentLinearUnit(); 
            _bufferParams.BufferSpatialReference = new SpatialReference(4326);
            _bufferParams.OutSpatialReference = _mapWidget.Map.SpatialReference;

            _bufferParams.Features.Clear();
            if (_selectedRadioButtonName == "rbDrawPoints" || _selectedRadioButtonName == "rbSelectedFeaturesFromLayer")
            {
                _bufferParams.Features.AddRange(_bufferPointLayer.Graphics);
            }
            else if (_selectedRadioButtonName == "rbSelectedFeaturesOnMap")
            {
                _bufferPointLayer.ClearGraphics();

                IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
                foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
                {
                    client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);
                    if (featureL.SelectionCount > 0)
                        _bufferParams.Features.AddRange(featureL.SelectedGraphics);
                }
            }

            if (_bufferParams.Features.Count == 0)
            {
                MessageBox.Show("Please select features on the map first", "Error");
                return;
            }

            var bufferDistance = Convert.ToDouble(ringInterval.Text);
            var ringDistance = bufferDistance; 

            var numRings = Convert.ToInt32(numberOfRings.Text); 
            for (var i = 0; i < numRings; i++)
            {
                _bufferParams.Distances.Add(ringDistance);
                ringDistance = ringDistance + bufferDistance; 
            }

            _geometryService.BufferAsync(_bufferParams);
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == null)
                return;
            _selectedRadioButtonName = radioButton.Name;

            if (_selectedRadioButtonName == "rbDrawPoints")
            {
                if (btnAddPoint != null)
                    btnAddPoint.IsEnabled = true;
            }
            else 
            {
                if (btnAddPoint != null)
                    btnAddPoint.IsEnabled = false;
                _mapWidget.Map.MouseClick -= Map_MouseClick;
            }
        }

        // ***********************************************************************************
        // * ...Set the map mouseclick event so that user can enter the incident
        // ***********************************************************************************
        private void btnAddPoint_Click(object sender, RoutedEventArgs e)
        {
            if (_mapWidget != null)
                _mapWidget.Map.MouseClick += Map_MouseClick;
        }

        // ***********************************************************************************
        // * Query the selected resource layer to get the different resource types... 
        // ***********************************************************************************
        private void cmbLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLayers.SelectedIndex > 0)
            {
                rbSelectedFeaturesFromLayer.IsChecked = true;
                BufferLayer bufferLayer = (BufferLayer)cmbLayers.SelectedItem;
                queryBufferLayer(bufferLayer.DataSource);
            }
        }

        // ***********************************************************************************
        // * Query the selected resource layer to get the different resource types... 
        // ***********************************************************************************
        //private void queryResourceLayer(string resourceLayerName)
        private async void queryBufferLayer(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource)
        {
            var query = new ESRI.ArcGIS.OperationsDashboard.Query();
            query.WhereClause = "1=1";
            query.ReturnGeometry = false;
            query.Fields = new string[] { _bufferField };

            var result = await dataSource.ExecuteQueryAsync(query);
            if (result == null || result.Features == null)
                return;
            else
                queryBufferLayer_ExecuteCompleted(result);
        }

        // ***********************************************************************************
        // * Query for the facilities is completed... populate facility type combobox
        // ***********************************************************************************
        void queryBufferLayer_ExecuteCompleted(QueryResult result)
        {
            BufferTypes.Clear();

            //set up facilities type dropdown 
            BufferType resourceType = new BufferType();
            resourceType.Name = "Select Type";
            BufferTypes.Add(resourceType);

            if (result != null && result.Features.Count > 0)
            {
                foreach (Graphic graphic in result.Features)
                {
                    if (graphic.Attributes[_bufferField] != null)
                    {
                        //string type = graphic.Attributes["MRPTYPE"].ToString();
                        string type = graphic.Attributes[_bufferField].ToString();
                        resourceType = new BufferType();
                        resourceType.Name = type;

                        var resourceItem = BufferTypes.FirstOrDefault(item => item.Name == resourceType.Name);
                        if (resourceItem == null) // none is found.
                            BufferTypes.Add(resourceType);
                    }
                }
                //cmbFacility.ItemsSource = _resourceTypes;
            }
            else
                System.Windows.MessageBox.Show("No features returned from query");
        }

        // ***********************************************************************************
        // * User selected a facility type... query for that facility type 
        // ***********************************************************************************
        private async void cmbFieldType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BufferType resourceType = (BufferType)cmbFacility.SelectedItem;

            if (resourceType.Name == "Select Type")
                return;

            var query = new ESRI.ArcGIS.OperationsDashboard.Query();
            query.WhereClause = _bufferField + "='" + resourceType.Name + "'";
            query.ReturnGeometry = true;
            query.SpatialFilter = _mapWidget.Map.Extent;
            query.Fields = new string[] { "*" };

            BufferLayer layer = (BufferLayer)cmbLayers.SelectedItem;

            var result = await layer.DataSource.ExecuteQueryAsync(query);
            if (result == null || result.Features == null)
                return;
            else
                queryResourceType_ExecuteCompleted(result);
        }

        // ***********************************************************************************
        // * Query for the facilities is completed... populate facility type combobox
        // ***********************************************************************************
        void queryResourceType_ExecuteCompleted(QueryResult result)
        {
            _bufferPointLayer.Graphics.Clear();
            if (result.Features != null && result.Features.Count > 0)
            {

                foreach (Graphic graphic in result.Features)
                {
                    SimpleMarkerSymbol sms = new SimpleMarkerSymbol()
                    {
                        Size = 16,
                        Style = SimpleMarkerSymbol.SimpleMarkerStyle.Diamond,
                    };
                    graphic.Symbol = sms;
                    _bufferPointLayer.Graphics.Add(graphic);
                }
            }
            else
                System.Windows.MessageBox.Show("No features returned from query");
        }


        // ***********************************************************************************
        // * ...User is done using the map tool. close the dialog. 
        // ***********************************************************************************
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            clearMapToolParameters();
            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
            {
                _mapWidget.SetToolbar(null);
            }
        }

        // ***********************************************************************************
        // * ...Clear the graphicslayers and charts
        // ***********************************************************************************
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            clearMapToolParameters();
        }

        private void clearMapToolParameters()
        {
            ////clear the graphicsLayer
            _bufferPointLayer.Graphics.Clear();
            _bufferPolygonLayer.Graphics.Clear();
            _bufferParams.Features.Clear();

            ////Clear selected features 
            IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;

            foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
            {
                client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);
                featureL.ClearSelection();
            }

            _mapWidget.Map.MouseClick -= Map_MouseClick;
        }
        #endregion
    }

    public class BufferType
    {
        public string Name { get; set; }
    }

    public class BufferLayer
    {
        public string Name { get; set; }
        public ESRI.ArcGIS.OperationsDashboard.DataSource DataSource { get; set; }
    }
}
