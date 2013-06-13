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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Geometry;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace RangeFanAddinUpdate
{
    public enum FeatureActionType
    {
        Follow,
        ShowPopup,
        ZoomTo,
        Highlight,
        RemoveRangeFan
    }
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "Create range fan")]
    [ExportMetadata("Description", "Create Range Fan based on bearing, traversal, and range of unit")]
    [ExportMetadata("ImagePath", "/RangeFanAddinUpdate;component/Images/RangeFan-16x.png")]
    [ExportMetadata("DataSourceRequired", true)]
    [DataContract]
    public partial class RangeFanWidget : UserControl, IWidget, IDataSourceConsumer
    {
        client.Map _map = null;
        client.GraphicsLayer _graphics = null;
        Symbol _sym = new MarkerSymbol();
        DataSource _datasource = null;
        //List<Feature> _rfFeatures = new List<Feature>();
        public ObservableCollection<Feature> _rfFeatures = new ObservableCollection<Feature>();
        /// <summary>
        /// The name of a field within the selected data source. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "field")]
        public string Field { get; set; }

        // Gets/sets the feature actions shown in the FeatureActionContextMenu.
        public IEnumerable<IFeatureAction> FeatureActions { get; set; }

        // Information about feature actions selected.
        [DataMember(Name = "featureActions")]
        public FeatureActionType[] PersistedFeatureActions { get; set; }

        // Gets/sets the state of the UpdateExtentType of the HighlighFeatureAction when configured.
        [DataMember(Name = "highlightUpdateType")]
        public UpdateExtentType HighlightUpdateType { get; set; }
        
        [DataMember(Name = "bearing")]
        public double Bearing { get; set; }

        [DataMember(Name = "traversal")]
        public double Traversal { get; set; }
        [DataMember(Name = "range")]
        public double Range { get; set; }
        
        [DataMember(Name = "mapWidgetId")]
        public string MapWidgetId { get; set; }

        public RangeFanWidget()
        {
            InitializeComponent();
            FeatureListBox.DataContext = this;
        }

        

        #region IWidget Members

        private string _caption = "Default Caption";
        /// <summary>
        /// The text that is displayed in the widget's containing window title bar. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "caption")]
        public string Caption
        {
            get
            {
                return _caption;
            }

            set
            {
                if (value != _caption)
                {
                    _caption = value;
                }
            }
        }

        /// <summary>
        /// The unique identifier of the widget, set by the application when the widget is added to the configuration.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// OnActivated is called when the widget is first added to the configuration, or when loading from a saved configuration, after all 
        /// widgets have been restored. Saved properties can be retrieved, including properties from other widgets.
        /// Note that some widgets may have properties which are set asynchronously and are not yet available.
        /// </summary>
        public void OnActivated()
        {
            GetMapFromWidget();
            InitializeFeatureActions();

        }

        /// <summary>
        ///  OnDeactivated is called before the widget is removed from the configuration.
        /// </summary>
        public void OnDeactivated()
        {
        }

        /// <summary>
        ///  Determines if the Configure method is called after the widget is created, before it is added to the configuration. Provides an opportunity to gather user-defined settings.
        /// </summary>
        /// <value>Return true if the Configure method should be called, otherwise return false.</value>
        public bool CanConfigure
        {
            get { return true; }
        }

        /// <summary>
        ///  Provides functionality for the widget to be configured by the end user through a dialog.
        /// </summary>
        /// <param name="owner">The application window which should be the owner of the dialog.</param>
        /// <param name="dataSources">The complete list of DataSources in the configuration.</param>
        /// <returns>True if the user clicks ok, otherwise false.</returns>
        public bool Configure(Window owner, IList<DataSource> dataSources)
        {
            // Show the configuration dialog.
            Config.RangeFanWidgetDialog dialog = new Config.RangeFanWidgetDialog(dataSources, Caption, DataSourceIds != null ? DataSourceIds[0] : null, Field, FeatureActions, MapWidgetId) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            Caption = dialog.Caption;
            _datasource = dialog.DataSource;
            DataSourceIds = new string[] { dialog.DataSource.Id };
            Field = dialog.Field.Name;
            Traversal = System.Convert.ToDouble(dialog.TraversalTextBox.Text);
            Bearing = System.Convert.ToDouble(dialog.BearingTextBox.Text);
            Range = System.Convert.ToDouble(dialog.RangeTextBox.Text);
            FeatureActions = dialog.SelectedFeatureActions;

            InitializePersistedFeatureActions();
            // The default UI simply shows the values of the configured properties.
            // UpdateControls();
            MapWidgetId = dialog.MapWidgetId;

            // Get the map widget from the ID. 
            // If this fails, this will return false to indicate that configuration was unsuccessful and the widget shoud not be added to the configuration.
            bool findMapWidget = GetMapFromWidget();
            return findMapWidget;

        }
        private bool GetMapFromWidget()
        {
            // Find the map widget based on it's ID. 
            MapWidget mapWidget = OperationsDashboard.Instance.Widgets.FirstOrDefault(widget => widget.Id == this.MapWidgetId) as MapWidget;
            if (mapWidget != null)
            {
                // If the map widget is already initialized, the Map on the widget should be set, so get a reference to the map. If the map widget 
                // is not initialized, map will not be set, so wait for initialization to complete and then get a reference to the map.
                if (mapWidget.IsInitialized)
                {
                    SetMap(mapWidget);
                }
                else
                {
                    mapWidget.Initialized += (s, e) =>
                    {
                        SetMap(mapWidget);
                    };
                }
                return true;
            }
            return false;
        }

        private void SetMap(MapWidget mapWidget)
        {
            if ((mapWidget != null) && (mapWidget.Map != null))
            {
                // From the map widget, get the map. 
                _map = mapWidget.Map;

            }
        }
        private void InitializeFeatureActions()
        {
            // Check the persisted information.
            FeatureActions = null;
            if (PersistedFeatureActions == null)
                return;

            // For each feature action saved, create the appropriate feature action class and set any properties.
            List<IFeatureAction> featureActions = new List<IFeatureAction>();
            foreach (var persistedFeatureAction in PersistedFeatureActions)
            {
                switch (persistedFeatureAction)
                {
                    case FeatureActionType.ZoomTo:
                        featureActions.Add(new ZoomToFeatureAction());
                        break;
                    case FeatureActionType.Follow:
                        featureActions.Add(new FollowFeatureAction());
                        break;
                    case FeatureActionType.ShowPopup:
                        featureActions.Add(new ShowPopupFeatureAction());
                        break;
                    case FeatureActionType.Highlight:
                        featureActions.Add(new HighlightFeatureAction() { UpdateExtent = HighlightUpdateType });
                        break;
                    case FeatureActionType.RemoveRangeFan:
                        featureActions.Add(new RemoveFanFA());
                        break;
                    default:
                        throw new NotImplementedException(string.Format("Cannot create feature action of type: {0}", persistedFeatureAction.ToString()));
                }
            }
            FeatureActions = featureActions;
        }

        // Initializes the PersistedFeatureActions property used to persist the feature actions selected in the config dialog.
        private void InitializePersistedFeatureActions()
        {
            // Clear any current information
            PersistedFeatureActions = null;
            HighlightUpdateType = UpdateExtentType.Pan;

            if (FeatureActions == null)
                return;

            // For each feature action object, create persistence helper and set any properties.
            List<FeatureActionType> persistedFeatureActions = new List<FeatureActionType>();
            foreach (var featureAction in FeatureActions)
            {
                if (featureAction is HighlightFeatureAction)
                {
                    persistedFeatureActions.Add(FeatureActionType.Highlight);

                    // Persist the UpdateExtent state of the highlight feature action
                    HighlightUpdateType = ((HighlightFeatureAction)featureAction).UpdateExtent;
                }
                else if (featureAction is FollowFeatureAction)
                    persistedFeatureActions.Add(FeatureActionType.Follow);
                else if (featureAction is ShowPopupFeatureAction)
                    persistedFeatureActions.Add(FeatureActionType.ShowPopup);
                else if (featureAction is ZoomToFeatureAction)
                    persistedFeatureActions.Add(FeatureActionType.ZoomTo);
                else if (featureAction is HighlightFeatureAction)
                    persistedFeatureActions.Add(FeatureActionType.Highlight);
                else if (featureAction is RemoveFanFA)
                    persistedFeatureActions.Add(FeatureActionType.RemoveRangeFan);
                else
                    throw new NotImplementedException(string.Format("Cannot persist feature action of type: {0}", featureAction.GetType().ToString()));
            }

            PersistedFeatureActions = persistedFeatureActions.ToArray();
        }
   
        #endregion
        public bool addToList(client.Graphic pGraphic)
        {
            try
            {
                if (_map == null)
                    GetMapFromWidget();

                if (_graphics == null)
                {
                    _graphics = new ESRI.ArcGIS.Client.GraphicsLayer();
                    _graphics.ID = "RangeFanGraphics";
                    _map.Layers.Add(_graphics);
                }
                
                string oID = _datasource.ObjectIdFieldName;
                bool contains = false;
                foreach (Feature p in _rfFeatures)
                {
                    if (p.Graphic.Attributes[oID].ToString() == pGraphic.Attributes[oID].ToString())
                        contains = true;
                }
                if (contains == false)
                {
                    Feature pFeature = new Feature(pGraphic, Field);


                    client.Graphic cFan = CreateFan(pGraphic);
                    if (cFan != null)
                        _graphics.Graphics.Add(cFan);

                    _rfFeatures.Add(pFeature);

                    FeatureListBox.ItemsSource = null;
                    FeatureListBox.ItemsSource = _rfFeatures;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void removefromList(client.Graphic pGraphic)
        {
            try
            {
                string oid = pGraphic.Attributes[_datasource.ObjectIdFieldName].ToString();
                System.Diagnostics.Debug.WriteLine("Removing OID: " + oid);
                foreach (Feature f in _rfFeatures)
                {
                    if (f.Graphic.Attributes[_datasource.ObjectIdFieldName].ToString() == oid)
                    {
                        _rfFeatures.Remove(f);
                        break;
                    }
                }
                int idx = 0;
                for (int g = 0; g < _graphics.Graphics.Count; g++)
                {
                    if (_graphics.Graphics[g].Attributes["Name"].ToString() == oid)
                    {
                        idx = g;
                    }
                }
                _graphics.Graphics.RemoveAt(idx);
                FeatureListBox.ItemsSource = _rfFeatures;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            
        }
        private client.Graphic CreateFan(client.Graphic g)
        {
            try
            {
                SimpleFillSymbol _sym = new SimpleFillSymbol()
                {
                    Fill = new SolidColorBrush(Color.FromArgb(100, (byte)255, (byte)190, (byte)232)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, (byte)197, (byte)0, (byte)255)),
                    BorderThickness = 1
                };
                client.Graphic pPolyGraphic = new client.Graphic();
                if (Traversal < 360)
                {
                    double initBearing = Bearing;
                    initBearing = Geo2Arithmetic(Bearing);  //Need to convert from geographic angles (zero north clockwise) to arithmetic (zero east counterclockwise)
                    if (Traversal == 0)
                        Traversal = 1;
                    double leftAngle = initBearing - (Traversal / 2.0);
                    double rightAngle = initBearing + (Traversal / 2.0);

                    double centerpointX = g.Geometry.Extent.GetCenter().X;
                    double centerpointY = g.Geometry.Extent.GetCenter().Y;

                    ObservableCollection<ESRI.ArcGIS.Client.Geometry.PointCollection> pcol = new ObservableCollection<ESRI.ArcGIS.Client.Geometry.PointCollection>();
                    ESRI.ArcGIS.Client.Geometry.PointCollection ptCollection = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                    ptCollection.Add(g.Geometry as MapPoint);

                    for (int i = System.Convert.ToInt16(leftAngle); i < rightAngle; i++)
                    {
                        double x = centerpointX + (Range * Math.Cos(DegreeToRadian(i)));
                        double y = centerpointY + (Range * Math.Sin(DegreeToRadian(i)));
                        ESRI.ArcGIS.Client.Geometry.MapPoint mPt = new MapPoint(x, y);
                        ptCollection.Add(mPt);
                    }
                    ptCollection.Add(g.Geometry as MapPoint);

                    ESRI.ArcGIS.Client.Geometry.Polygon pPoly = new ESRI.ArcGIS.Client.Geometry.Polygon();
                    pcol.Add(ptCollection);
                    pPoly.Rings = pcol;

                    pPolyGraphic.Geometry = pPoly;

                    pPolyGraphic.Symbol = _sym;
                    pPolyGraphic.Attributes.Add("Name", g.Attributes[_datasource.ObjectIdFieldName].ToString());
                    System.Diagnostics.Debug.WriteLine(g.Attributes[_datasource.ObjectIdFieldName].ToString());
                }
                else
                {
                    Circle pCircle = new Circle();
                    ESRI.ArcGIS.Client.Geometry.MapPoint mPt = new MapPoint(g.Geometry.Extent.GetCenter().X, g.Geometry.Extent.GetCenter().Y);
                    pCircle.Center = mPt;
                    pCircle.Radius = Range;
                    pPolyGraphic.Symbol = _sym;
                    pPolyGraphic.Geometry = pCircle;
                    pPolyGraphic.Attributes.Add("Name", g.Attributes[_datasource.ObjectIdFieldName].ToString());
                }
                return pPolyGraphic;

            }
            catch
            {
                return null;
            }
        }
        public class Circle : ESRI.ArcGIS.Client.Geometry.Polygon
        {
            private double radius = double.NaN;
            private ESRI.ArcGIS.Client.Geometry.MapPoint center = null;
            private int pointCount = 360;

            public double Radius
            {
                get { return radius; }
                set { radius = value; CreateRing(); }
            }

            [System.ComponentModel.TypeConverter(typeof(MapPointConverter))]
            public MapPoint Center
            {
                get { return center; }
                set { center = value; CreateRing(); }
            }

            public int PointCount
            {
                get { return pointCount; }
                set { pointCount = value; CreateRing(); }
            }

            private void CreateRing()
            {
                this.Rings.Clear();
                if (!double.IsNaN(Radius) && Radius > 0 && Center != null && PointCount > 2)
                {
                    ESRI.ArcGIS.Client.Geometry.PointCollection pnts = new ESRI.ArcGIS.Client.Geometry.PointCollection();
                    for (int i = 0; i <= PointCount; i++)
                    {
                        double rad = 2 * Math.PI / PointCount * i;
                        double x = Math.Cos(rad) * radius + Center.X;
                        double y = Math.Sin(rad) * radius + Center.Y;
                        pnts.Add(new MapPoint(x, y));
                    }
                    this.Rings.Add(pnts);
                }
            }
        }

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        public double Geo2Arithmetic(double inAngle)
        {
            double outAngle = 0.0;
            if (inAngle > 360.0)
                inAngle = inAngle % 360.0;
            //0 to 90
            if (inAngle == 360.0)
                inAngle = 0.0;
            if (inAngle >= 0.0 && inAngle <= 90.0)
                outAngle = Math.Abs(inAngle - 90.0);
            // 90 to 360
            if (inAngle >= 90.0 && inAngle < 360.0)
                outAngle = 360.0 - (inAngle - 90.0);

            return outAngle;
        }

        #region IDataSourceConsumer Members

        /// <summary>
        /// Returns the ID(s) of the data source(s) consumed by the widget.
        /// </summary>
        [DataMember(Name = "dataSourceIds")]
        public string[] DataSourceIds { get; set; }

        public DataSource DataSource
        {
            get
            {
                return OperationsDashboard.Instance.DataSources.FirstOrDefault((dataSource) => dataSource.Id == DataSourceIds[0]);
            }
        }
        /// <summary>
        /// Called when a DataSource is removed from the configuration. 
        /// </summary>
        /// <param name="dataSource">The DataSource being removed.</param>
        public void OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            DataSourceIds = null;
        }

        /// <summary>
        /// Called when a DataSource found in the DataSourceIds property is updated.
        /// </summary>
        /// <param name="dataSource">The DataSource being updated.</param>
        public async void OnRefresh(DataSource dataSource)
        {
            try
            {
                int[] oIds = new int[_rfFeatures.Count];
                int count = 0;
                foreach (Feature f in _rfFeatures)
                {
                    oIds[count] = System.Convert.ToInt32(f.Graphic.Attributes[dataSource.ObjectIdFieldName].ToString());
                    System.Diagnostics.Debug.WriteLine("Refresh:  " + oIds[count].ToString());
                    count++;
                }

                var result = await dataSource.ExecuteQueryObjectIdsAsync(oIds, new Query());
                if (result == null || result.Features == null)
                    return;


                client.GraphicsLayer gLayer = _map.Layers["RangeFanGraphics"] as client.GraphicsLayer;
                if (gLayer != null)
                {
                    if (gLayer.Graphics.Count == result.Features.Count)
                    {
                        foreach (client.Graphic g in result.Features)
                        {
                            client.Graphic pFan = CreateFan(g);
                            if (pFan != null)
                            {
                                foreach (client.Graphic graphic in gLayer.Graphics)
                                {
                                    if (graphic.Attributes["Name"].ToString() == g.Attributes[_datasource.ObjectIdFieldName].ToString())
                                        graphic.Geometry = pFan.Geometry;
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        #endregion
    }
    

    // Helper class that represents a feature in the collection of features bound to the FeatureListBox.
    public class Feature
    {
        readonly string _field;

        public Feature(client.Graphic feature, string field)
        {
            Graphic = feature;
            _field = field;
        }

        public client.Graphic Graphic { get; private set; }

        public string FieldValue { get { string val = Graphic.Attributes[_field].ToString(); return val; } }


    }
}
