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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Tasks;
using ESRI.ArcGIS.OperationsDashboard;



namespace ERGChemicalAddIn
{
    /// <summary>
    /// A MapToolbar is a customization for Operations Dashboard for ArcGIS which can be set to replace the configured toolbar 
    /// of a map widget. Typically, the toolbar is created and set into the map widget by a custom map tool. The toolbar must
    /// provide a way to revert to the configured toolbar by passing null into the MapWidget.SetToolbar method.
    /// </summary>
    public partial class ERGChemicalMapToolbar : UserControl, IMapToolbar, INotifyPropertyChanged
    {
        private MapWidget _mapWidget = null;
        ResourceDictionary _mydictionary;
        private MessageBoxResult _windDirectionQuestionResult = MessageBoxResult.No;

        //graphicsLayers 
        private client.GraphicsLayer _spillLocationGraphicsLayer;
        private client.GraphicsLayer _ergZoneGraphicsLayer;
        private client.GraphicsLayer _facilitiesGraphicsLayer;

        //for queries 
        private string _chemicalURL;
        private string _placardURL;
       
        //wind direction query 
        private string _findNearestWSURL;
        private string _windDirectionFieldName;
        private string _windWeatherStationFieldName;
        private string _windRecordedDateFieldName; 

        private string _weatherStationDistanceInfo;

        private int _windDirectionTo = 45;
        public int WindDirectionTo
        {
            get { return _windDirectionTo; }
            set
            {
                _windDirectionTo = value;
                RaisePropertyChanged("WindDirectionTo");
            }
        }

        //Configurable variables... 
        //chemicals list for the dropdown 
        public ObservableCollection<string> ChemicalList { get; private set; }
        private List<GPParameterInfo> chemicalGPParameters; 

        //placards list for the dropdown 
        public ObservableCollection<string> PlacardList { get; private set; }
        private List<GPParameterInfo> placardGPParameters;

        private client.FeatureLayer _windDirectionLayer;

        private string _gpJobId = "";
        private string _gpExecutionType = "";

        private string _defaultChemicalName;
        private string _defaultPlacardName;
        private string _defaultERGName; 

        public ERGChemicalMapToolbar(MapWidget mapWidget, string ERGChemicalURL, string ERGPlacardURL,
           string FindNearestWSURL, string WindDirectionLayerDataSource, string windDirectionFieldName,
            string windWeatherStationFieldName, string windRecordedDateFieldName, string defaultChemicalName, string defaultPlacardName)
        {
            try
            {
                InitializeComponent();
                this.DataContext = this;

                // Store a reference to the MapWidget that the toolbar has been installed to.
                _mapWidget = mapWidget;

                if (string.IsNullOrEmpty(ERGChemicalURL))
                {
                    MessageBox.Show("Please configure the tool!", "Error");
                    return;
                }

                //set up erg Chemical GP URL
                ERGChemicalURL = (ERGChemicalURL ?? "");
                _chemicalURL = ERGChemicalURL;
                chemicalGPParameters = new List<GPParameterInfo>();
                var chemicalJSONUrl = _chemicalURL + "?f=pjson";
                ChemicalList = getListOfChemicalsOrPlacards("chemical", chemicalJSONUrl);
                cmbChemicalOrPlacardType.ItemsSource = ChemicalList;

                if (!String.IsNullOrEmpty(defaultChemicalName))
                    _defaultChemicalName = defaultChemicalName;
                else
                    _defaultChemicalName = _defaultERGName;

                cmbChemicalOrPlacardType.SelectedValue = _defaultChemicalName;

                //set up erg Placard GP URL
                ERGPlacardURL = (ERGPlacardURL ?? "");
                _placardURL = ERGPlacardURL;
                placardGPParameters = new List<GPParameterInfo>();
                var placardJsonUrl = _placardURL + "?f=pjson";
                PlacardList = getListOfChemicalsOrPlacards("placard", placardJsonUrl);

                if (!String.IsNullOrEmpty(defaultPlacardName))
                    _defaultPlacardName = defaultPlacardName;
                else
                    _defaultPlacardName = _defaultERGName; 

                //wind direction
                _findNearestWSURL = FindNearestWSURL;

                if (!string.IsNullOrEmpty(WindDirectionLayerDataSource) && !string.IsNullOrEmpty(_findNearestWSURL))
                {
                    ESRI.ArcGIS.OperationsDashboard.DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Name == WindDirectionLayerDataSource);
                    if (dataSource != null)
                        _windDirectionLayer = _mapWidget.FindFeatureLayer(dataSource);

                    //wind direction fields 
                    if (!string.IsNullOrEmpty(windDirectionFieldName))
                        _windDirectionFieldName = windDirectionFieldName;

                    if (!string.IsNullOrEmpty(windWeatherStationFieldName))
                        _windWeatherStationFieldName = windWeatherStationFieldName;

                    if (!string.IsNullOrEmpty(windRecordedDateFieldName))
                        _windRecordedDateFieldName = windRecordedDateFieldName;

                    btnLookupWindDirection.IsEnabled = true;
                }

                _mydictionary = new ResourceDictionary();
                _mydictionary.Source = new Uri("/ERGChemicalAddin;component/SymbolDictionary.xaml", UriKind.RelativeOrAbsolute);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is an error! Check your parameters!", "Error");
                return; 
            }
        }


        #region mapTool
        public void OnActivated()
        {
            setupGraphicsLayers();
            ReadOnlyObservableCollection<IWidget> widgets = OperationsDashboard.Instance.Widgets; 
        }

        // ***********************************************************************************
        // * ... Get list of chemicals and placards from the gp service 
        // ***********************************************************************************
        private ObservableCollection<string> getListOfChemicalsOrPlacards(string serviceType, string serviceURL)
        {
            ObservableCollection<string> list = new ObservableCollection<string>();
            try
            {
                string jsonString = null;

                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF8;
                    jsonString = webClient.DownloadString(serviceURL);
                }

                if (jsonString != null)
                {
                    JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

                    dynamic gpServiceInfo = jsSerializer.Deserialize<dynamic>(jsonString);
                    dynamic gpParameters = gpServiceInfo["parameters"];

                    dynamic gpExecutionType = gpServiceInfo["executionType"];
                    _gpExecutionType = gpExecutionType as string; 
        

                    for (int i = 0; i < gpParameters.Length; i++)
                    {
                        GPParameterInfo gpInfo = new GPParameterInfo();
                        var parameter = gpParameters[i];
                        gpInfo.Name = parameter["name"];
                        gpInfo.ParameterType = parameter["dataType"];

                        if (serviceType == "chemical")
                            chemicalGPParameters.Add(gpInfo);
                        else if (serviceType == "placard")
                            placardGPParameters.Add(gpInfo);

                        if (parameter["name"] == "material_type" || parameter["name"] == "placard_id")
                        {
                            var choiceList = parameter["choiceList"];
                            _defaultERGName = parameter["defaultValue"];
                            for (int j = 0; j < choiceList.Length; j++) //loop through parameters
                            {
                                list.Add(choiceList[j]);
                            }
                        }
                    }
                }
                else
                    list.Add("Service did not return a list");

                return list;
            }
            catch (Exception ex)
            {
                list.Add("Service didn't return list. Check configuration");
                return list; 
            }
        }


        // ***********************************************************************************
        // * add graphicslayers to the AcceleratedDisplayLayers group layer
        // ***********************************************************************************
        private void setupGraphicsLayers()
        {
            try
            {
                client.AcceleratedDisplayLayers aclyrs = _mapWidget.Map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;

                _ergZoneGraphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer() { ID = "ergZoneGraphicsLayer" };
                if (aclyrs.Count() > 0)
                    aclyrs.ChildLayers.Add(_ergZoneGraphicsLayer);

                _spillLocationGraphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer() { ID = "spillLocationGraphicsLayer" };
                if (aclyrs.Count() > 0)
                    aclyrs.ChildLayers.Add(_spillLocationGraphicsLayer);

                _facilitiesGraphicsLayer = new ESRI.ArcGIS.Client.GraphicsLayer() { ID = "facilitiesGraphicsLayer" };
                if (aclyrs.Count() > 0)
                    aclyrs.ChildLayers.Add(_facilitiesGraphicsLayer);
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public void OnDeactivated()
        {
            // Add any code that cleans up actions taken when activating the toolbar. 
            // For example, ensure any mouse handlers are removed.
            if (_mapWidget != null)
                _mapWidget.Map.MouseClick -= Map_MouseClick;
        }

        // ***********************************************************************************
        // * ...Let user enter the incident location on the map
        // ***********************************************************************************
        void Map_MouseClick(object sender, client.Map.MouseEventArgs e)
        {
            try
            {
                _spillLocationGraphicsLayer.ClearGraphics();
                client.Graphic graphic = new ESRI.ArcGIS.Client.Graphic();
                graphic.Geometry = e.MapPoint;

                graphic.Symbol = _mydictionary["spillSymbol"] as client.Symbols.MarkerSymbol;               
                graphic.SetZIndex(1);
                _spillLocationGraphicsLayer.Graphics.Add(graphic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in mouseclick");
            }
            _mapWidget.Map.MouseClick -= Map_MouseClick;
        }

        #endregion


        #region buttons

        // ***********************************************************************************
        // * User chooses chemical or placard... Populate Chemical or Placard Type combobox with 
        // * list of chemicals or placards
        // ***********************************************************************************
        private void cmbChemicalorPlacard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ChemicalList != null || PlacardList != null)
                {
                    if (cmbChemicalorPlacard.SelectedValue.ToString() == "Chemical")
                    {
                        cmbChemicalOrPlacardType.ItemsSource = ChemicalList;
                        cmbChemicalOrPlacardType.SelectedValue = _defaultChemicalName;
                    }
                    else
                    {
                        cmbChemicalOrPlacardType.ItemsSource = PlacardList;
                        cmbChemicalOrPlacardType.SelectedValue = _defaultPlacardName;
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        // ***********************************************************************************
        // * ...User is done using the map tool. close the dialog. 
        // ***********************************************************************************
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            clearMapToolParameters();
            // When the user is finished with the toolbar, revert to the configured toolbar.
            if (_mapWidget != null)
                _mapWidget.SetToolbar(null);
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
        // * ...Clear the graphicslayers and charts
        // ***********************************************************************************
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            clearMapToolParameters();
        }


        // ***********************************************************************************
        // * ...Clear the graphicslayers and charts
        // ***********************************************************************************
        private void clearMapToolParameters()
        {
            //clear the graphicsLayer
            _spillLocationGraphicsLayer.Graphics.Clear();
            _ergZoneGraphicsLayer.Graphics.Clear();
            _facilitiesGraphicsLayer.Graphics.Clear();

             //Select features using polygon
            IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;

            foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
            {
                client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);
                featureL.ClearSelection();
            }
        }


        // ***********************************************************************************
        // * ...Link to the erg guide book
        // ***********************************************************************************
        private void ergButton_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://wwwapps.tc.gc.ca/saf-sec-sur/3/erg-gmu/erg/guidepage.aspx?guide=");
        }


        // ***********************************************************************************
        // * ...Execute Placard of ERG Chemical GP Tool 
        // ***********************************************************************************
        private void btnSolve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_spillLocationGraphicsLayer.Graphics.Count > 0)
                {
                    Geoprocessor geoprocessorTask = new Geoprocessor();
                    if (cmbChemicalOrPlacardType.SelectedValue == null)
                    {
                        MessageBox.Show("Check your material or placard type!", "Error");
                        return;
                    }

                    var materialType = cmbChemicalOrPlacardType.SelectedValue.ToString();
                    var spillTime = cmbSpillTime.SelectedValue.ToString();
                    var spillSize = cmbSpillSize.SelectedValue.ToString();
                    var windDir = Convert.ToInt32(windDirection.Value);
                    List<GPParameter> parameters = new List<GPParameter>();

                    parameters.Add(new GPFeatureRecordSetLayer("in_features", new FeatureSet(_spillLocationGraphicsLayer.Graphics)));

                    if (cmbChemicalorPlacard.SelectedValue.ToString() == "Chemical")
                    {
                        geoprocessorTask.Url = _chemicalURL;
                        parameters.Add(new GPString("material_type", materialType));
                        geoprocessorTask.JobCompleted += ergChemicalGeoprocessorTask_JobCompleted;
                    }
                    else
                    {
                        geoprocessorTask.Url = _placardURL;
                        geoprocessorTask.JobCompleted += ergPlacardGeoprocessorTask_JobCompleted;
                        parameters.Add(new GPString("placard_id", materialType));
                    }

                    geoprocessorTask.OutputSpatialReference = _mapWidget.Map.SpatialReference;
                    geoprocessorTask.ProcessSpatialReference = _mapWidget.Map.SpatialReference;

                    if (_windDirectionQuestionResult == MessageBoxResult.No)
                        parameters.Add(new GPLong("wind_bearing", windDir));
                    else
                        parameters.Add(new GPLong("wind_bearing", _windDirectionTo));

                    parameters.Add(new GPString("time_of_day", spillTime));
                    parameters.Add(new GPString("spill_size", spillSize));


                    if (_gpExecutionType == "esriExecutionTypeSynchronous")
                    {
                        geoprocessorTask.ExecuteCompleted += ERGGeoprocessorTask_ExecuteCompleted;
                        geoprocessorTask.Failed += GeoprocessorTask_Failed;
                        geoprocessorTask.ExecuteAsync(parameters);
                    }
                    else
                        geoprocessorTask.SubmitJobAsync(parameters);
                }
                else
                    MessageBox.Show("Please add spill location on the map", "Error");
            }
            catch (Exception ex)
            {
                return;
            }
        }


        // ***********************************************************************************
        // * ...Find the nearest Weather Station 
        // ***********************************************************************************
        private void btnLookupWindDirection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_spillLocationGraphicsLayer.Graphics.Count > 0)
                {
                    List<GPParameter> parameters = new List<GPParameter>();

                    client.Projection.WebMercator wm = new client.Projection.WebMercator();
                    ESRI.ArcGIS.Client.Geometry.Geometry geoPoint = wm.ToGeographic(_spillLocationGraphicsLayer.Graphics[0].Geometry);

                    parameters.Add(new GPFeatureRecordSetLayer("Feature_Set", geoPoint));

                    Geoprocessor findNearestWSGPTask = new Geoprocessor(_findNearestWSURL);
                    findNearestWSGPTask.OutputSpatialReference = _mapWidget.Map.SpatialReference;

                    findNearestWSGPTask.ExecuteCompleted += findNearestWSGPTask_ExecuteCompleted;
                    findNearestWSGPTask.Failed += GeoprocessorTask_Failed;
                    findNearestWSGPTask.ExecuteAsync(parameters);
                }
                else
                {
                    MessageBox.Show("Please add the incident location on the map first!", "Warning");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not get wind direction!", "Error");
                return;
            }
        }
        // End Buttons
        #endregion


        #region ERG Chemical GP Tool Logic

        //Execute task is completed 
        void ERGGeoprocessorTask_ExecuteCompleted(object sender, GPExecuteCompleteEventArgs e)
        {
            foreach (GPParameter gpParameter in e.Results.OutParameters)
            {
                if (gpParameter is GPFeatureRecordSetLayer)
                {
                    if (gpParameter.Name == "output_areas")
                    {
                        _ergZoneGraphicsLayer.Graphics.Clear();
                        ESRI.ArcGIS.Client.Geometry.Polygon sharedPolygon = null;

                        //add the erg zone polygons on the map
                        GPFeatureRecordSetLayer gpLayer = gpParameter as GPFeatureRecordSetLayer;
                        foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                        {
                            string zone = graphic.Attributes["ERGZone"].ToString();
                            switch (zone)
                            {
                                case "Initial Isolation Zone":
                                    graphic.Symbol = _mydictionary["sfsZone2"] as client.Symbols.SimpleFillSymbol;
                                    break;
                                case "Protective Action Zone":
                                    graphic.Symbol = _mydictionary["sfsZone1"] as client.Symbols.SimpleFillSymbol;
                                    break;
                                case "Combined Zone":
                                    graphic.Symbol = _mydictionary["sfsZone3"] as client.Symbols.SimpleFillSymbol;
                                    sharedPolygon = (ESRI.ArcGIS.Client.Geometry.Polygon)graphic.Geometry;
                                    break;
                            }
                            _ergZoneGraphicsLayer.Graphics.Add(graphic);
                        }
                        //zoom to the result
                        if (chkZoomToMap.IsChecked == true)
                            _mapWidget.Map.Extent = sharedPolygon.Extent.Expand(1.2);
                    }
                    else 
                    {
                        //add the erg zone polygons on the map
                        GPFeatureRecordSetLayer gpLayer = gpParameter as GPFeatureRecordSetLayer;
                        foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                        {
                            string lineType = graphic.Attributes["LineType"].ToString();
                            switch (lineType)
                            {
                                case "Arc":
                                    graphic.Symbol = _mydictionary["ArcLineSymbol"] as client.Symbols.SimpleLineSymbol;
                                    break;
                                case "Radial":
                                    graphic.Symbol = _mydictionary["RadialSymbol"] as client.Symbols.SimpleLineSymbol;
                                    break;
                            }
                            _ergZoneGraphicsLayer.Graphics.Add(graphic);
                        }
                    }

                }
            }
        }


        // ***********************************************************************************
        // * ..ERG Placard GP Tool Job Completed  
        // ***********************************************************************************
        private void ergPlacardGeoprocessorTask_JobCompleted(object sender, JobInfoEventArgs e)
        {
            Geoprocessor geoprocessorTask = sender as Geoprocessor;
            geoprocessorTask.GetResultDataCompleted += ergGPTask_GetResultDataCompleted;
            geoprocessorTask.Failed += new EventHandler<TaskFailedEventArgs>(GeoprocessorTask_Failed);

            _gpJobId = e.JobInfo.JobId;
            geoprocessorTask.GetResultDataAsync(e.JobInfo.JobId, "output_areas");
        }


        // ***********************************************************************************
        // * ..ERGChemcial GP Tool Job Completed  
        // ***********************************************************************************
        private void ergChemicalGeoprocessorTask_JobCompleted(object sender, JobInfoEventArgs e)
        {
            Geoprocessor geoprocessorTask = sender as Geoprocessor;
            geoprocessorTask.GetResultDataCompleted += ergGPTask_GetResultDataCompleted;
            geoprocessorTask.Failed += new EventHandler<TaskFailedEventArgs>(GeoprocessorTask_Failed);

            _gpJobId = e.JobInfo.JobId;
            geoprocessorTask.GetResultDataAsync(e.JobInfo.JobId, "output_areas");
        }


        // ***********************************************************************************
        // * ..ERGChemcial GP Tool Job Completed Successfully... Get the Result
        // ***********************************************************************************
        void ergGPTask_GetResultDataCompleted(object sender, GPParameterEventArgs e)
        {
            try
            {
                if (e.Parameter.Name == "output_areas")
                {
                    _ergZoneGraphicsLayer.Graphics.Clear();
                    ESRI.ArcGIS.Client.Geometry.Polygon sharedPolygon = null;

                    //add the erg zone polygons on the map
                    GPFeatureRecordSetLayer gpLayer = e.Parameter as GPFeatureRecordSetLayer;
                    foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                    {
                        string zone = graphic.Attributes["ERGZone"].ToString();
                        switch (zone)
                        {
                            case "Initial Isolation Zone":
                                graphic.Symbol = _mydictionary["sfsZone2"] as client.Symbols.SimpleFillSymbol;
                                break;
                            case "Protective Action Zone":
                                graphic.Symbol = _mydictionary["sfsZone1"] as client.Symbols.SimpleFillSymbol;
                                break;
                            case "Combined Zone":
                                graphic.Symbol = _mydictionary["sfsZone3"] as client.Symbols.SimpleFillSymbol;
                                sharedPolygon = (ESRI.ArcGIS.Client.Geometry.Polygon)graphic.Geometry;
                                break;
                        }
                        _ergZoneGraphicsLayer.Graphics.Add(graphic);
                    }
                    //zoom to the result
                    if (chkZoomToMap.IsChecked == true)
                        _mapWidget.Map.Extent = sharedPolygon.Extent.Expand(1.2);

                    selectFeaturesOnTheMap(sharedPolygon);
                    Geoprocessor geoprocessorTask = sender as Geoprocessor;
                    geoprocessorTask.GetResultDataAsync(_gpJobId, "output_lines");
                }
                else
                {
                    //add the erg zone polygons on the map
                    GPFeatureRecordSetLayer gpLayer = e.Parameter as GPFeatureRecordSetLayer;
                    foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                    {
                        string lineType = graphic.Attributes["LineType"].ToString();
                        switch (lineType)
                        {
                            case "Arc":
                                graphic.Symbol = _mydictionary["ArcLineSymbol"] as client.Symbols.SimpleLineSymbol;
                                break;
                            case "Radial":
                                graphic.Symbol = _mydictionary["RadialSymbol"] as client.Symbols.SimpleLineSymbol;
                                break;
                        }
                        _ergZoneGraphicsLayer.Graphics.Add(graphic);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing ERG Task Results!", "Error");
                return;
            }
        }
        #endregion


        // ***********************************************************************************
        // * ... Select facilities that intersect the affeced area on the map
        // ***********************************************************************************
        private async void selectFeaturesOnTheMap(ESRI.ArcGIS.Client.Geometry.Polygon geometry)
        {
            // Find the Selectable data sources provided by feature layers in the map widget.
            var dataSourcesFromSameWidget = OperationsDashboard.Instance.DataSources.Select((dataSource) =>
            {
                client.FeatureLayer fl = _mapWidget.FindFeatureLayer(dataSource);
                return ((fl != null) && (dataSource.IsSelectable)) ? fl : null;
            });

            IEnumerable<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources = OperationsDashboard.Instance.DataSources;
            foreach (ESRI.ArcGIS.OperationsDashboard.DataSource d in dataSources)
            {

                client.FeatureLayer featureL = _mapWidget.FindFeatureLayer(d);
  
                if (dataSourcesFromSameWidget.Contains(featureL))
                {
                    ESRI.ArcGIS.OperationsDashboard.Query query = new ESRI.ArcGIS.OperationsDashboard.Query();
                    query.SpatialFilter = geometry;
                    query.ReturnGeometry = true;
                    query.Fields = new string[] { d.ObjectIdFieldName };

                    ESRI.ArcGIS.OperationsDashboard.QueryResult result = await d.ExecuteQueryAsync(query);
                    if (result.Features.Count > 0)
                    {
                        // Get the array of IDs from the query results.
                        var resultOids = from feature in result.Features select System.Convert.ToInt32(feature.Attributes[d.ObjectIdFieldName]);

                        // For each result feature, find the corresponding graphic in the map.
                        foreach (client.Graphic feature in featureL.Graphics)
                        {
                            int featureOid;
                            int.TryParse(feature.Attributes[d.ObjectIdFieldName].ToString(), out featureOid);
                            if (resultOids.Contains(featureOid))
                                feature.Select();
                        }
                    }
                    else
                        featureL.ClearSelection();
                }
            }
        }

        
        #region Get Wind Direction Info

        // ***********************************************************************************
        // * ... Nearest weather station located... get the wind info from this station   
        // ***********************************************************************************
        // ***********************************************************************************
        // * ..ERG Placard GP Tool Job Completed  
        // ***********************************************************************************
        private void findNearestWSGPTask_JobCompleted(object sender, JobInfoEventArgs e)
        {
            Geoprocessor geoprocessorTask = sender as Geoprocessor;
            geoprocessorTask.GetResultDataCompleted += findNearestWSGPTask_GetResultDataCompleted;
            geoprocessorTask.Failed += new EventHandler<TaskFailedEventArgs>(GeoprocessorTask_Failed);

            _gpJobId = e.JobInfo.JobId;
            geoprocessorTask.GetResultDataAsync(e.JobInfo.JobId, "SACPoint_shp");
        }

        void findNearestWSGPTask_GetResultDataCompleted(object sender, GPParameterEventArgs e)
        {
            try
            {
                GPFeatureRecordSetLayer gpLayer =e.Parameter as GPFeatureRecordSetLayer; 
                foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                {
                    double distance = Convert.ToDouble(graphic.Attributes["NEAR_DIST"]) * 69.09;
                    _weatherStationDistanceInfo = "Distance to weather station: " + distance.ToString("0.000") + " miles";

                    //get the wind direction from the nearest weather station... 
                    int fid = Convert.ToInt32(graphic.Attributes["NEAR_FID"]);

                    if (_windDirectionLayer != null)
                    {
                        QueryTask windDirectionQueryTask = new QueryTask(_windDirectionLayer.Url);
                        windDirectionQueryTask.ExecuteCompleted += WindDirectionQueryTask_ExecuteCompleted;
                        windDirectionQueryTask.Failed += QueryTask_Failed;

                        ESRI.ArcGIS.Client.Tasks.Query windDirectionQuery = new ESRI.ArcGIS.Client.Tasks.Query();
                        windDirectionQuery.OutFields.AddRange(new string[] { "*" });
                        windDirectionQuery.Where = "OBJECTID =" + fid;

                        windDirectionQueryTask.ExecuteAsync(windDirectionQuery);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing ERG Task Results!", "Error");
                return;
            }
        }

        private void findNearestWSGPTask_ExecuteCompleted(object sender, GPExecuteCompleteEventArgs e)
        {
            try
            {
                foreach (GPParameter gpParameter in e.Results.OutParameters)
                {
                    if (gpParameter is GPFeatureRecordSetLayer)
                    {
                        GPFeatureRecordSetLayer gpLayer = gpParameter as GPFeatureRecordSetLayer;
                        foreach (client.Graphic graphic in gpLayer.FeatureSet.Features)
                        {
                            double distance = Convert.ToDouble(graphic.Attributes["NEAR_DIST"]) * 69.09;
                            _weatherStationDistanceInfo = "Distance to weather station: " + distance.ToString("0.000") + " miles";

                            //get the wind direction from the nearest weather station... 
                            int fid = Convert.ToInt32(graphic.Attributes["NEAR_FID"]);

                            if (_windDirectionLayer != null)
                            {
                                QueryTask windDirectionQueryTask = new QueryTask(_windDirectionLayer.Url);
                                windDirectionQueryTask.ExecuteCompleted += WindDirectionQueryTask_ExecuteCompleted;
                                windDirectionQueryTask.Failed += QueryTask_Failed;

                                ESRI.ArcGIS.Client.Tasks.Query windDirectionQuery = new ESRI.ArcGIS.Client.Tasks.Query();
                                windDirectionQuery.OutFields.AddRange(new string[] { "*" });
                                windDirectionQuery.Where = "OBJECTID =" + fid;

                                windDirectionQueryTask.ExecuteAsync(windDirectionQuery);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error finding the nearest weather station!", "Error");
                return;
            }
        }


        // ***********************************************************************************
        // * ... Got the wind direction info from the weather station layer...   
        // ***********************************************************************************
        private void WindDirectionQueryTask_ExecuteCompleted(object sender, ESRI.ArcGIS.Client.Tasks.QueryEventArgs args)
        {
            try
            {
                FeatureSet featureSet = args.FeatureSet;
                if (featureSet.Features.Count > 0)
                {
                    string weatherStationName = "";
                    DateTime date;
                    string recordedDate ="";
                    int windTo = -999;
                    client.Graphic graphic = featureSet.Features[0];

                    if (!string.IsNullOrEmpty(_windDirectionFieldName))
                    {
                        windTo = Convert.ToInt32(graphic.Attributes[_windDirectionFieldName]) + 180;
                        if (windTo > 360)
                            windTo = windTo - 360;
                    }

                    string windDirection = "Wind direction (blowing to:) " + windTo.ToString();

                    if (!string.IsNullOrEmpty(_windWeatherStationFieldName))
                        weatherStationName = "Station Name: " + Convert.ToString(graphic.Attributes[_windWeatherStationFieldName]);

                    if (!string.IsNullOrEmpty(_windRecordedDateFieldName))
                    {
                        date = Convert.ToDateTime(graphic.Attributes[_windRecordedDateFieldName]);
                        recordedDate = "Recorded on: " + date.ToString("F", CultureInfo.CreateSpecificCulture("en-us"));
                    }

                    string windDirectionInfo = weatherStationName + System.Environment.NewLine
                            + _weatherStationDistanceInfo + System.Environment.NewLine
                            + windDirection + System.Environment.NewLine
                            + recordedDate + System.Environment.NewLine + System.Environment.NewLine
                            + "Do you want to use wind direction from this weather station?";

                    _windDirectionQuestionResult = MessageBox.Show(windDirectionInfo, "Wind Direction Information", MessageBoxButton.YesNo);
                    if (_windDirectionQuestionResult == MessageBoxResult.Yes)
                    {
                        _windDirectionTo = windTo;
                        WindDirectionTo = _windDirectionTo;
                    }
                }
                else
                    MessageBox.Show("Please try again!", "Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error find the wind direction info!", "Error");
                return;
            }
        }

        #endregion

        private void GeoprocessorTask_Failed(object sender, TaskFailedEventArgs e)
        {
            MessageBox.Show("Geoprocessor service failed: " + e.Error);
        }

        // Notify when query fails.
        private void QueryTask_Failed(object sender, TaskFailedEventArgs args)
        {
            MessageBox.Show("Query failed: " + args.Error);
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
    }


    public class GPParameterInfo
    {
        public string Name { get; set; }
        public string ParameterType { get; set; }
    }
}
