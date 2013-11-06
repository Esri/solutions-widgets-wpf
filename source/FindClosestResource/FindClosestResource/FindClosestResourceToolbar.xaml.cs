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
using Xceed.Wpf.Toolkit;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Xml.Linq;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.Client;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace FindClosestResource
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    /// 
    public partial class FindClosestResourceToolbar : UserControl, IMapToolbar, INotifyPropertyChanged
    {

        private FindCloseFacilityResultView _result;
       
        public FindCloseFacilityResultView Result
        {
            get { return _result; }
            set
            {
                _result = value;
                RaisePropertyChanged("Result");
            }
        }
        
        private string _facilityType;
        public string FacilityType
        {
            get { return _facilityType; }
            set
            {
                _facilityType = value;
                RaisePropertyChanged("FacilityType");
            }
        }

        public MapWidget _mapWidget = null;

        //set up routeTask 
        RouteTask _routeTask = null;

        // ********************************************************************
        // create the add barrier graphicslayers 
        client.GraphicsLayer _polylineBarriersGraphicLayer = null;
        private string _polylineBarriersGraphicsLayerID = "polylineBarrierGraphicsLayer";
        private LineSymbol _polylineBarrierSymbol = null;

        private client.GraphicsLayer _pointBarriersGraphicsLayer = null;
        private string _pointBarriersGraphicsLayerID = "pointBarriersGraphicsLayer";

        private client.GraphicsLayer _polygonBarriersGraphicsLayer = null;
        private string _polygonBarriersGraphicsLayerID = "polygonBarriersGraphicsLayer";
        // ********************************************************************

        //****************** Populate Resource Layers, Resource Types and Barriers combo boxes**// 
        //resource or facility type map services list
        public ObservableCollection<ResourceLayer> ResourceLayers { get; private set; }

        // map service layers to be used barrier types... 
        public ObservableCollection<ResourceLayer> BarriersDataSouces; 

        //resource or facility type map services list
        public ObservableCollection<ResourceType> ResourceTypes {get; private set;}

        private string _resourceTypeField; 
        //******************************************************************************************
        
        //set up incidentsGraphicsLayer 
        client.GraphicsLayer _incidentsGraphicsLayer = null;
        private string _incidentsGraphicsLayerID = "incidentsGraphicsLayer";

        //set up facilitiesGraphicsLayer 
        client.GraphicsLayer _facilitiesGraphicsLayer = null;
        private string _facilitiesGraphicsLayerID = "facilitiesGraphicsLayer";

        //Set up the routesGraphicsLayer 
        client.GraphicsLayer _routesGraphicsLayer = null;
        private string _routesGraphicsLayerID = "routesGraphicsLayer";

        //display route rank numbers on the map
        private GraphicsLayer _routeLabelsGraphicsLayer = null;
        private string _routeLabelsGraphicsLayerID = "routeLabelsGraphicsLayer"; 

        //highlight route layer used to zoom into route segments 
        public GraphicsLayer HiglightRouteLayer = null;
        private string _highlightRouteLayerID = "higlightRouteGraphicsLayer";

        private SimpleMarkerSymbol _incidentMarkerSymbol = null;
        private client.Draw _drawObject = null;

        public FindClosestResourceToolbar(MapWidget mapWidget, ObservableCollection<ESRI.ArcGIS.OperationsDashboard.DataSource> resourceDatasources, String resourceTypeField, 
            ObservableCollection<ESRI.ArcGIS.OperationsDashboard.DataSource> barriersDataSources)
        {

            InitializeComponent();
            this.DataContext = this; 

            // Store a reference to the MapWidget that the toolbar has been installed to.
            _mapWidget = mapWidget;

            //set up the route task with find closest facility option 
            _routeTask = new RouteTask("http://sampleserver3.arcgisonline.com/ArcGIS/rest/services/Network/USA/NAServer/Closest%20Facility");
            //_routeTask = new RouteTask("http://route.arcgis.com/arcgis/rest/services/World/ClosestFacility/NAServer/ClosestFacility_World/solveClosestFacility");
            _routeTask.SolveClosestFacilityCompleted += SolveClosestFacility_Completed;
            _routeTask.Failed += SolveClosestFacility_Failed;

            //check if the graphicslayers need to be added to the map. 
            setupGraphicsLayer(); 

            //set up the resources/facilities datasource in the combobox 
            setupResourcesDataSource(resourceDatasources); 

            ResourceTypes = new ObservableCollection<ResourceType>();
            _resourceTypeField = resourceTypeField; 

            //set up the barriers types combobox 
            //this will have to be read from the config file...
            BarriersDataSouces = new ObservableCollection<ResourceLayer>();
            //set up facilities type dropdown 
            ResourceLayer barrierLayer = new ResourceLayer();
            barrierLayer.Name = "Select Barrier";
            barrierLayer.DataSource = null;
            BarriersDataSouces.Add(barrierLayer);

            //Barriers - passed from the configurar
            foreach (ESRI.ArcGIS.OperationsDashboard.DataSource datasource in barriersDataSources)
            {
                barrierLayer = new ResourceLayer();
                barrierLayer.Name = datasource.Name; 
                barrierLayer.DataSource = datasource; 
                BarriersDataSouces.Add(barrierLayer);
            }

            cmbBarriers.ItemsSource = BarriersDataSouces;

            _incidentMarkerSymbol = new SimpleMarkerSymbol
            {
                Size = 20,
                Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle
            };

            //polyline barrier layer symbol
            _polylineBarrierSymbol = new client.Symbols.SimpleLineSymbol()
            {
                Color = new SolidColorBrush(Color.FromRgb(138, 43, 226)),
                Width = 5
            };

        }

        // ***********************************************************************************
        // * Setup resources datasource for the map tool. 
        // ***********************************************************************************
        private void setupResourcesDataSource(ObservableCollection<ESRI.ArcGIS.OperationsDashboard.DataSource> resourceDatasources)
        {
            if (resourceDatasources.Count == 0)
                return;
            else
            {
                ////set up resources/facilities layer  
                ResourceLayers = new ObservableCollection<ResourceLayer>();
                //var selectLyr = new ResourceLayer();
                //selectLyr.Name = "Select a layer";
                //selectLyr.DataSource = null;
                //ResourceLayers.Add(selectLyr);

                var resourceLayer = new ResourceLayer();
                resourceLayer.Name = resourceDatasources[0].Name;
                resourceLayer.DataSource = resourceDatasources[0];
                ResourceLayers.Add(resourceLayer);
            }
        }

        // ***********************************************************************************
        // * Setup all graphicslayers that will be used by the map tool 
        // ***********************************************************************************
        private void setupGraphicsLayer()
        {
            //Check if the graphicslayers are in the map if so then skip adding the layers... 
            //... barriers ... 
            //create and add barrier graphicslayer to the map
            // Find the AcceleratedDisplayLayers collection from the Map and add the barrier graphics layer
            client.AcceleratedDisplayLayers acLayers = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
            if (acLayers.ChildLayers[_polylineBarriersGraphicsLayerID] == null)
            {
                _polylineBarriersGraphicLayer = new client.GraphicsLayer()
                {
                    ID = _polylineBarriersGraphicsLayerID,
                };
                acLayers.ChildLayers.Add(_polylineBarriersGraphicLayer);
            }
            else
                _polylineBarriersGraphicLayer = (GraphicsLayer)acLayers.ChildLayers[_polylineBarriersGraphicsLayerID];

            //set up polygon barriers
            if (acLayers.ChildLayers[_polygonBarriersGraphicsLayerID] == null)
            {
                _polygonBarriersGraphicsLayer = new client.GraphicsLayer()
                {
                    ID = _polygonBarriersGraphicsLayerID
                };

                acLayers.ChildLayers.Add(_polygonBarriersGraphicsLayer);
            }
            else
                _polygonBarriersGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_polygonBarriersGraphicsLayerID];

            //set up routes graphics layer 
            //this layer will contain find closest facility results 
            if (acLayers.ChildLayers[_routesGraphicsLayerID] == null)
            {
                _routesGraphicsLayer = new client.GraphicsLayer()
                {
                    ID = _routesGraphicsLayerID,
                };
                acLayers.ChildLayers.Add(_routesGraphicsLayer);
            }
            else
                _routesGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_routesGraphicsLayerID];

            if (acLayers.ChildLayers[_highlightRouteLayerID] == null)
            {
                HiglightRouteLayer = new client.GraphicsLayer()
                {
                    ID = _highlightRouteLayerID
                };
                acLayers.ChildLayers.Add(HiglightRouteLayer);
            }
            else
                HiglightRouteLayer = (GraphicsLayer)acLayers.ChildLayers[_highlightRouteLayerID];

            if (acLayers.ChildLayers[_routeLabelsGraphicsLayerID] == null)
            {
                _routeLabelsGraphicsLayer = new GraphicsLayer()
                {
                    ID = _routeLabelsGraphicsLayerID
                };
                acLayers.ChildLayers.Add(_routeLabelsGraphicsLayer);
            }
            else
                _routeLabelsGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_routeLabelsGraphicsLayerID];


            // create and add incidentGraphicsLayer to the map
            if (acLayers.ChildLayers[_incidentsGraphicsLayerID] == null)
            {
                _incidentsGraphicsLayer = new client.GraphicsLayer()
                {
                    ID = _incidentsGraphicsLayerID
                };
                acLayers.ChildLayers.Add(_incidentsGraphicsLayer);
            }
            else
                _incidentsGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_incidentsGraphicsLayerID];

            //set up facilities graphicsLayer 
            if (acLayers.ChildLayers[_facilitiesGraphicsLayerID] == null)
            {
                _facilitiesGraphicsLayer = new client.GraphicsLayer()
                {
                    ID = _facilitiesGraphicsLayerID
                };
                acLayers.ChildLayers.Add(_facilitiesGraphicsLayer);
            }
            else
                _facilitiesGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_facilitiesGraphicsLayerID];

            //set up point barrier geometries 
            if (acLayers.ChildLayers[_pointBarriersGraphicsLayerID] == null)
            {
                _pointBarriersGraphicsLayer = new client.GraphicsLayer()
                {
                    ID = _pointBarriersGraphicsLayerID
                };
                acLayers.ChildLayers.Add(_pointBarriersGraphicsLayer);
            }
            else
                _pointBarriersGraphicsLayer = (GraphicsLayer)acLayers.ChildLayers[_pointBarriersGraphicsLayerID];

        }

        /// <summary>
        /// OnActivated is called when the toolbar is installed into the map widget.
        /// </summary>
        public void OnActivated()
        {
            if (ResourceLayers == null || ResourceLayers.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("You have to configure this tool first!");
                clearTheMap();

                // When the user is finished with the toolbar, revert to the configured toolbar.
                if (_mapWidget != null)
                    _mapWidget.SetToolbar(null);
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
                _mapWidget.Map.MouseClick -= Map_MouseClick;
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            clearTheMap(); 

            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
                _mapWidget.SetToolbar(null);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            clearTheMap();
        }

        // ***********************************************************************************
        // * Clear all close facility resources on the map. 
        // ***********************************************************************************
        public void clearTheMap() 
        {
            if (_incidentsGraphicsLayer != null)
                _incidentsGraphicsLayer.Graphics.Clear();
            if (_facilitiesGraphicsLayer != null)
                _facilitiesGraphicsLayer.Graphics.Clear();
            if (_polygonBarriersGraphicsLayer != null)
                _polygonBarriersGraphicsLayer.Graphics.Clear();
            if (_pointBarriersGraphicsLayer != null)
                _pointBarriersGraphicsLayer.Graphics.Clear();
            if (_routesGraphicsLayer != null)
                _routesGraphicsLayer.Graphics.Clear();
            if (_routeLabelsGraphicsLayer != null)
                _routeLabelsGraphicsLayer.Graphics.Clear();
            if (_polylineBarriersGraphicLayer != null)
                _polylineBarriersGraphicLayer.Graphics.Clear();
            if (HiglightRouteLayer != null)
                HiglightRouteLayer.Graphics.Clear();

            cmbBarriers.SelectedIndex = 0;
            chkBarrierType.IsChecked = false;
            cmbFacility.SelectedIndex = 0;

            if (_mapWidget != null)
                _mapWidget.Map.MouseClick -= Map_MouseClick;
        }


        // ***********************************************************************************
        // * Add an incident location on the map... closest facility will be found for this location
        // ***********************************************************************************
        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {

            //if (_incidentsGraphicsLayer.Graphics.Count > 0)
                //_incidentsGraphicsLayer.Graphics.Clear();

            client.Geometry.MapPoint clickPoint = e.MapPoint;
            if (clickPoint != null)
            {
                client.Graphic tempGraphic = new client.Graphic()
                {
                    Geometry = clickPoint,
                    Symbol = new client.Symbols.SimpleMarkerSymbol()
                    {
                        Color = System.Windows.Media.Brushes.Red,
                        Size = 12,
                        Style = client.Symbols.SimpleMarkerSymbol.SimpleMarkerStyle.Circle
                    }
                };
                _incidentsGraphicsLayer.Graphics.Add(tempGraphic);
            }
        }


        // ***********************************************************************************
        // * Add an incident point on the map... closest facility will be found for this location
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
            //if(cmbLayers.SelectedIndex > 0)
            //{
                ResourceLayer layer = (ResourceLayer) cmbLayers.SelectedItem; 
                queryResourceLayer(layer.DataSource); 
            //}
        }

        // ***********************************************************************************
        // * Query the selected resource layer to get the different resource types... 
        // ***********************************************************************************
        //private void queryResourceLayer(string resourceLayerName)
        private async void queryResourceLayer(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource)
        {
            var query = new ESRI.ArcGIS.OperationsDashboard.Query();
            query.WhereClause = "1=1";
            query.ReturnGeometry = false;
            query.Fields = new string[] { _resourceTypeField };

            var result = await dataSource.ExecuteQueryAsync(query);
            if (result == null || result.Features == null)
                return;
            else
                queryResourceLayer_ExecuteCompleted(result);
        }


        // ***********************************************************************************
        // * Query for the facilities is completed... populate facility type combobox
        // ***********************************************************************************
        void queryResourceLayer_ExecuteCompleted(QueryResult result)
        {
            ResourceTypes.Clear();

            //set up facilities type dropdown 
            ResourceType resourceType = new ResourceType();
            resourceType.Name = "Select Type";
            resourceType.url = "";
            ResourceTypes.Add(resourceType);

            if (result != null && result.Features.Count > 0)
            {
                foreach (Graphic graphic in result.Features)
                {
                    if (graphic.Attributes[_resourceTypeField] != null)
                    {
                        //string type = graphic.Attributes["MRPTYPE"].ToString();
                        string type = graphic.Attributes[_resourceTypeField].ToString();
                        resourceType = new ResourceType();
                        resourceType.Name = type;
                        resourceType.url = "";

                        var resourceItem = ResourceTypes.FirstOrDefault(item => item.Name == resourceType.Name);
                        if (resourceItem == null) // none is found.
                            ResourceTypes.Add(resourceType);
                    }
                }
                //cmbFacility.ItemsSource = _resourceTypes;
            }
            else
                System.Windows.MessageBox.Show("No features returned from query");
        }


        // ***********************************************************************************
        // * Queries are completed... but failed 
        // ***********************************************************************************
        private void queryTasks_Failed(object sender, TaskFailedEventArgs args)
        {
            System.Windows.MessageBox.Show("Query execute error: " + args.Error);
            _mapWidget.SetToolbar(null);
        }


        // ***********************************************************************************
        // * User selected a facility type... query for that facility type 
        // ***********************************************************************************
        private async void cmbFacility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
                ResourceType resourceType = (ResourceType)cmbFacility.SelectedItem;

                if (resourceType.Name == "Select Type")
                    return;

                _facilityType = resourceType.Name;
                FacilityType = _facilityType;

                var query = new ESRI.ArcGIS.OperationsDashboard.Query();
                query.WhereClause = _resourceTypeField + "='" + _facilityType + "'";
                query.ReturnGeometry = true;
                query.SpatialFilter = _mapWidget.Map.Extent;
                query.Fields = new string[] {"*"};

                ResourceLayer layer = (ResourceLayer)cmbLayers.SelectedItem;

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
            _facilitiesGraphicsLayer.Graphics.Clear();
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
                    _facilitiesGraphicsLayer.Graphics.Add(graphic);
                }
            }
            else
                System.Windows.MessageBox.Show("No features returned from query");
        }

        
        // ***********************************************************************************
        // * Solve button is clicked. Send the closest facility request to NA Service 
        // ***********************************************************************************
        private void btnSolve_Click(object sender, RoutedEventArgs e)
        {
            List<AttributeParameter> aps = new List<AttributeParameter>();

            if (_incidentsGraphicsLayer.Graphics.Count == 0 || _facilitiesGraphicsLayer.Graphics.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add incident points or select facility type!", "ERROR");
                return;
            }
            RouteClosestFacilityParameters routeParams = new RouteClosestFacilityParameters()
            {
                Incidents = _incidentsGraphicsLayer.Graphics,
                Barriers = _pointBarriersGraphicsLayer.Graphics.Count > 0 ? _pointBarriersGraphicsLayer.Graphics : null,
                PolylineBarriers = _polylineBarriersGraphicLayer.Graphics.Count > 0 ? _polylineBarriersGraphicLayer.Graphics : null, //_polylineBarriers : null,
                PolygonBarriers = _polygonBarriersGraphicsLayer.Graphics.Count > 0 ? _polygonBarriersGraphicsLayer.Graphics : null,

                Facilities = _facilitiesGraphicsLayer.Graphics,  //MUST GET THIS FROM THE DATASOURCE ... RESOURCES LAYER???              
                ReturnDirections = true, //ReturnDirections2.IsChecked.HasValue ? ReturnDirections2.IsChecked.Value : false,
                DirectionsLengthUnits = esriUnits.esriMiles, //GetDirectionsLengthUnits(DirectionsLengthUnits2.SelectionBoxItem.ToString().Trim()),
                DirectionsTimeAttribute = "Time",

                ReturnRoutes = true,
                ReturnFacilities = true,
                ReturnIncidents = true,
                ReturnBarriers = true, 
                ReturnPolylineBarriers = true, //ReturnPolylineBarriers2.IsChecked.HasValue ? ReturnPolylineBarriers2.IsChecked.Value : false,
                ReturnPolygonBarriers = true, //ReturnPolygonBarriers2.IsChecked.HasValue ? ReturnPolygonBarriers2.IsChecked.Value : false,

                FacilityReturnType = FacilityReturnType.ServerFacilityReturnAll,
                OutputLines = "esriNAOutputLineTrueShape", //GetOutputLines(OutputLines2.SelectionBoxItem.ToString().Trim()),
                DefaultTargetFacilityCount = (int)numFacilities.Value,
                TravelDirection = FacilityTravelDirection.TravelDirectionToFacility,
                DefaultCutoff = 1000,
                //AttributeParameterValues = aps,
                OutSpatialReference = _mapWidget.Map.SpatialReference, //string.IsNullOrEmpty(OutputSpatialReference2.Text) ? null : new SpatialReference(int.Parse(OutputSpatialReference2.Text)),
                //AccumulateAttributes = AccumulateAttributeNames2.Text.Split(','),
                //ImpedanceAttribute = ImpedanceAttributeName2.Text,
                //RestrictionAttributes = RestrictionAttributeNames2.Text.Split(','),

                //RestrictUTurns = GetRestrictUTurns(RestrictUTurns2.SelectionBoxItem.ToString().Trim()),
                UseHierarchy = false,
                //OutputGeometryPrecision = string.IsNullOrEmpty(OutputGeometryPrecision2.Text) ? 0 : double.Parse(OutputGeometryPrecision2.Text),
                //OutputGeometryPrecisionUnits = GetGeometryPrecisionUnits(OutputGeometryPrecisionUnits2.SelectionBoxItem.ToString().Trim()),
            };

            if (_mapWidget != null)
                _mapWidget.Map.MouseClick -= Map_MouseClick;

            if (_routeTask.IsBusy)
                _routeTask.CancelAsync();

            _routeTask.SolveClosestFacilityAsync(routeParams);
        }


        // ***********************************************************************************
        // * Add a from location point on the map... closest facility will be found for this location
        // ***********************************************************************************
        void SolveClosestFacility_Completed(object sender, RouteEventArgs e)
        {
            _routesGraphicsLayer.Graphics.Clear();
            _routeLabelsGraphicsLayer.Graphics.Clear();
            if (e.RouteResults != null)
            {
                int i = 0;
                Random randomGen = new Random();
                foreach (RouteResult route in e.RouteResults)
                {
                    Graphic routeGraphic = route.Route;

                    Color color = createRandomColor(randomGen);
                    randomGen.Next(255);
                   
                    routeGraphic.Symbol = new SimpleLineSymbol() { Width = 5, Color = new SolidColorBrush(color) };
                    _routesGraphicsLayer.Graphics.Add(routeGraphic);

                    //Route rank identification symbols...
                    client.Geometry.Polyline pl = (client.Geometry.Polyline)routeGraphic.Geometry;
                    int index = pl.Paths[0].Count / 4;
             
                    Graphic squareGraphic = new Graphic();
                    client.Geometry.PointCollection ptColl = pl.Paths[pl.Paths.Count/2];
                    squareGraphic.Geometry = ptColl[ptColl.Count / 2];

                    //this is the white outline... 
                    SimpleMarkerSymbol sms = new SimpleMarkerSymbol()
                    {
                        Style = SimpleMarkerSymbol.SimpleMarkerStyle.Square,
                        Size = 24,
                        Color = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    };
                    squareGraphic.Symbol = sms;
                    _routeLabelsGraphicsLayer.Graphics.Add(squareGraphic);

                    //purple rectangle behind the rank number
                    Graphic squareGraphic2 = new Graphic();
                    SimpleMarkerSymbol sms2 = new SimpleMarkerSymbol()
                    {
                        Style = SimpleMarkerSymbol.SimpleMarkerStyle.Square,
                        Size = 20,
                        Color = new SolidColorBrush(Color.FromRgb(0,0,139))
                    };
                    squareGraphic2.Symbol = sms2;
                    squareGraphic2.Geometry = ptColl[ptColl.Count / 2];
                    _routeLabelsGraphicsLayer.Graphics.Add(squareGraphic2);

                    //rank number text symbol
                    Graphic routeRankGraphic = new Graphic();
                    routeRankGraphic.Geometry = ptColl[ptColl.Count / 2];

                    TextSymbol routeRankSymbol = new TextSymbol(); 
                    routeRankSymbol.FontFamily = new FontFamily("Arial Black");
                    routeRankSymbol.OffsetX = -4;
                    routeRankSymbol.OffsetY = 10;

                    routeRankSymbol.Text = e.RouteResults[i].Directions.RouteID.ToString();
                    routeRankSymbol.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));

                    routeRankSymbol.FontSize = 16;
                    routeRankGraphic.Symbol = routeRankSymbol;

                    _routeLabelsGraphicsLayer.Graphics.Add(routeRankGraphic);

                    i++;
                }

                //zoom to the map 
                if (chkZoomToMap.IsChecked ?? false)
                    _mapWidget.Map.Extent = _routesGraphicsLayer.FullExtent; 

                //Create and Display Closest Facilities List window...
                _result = new FindCloseFacilityResultView(this, e.RouteResults, _mapWidget);
                _mapWidget.SetToolbar(_result); 
            }
        }

        void SolveClosestFacility_Failed(object sender, TaskFailedEventArgs e)
        {
            System.Windows.MessageBox.Show("Network Analysis error: " + e.Error.Message);
        }

        // ***********************************************************************************
        // * Create random color for the routeLayer graphics 
        // ***********************************************************************************
        Color createRandomColor(Random randomGen)
        {
            System.Drawing.KnownColor[] names = (System.Drawing.KnownColor[])Enum.GetValues(typeof(System.Drawing.KnownColor));
            System.Drawing.KnownColor randomColorName = names[randomGen.Next(names.Length)];
            System.Drawing.Color randomColor = System.Drawing.Color.FromKnownColor(randomColorName);

            Color color = Color.FromArgb(255, randomColor.R, randomColor.G, randomColor.B);
            return color;
        }

       
        // ***********************************************************************************
        // * Add barrier lines on the map. This will be taken into a consideration 
        // * when the closest facilities are being determined
        // ***********************************************************************************
        private void btnAddBarrier_Click(object sender, RoutedEventArgs e)
        {
            _mapWidget.Map.MouseClick -= Map_MouseClick;

            if (_polylineBarriersGraphicLayer.Graphics.Count > 0)
                _polylineBarriersGraphicLayer.Graphics.Clear();

            // Set up the DrawObject with a draw mode and symbol ready for use when a user chooses it.
            if ((_mapWidget != null) && (_mapWidget.Map != null))
            {
                _drawObject = new client.Draw(_mapWidget.Map)
                {
                    DrawMode = client.DrawMode.Polyline,
                    LineSymbol = _polylineBarrierSymbol //(LineSymbol)this.FindResource("routeSymbol")
                };
            }

            _drawObject.IsEnabled = true;
            _drawObject.DrawComplete += DrawComplete;
        }


        // ***********************************************************************************
        // * User finished drawing a polyline on the map. Add the polyline 
        // * barriers GraphicsLayer. 
        // ***********************************************************************************
        void DrawComplete(object sender, client.DrawEventArgs e)
        {
            // Deactivate the draw object for now.
            if (_drawObject != null)
            {
                _drawObject.IsEnabled = false;
                _drawObject.DrawComplete -= DrawComplete;
            }

            client.Geometry.Polyline barrierGeometry = e.Geometry as client.Geometry.Polyline;
            client.Graphic barrierGraphic = new client.Graphic()
            {
                Symbol = _polylineBarrierSymbol, //(LineSymbol)this.FindResource("routeSymbol")
                Geometry = barrierGeometry
            };

            _polylineBarriersGraphicLayer.Graphics.Add(barrierGraphic);
        }


        // ***********************************************************************************
        // * User selected a barrier layer... Get the barrier layer name and query 
        // * for that layer
        // ***********************************************************************************
        private async void cmbBarriers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResourceLayer barriertType = (ResourceLayer)cmbBarriers.SelectedItem;
            if (barriertType.Name == "Select Barrier")
                return;


            var query = new ESRI.ArcGIS.OperationsDashboard.Query();
            query.ReturnGeometry = true;
            query.SpatialFilter = _mapWidget.Map.Extent;
            query.WhereClause = "1=1";
            query.Fields = new string[] { "OBJECTID" };

            var result = await barriertType.DataSource.ExecuteQueryAsync(query);
            if (result == null || result.Features == null)
                return;
            else
                queryBarrierType_ExecuteCompleted(result);
        }


        // ***********************************************************************************
        // * Query for the facilities is completed... populate facility type combobox
        // ***********************************************************************************
        //void queryBarrierType_ExecuteCompleted(object sender, ESRI.ArcGIS.Client.Tasks.QueryEventArgs args)
        void queryBarrierType_ExecuteCompleted(QueryResult result)
        {
            _pointBarriersGraphicsLayer.Graphics.Clear();

            if (result.Features != null && result.Features.Count > 0)
            {
                foreach (Graphic graphic in result.Features)
                {
                    if (graphic.Geometry.GetType() == typeof(client.Geometry.MapPoint))
                    {
                        _pointBarriersGraphicsLayer.Graphics.Add(graphic);
                        graphic.Symbol = _incidentMarkerSymbol; //(PictureMarkerSymbol)this.FindResource("roadBlockSymbol"); ;
                    }                    
                }
            }
            else
                System.Windows.MessageBox.Show("No features returned from query");
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


        private double _height = 0;
        private double _width = 0;
        private void btnMinMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.Height > 32)
            {
                _height = this.Height;
                _width = this.Width; 
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

    public class ResourceType
    {
        public string Name { get; set; }
        public string url { get; set; }
    }

    public class ResourceLayer
    {
        public string Name { get; set; }
        public ESRI.ArcGIS.OperationsDashboard.DataSource DataSource { get; set; }
    }
}
