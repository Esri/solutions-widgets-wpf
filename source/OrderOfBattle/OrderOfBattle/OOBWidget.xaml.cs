using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ESRI.ArcGIS.Client.Behaviors;
using ESRI.ArcGIS.Client.AdvancedSymbology;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;
using System.Net;
using System.Web;




namespace OOB
{
    /// <summary>
    /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
    /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
    /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
    /// data source is updated or removed.
    /// </summary>
    public enum FeatureActionType
    {
        Zoom,
        Pan,
        Highlight,
        Follow,
        Popup
    }
    public enum extentType
    {
        min,
        max
    }
    [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
    [ExportMetadata("DisplayName", "Order of Battle")]
    [ExportMetadata("Description", "Display features in schema defined hierarchical structure. Version 10.2")]
    [ExportMetadata("ImagePath", "/OrderOfBattle102;component/Images/infantry.jpg")]
    [ExportMetadata("DataSourceRequired", true)]
    [DataContract]
    [Serializable]

    public partial class OOBWidget : UserControl, IWidget, IDataSourceConsumer, IMapWidgetConsumer
    {
        /// <summary>
        /// A unique identifier of a data source in the configuration. This property is set during widget configuration.
        /// </summary>
        /// // Public members for data binding
        [DataMember(Name = "forceDataSourceId")]
        public string ForceDataSourceId { get; set; }


        [DataMember(Name = "dsstring")]
        public String dsString { get; set; }

        public Dictionary<String, String> DataSources = new Dictionary<string, string>();
        /// <summary>
        /// The name of a field within the selected data source. This property is set during widget configuration.
        /// </summary>
        [DataMember(Name = "initFieldName")]
        public string initFieldName { get; set; }
        private client.Field initField { get; set; }

        public IEnumerable<IFeatureAction> FeatureActions { get; set; }

        [DataMember(Name = "descType")]
        public DescriptionType DescType
        {
            get
            {
                return _dType;
            }
            set
            {
                _dType = value;
            }
        }
        private DescriptionType _dType = DescriptionType.None;

        private String _baseDesc = "";
        [DataMember(Name = "baseDesc")]
        public String BaseDescription
        {
            get
            {
                return _baseDesc;
            }
            set
            {
                _baseDesc = value;
            }
        }
        [DataMember(Name = "featureActions")]
        public FeatureActionType[] PersistedFeatureActions { get; set; }

        [DataMember(Name = "highlightUpdateType")]
        public UpdateExtentType HighlightUpdateType { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }

        [DataMember(Name = "oobname")]
        public string oobname { get; set; }

        [DataMember(Name = "numds")]
        public Int32 numDs { get; set; }

        [DataMember(Name = "mapWidgetId")]
        public string MapWidgetId { get; set; }

        private Boolean _isInitialized = false;

        [DataMember(Name = "oobDsString")]
        public String oobDsString { get; set; }

        [DataMember(Name = "dataSourceIds")]
        public string[] _datasourceids { get; set; }

        [DataMember(Name = "showIcon")]

        private Boolean _showIcon = false;
        public Dictionary<String, String> OOBDsStrings = new Dictionary<String, String>();

        private Dictionary<String, Query> _queries = null;
        private Dictionary<String, DataSource> _dataSources = new Dictionary<string, DataSource>();
        private Dictionary<String, OOBDataSource> _oobDataSources = new Dictionary<String, OOBDataSource>();
        public Dictionary<String, OOBDataSource> OOBDataSources
        {
            get { return _oobDataSources; }
        }
        //drag and drop variables
        private System.Windows.Point _lastMouseDown;
        private TreeViewItem draggedItem;
        private TreeViewItem _target;
        private string QueryValue { get; set; }
        //private ContextMenu cm = null;
        private enum selectionmode { None = 0, Unit = 1, Child = 2, UnitChild = 3, Dep = 4, UnitDep = 5, Leaf = 6, All = 7 };
        private selectionmode mode = selectionmode.None;
        private client.Map _map = null;
        private ContextMenu _cm;

        private MapWidget _mapw;
        private String _currentflname = null;
        public client.FeatureLayer _currentFLyr;
        private OOBCache oobcache;
        private Boolean _cacheDirty = false;
        private Boolean _blocking = false;
        private Boolean _updating = false;
        private Int16 counter = -1;



        public OOBWidget()
        {
            _cm = new ContextMenu();


            InitializeComponent();

            Value = "0";
            Caption = "Order Of Battle";

        }
        private List<DataSource> dsList = new List<DataSource>();
        private client.Geometry.PointCollection selGeo = new client.Geometry.PointCollection();
        private void UpdateControls()
        {
            //DataSourceBox.Text = DataSourceId;
            //UIDFieldBox.Text = ForceUIDFieldName;
            //HFFieldBox.Text = ForceHigherFormationFieldName;
        }
        //private Boolean _isFollowing = false;
        private FollowFeatureAction followAction = null;
        #region IWidget Members

        private string _caption = "Order of Battle";
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
            //GetMapFromWidget();
            DataContext = this;
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
        // Set up feature actions from persisted data.
        private Boolean ContextMenuContains(ContextMenu cm, MenuItem item)
        {
            foreach (MenuItem i in cm.Items)
            {
                if (i.Header.Equals(item.Header))
                {
                    return true;
                }
            }
            return false;
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
                MenuItem item = new MenuItem();
                switch (persistedFeatureAction)
                {
                    case FeatureActionType.Zoom:
                        featureActions.Add(new ZoomToFeatureAction());
                        item.Header = "Zoom To";
                        item.Click += zoom_to_feature;
                        break;
                    case FeatureActionType.Pan:
                        featureActions.Add(new PanToFeatureAction());
                        item.Header = "Pan To";
                        item.Click += pan_to_feature;
                        break;
                    case FeatureActionType.Highlight:
                        featureActions.Add(new HighlightFeatureAction() { UpdateExtent = HighlightUpdateType });
                        item.Header = "Highlight";
                        item.Click += highlight_selected;
                        break;
                    case FeatureActionType.Follow:
                        featureActions.Add(new FollowFeatureAction());
                        item.Header = "Follow";
                        item.Click += follow_selected;
                        break;
                    case FeatureActionType.Popup:
                        featureActions.Add(new ShowPopupFeatureAction());
                        item.Header = "Show Popup";
                        item.Click += show_popup_selected;
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Cannot create feature action of type: {0}", persistedFeatureAction.ToString()));
                }
                if (!ContextMenuContains(_cm, item))
                {
                    _cm.Items.Add(item);
                }
            }
            FeatureActions = featureActions;
        }
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
                MenuItem item = new MenuItem();
                if (featureAction is ZoomToFeatureAction)
                {
                    item.Header = "Zoom To";
                    persistedFeatureActions.Add(FeatureActionType.Zoom);
                    item.Click += zoom_to_feature;
                }
                else if (featureAction is PanToFeatureAction)
                {
                    item.Header = "Pan To";
                    persistedFeatureActions.Add(FeatureActionType.Pan);
                    item.Click += pan_to_feature;
                }
                else if (featureAction is HighlightFeatureAction)
                {
                    item.Header = "Highlight";
                    persistedFeatureActions.Add(FeatureActionType.Highlight);
                    // Persist the UpdateExtent state of the highlight feature action
                    HighlightUpdateType = ((HighlightFeatureAction)featureAction).UpdateExtent;
                    item.Click += highlight_selected;
                }
                else if (featureAction is FollowFeatureAction)
                {
                    persistedFeatureActions.Add(FeatureActionType.Follow);
                    item.Header = "Follow";
                    item.Click += follow_selected;
                }
                else if (featureAction is ShowPopupFeatureAction)
                {
                    persistedFeatureActions.Add(FeatureActionType.Popup);
                    item.Header = "Show Popup";
                    item.Click += show_popup_selected;
                }
                else
                    throw new NotImplementedException(string.Format("Cannot persist feature action of type: {0}", featureAction.GetType().ToString()));
                if (!ContextMenuContains(_cm, item))
                {
                    _cm.Items.Add(item);
                }
            }

            PersistedFeatureActions = persistedFeatureActions.ToArray();
        }

        private async void show_popup_selected(object sender, RoutedEventArgs e)
        {
            if (tv.SelectedItem == null)
            {
                return;
            }
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            DataSource d = n.NodeDataSource.DataSource;
            Dictionary<String, Query> queries = queryUnit(n);
            Query q = queries[n.NType];
            q.ReturnGeometry = true;
            QueryResult result = await d.ExecuteQueryAsync(q);
            if (result.Features.Count < 1)
            {
                return;
            }
            client.Graphic feature = result.Features[0];
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;
            if (feature.Geometry == null)
            {
                return;
            }
            ShowPopupFeatureAction pfa = new ShowPopupFeatureAction();
            pfa.Execute(d, feature);
        }
        private async void zoom_to_feature(object sender, RoutedEventArgs e)
        {
            if (tv.SelectedItem == null)
            {
                return;
            }
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            DataSource d = n.NodeDataSource.DataSource;
            Dictionary<String, Query> queries = queryUnit(n);
            Query q = queries[n.NType];
            q.ReturnGeometry = true;
            QueryResult result = await d.ExecuteQueryAsync(q);
            if (result.Features.Count < 1)
            {
                return;
            }
            client.Graphic feature = result.Features[0];
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;
            if (feature.Geometry == null)
            {
                return;
            }
            ZoomToFeatureAction zfa = new ZoomToFeatureAction();
            zfa.Execute(d, feature);
        }

        private async void pan_to_feature(object sender, RoutedEventArgs e)
        {
            if (tv.SelectedItem == null)
            {
                return;
            }
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            DataSource d = n.NodeDataSource.DataSource;
            Dictionary<String, Query> queries = queryUnit(n);
            Query q = queries[n.NType];
            q.ReturnGeometry = true;
            QueryResult result = await d.ExecuteQueryAsync(q);
            if (result.Features.Count < 1)
            {
                return;
            }
            client.Graphic feature = result.Features[0];
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;
            PanToFeatureAction pfa = new PanToFeatureAction();
            pfa.Execute(d, feature);
        }

        private async void highlight_selected(object sender, RoutedEventArgs e)
        {
            if (tv.SelectedItem == null)
            {
                return;
            }
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            DataSource d = n.NodeDataSource.DataSource;
            Dictionary<String, Query> queries = queryUnit(n);
            Query q = queries[n.NType];
            q.ReturnGeometry = true;
            QueryResult result = await d.ExecuteQueryAsync(q);
            if (result.Features.Count < 1)
            {
                return;
            }
            client.Graphic feature = result.Features[0];
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;
            HighlightFeatureAction hfa = new HighlightFeatureAction();
            hfa.UpdateExtent = HighlightUpdateType;
            hfa.Execute(d, feature);
        }

        private async void follow_selected(object sender, RoutedEventArgs e)
        {
            if (tv.SelectedItem == null)
            {
                return;
            }

            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            DataSource d = n.NodeDataSource.DataSource;
            Dictionary<String, Query> queries = queryUnit(n);
            Query q = queries[n.NType];
            q.ReturnGeometry = true;
            QueryResult result = await d.ExecuteQueryAsync(q);
            if (result.Features.Count < 1)
            {
                return;
            }
            client.Graphic feature = result.Features[0];
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;
            followAction = new FollowFeatureAction();
            if (followAction.CanExecute(d, feature))
                followAction.Execute(d, feature);
        }

        private void openTools(object sender, RoutedEventArgs e)
        {
            popTools.IsOpen = true;
        }

        private void openSelectionConfig(object sender, RoutedEventArgs e)
        {
            popSelect.IsOpen = true;
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



            Config.OOBWidgetDialog dialog = new Config.OOBWidgetDialog(OOBDataSources, Caption, FeatureActions, oobcache) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;
            Caption = dialog.OOBName;
            OOBDsStrings = dialog.OOBDsStrings;
            oobDsString = dialog.oobdsstring;

            //ShowIcon = dialog.ShowIcon;

            _oobDataSources = dialog.OOBDataSources;
            FeatureActions = dialog.SelectedFeatureActions;
            InitializePersistedFeatureActions();
            numDs = OOBDataSources.Count;
            SetDSIds();
            oobcache = dialog.cache;

            _mapw = MapWidget.FindMapWidget(OOBDataSources["UNITS"].DataSource);
            MapWidgetId = _mapw.Id;
            // Retrieve the selected values for the properties from the configuration dialog.
            //Caption = dialog.Caption;
            oobname = _caption;


            if (this.tv.Items.Count > 0)
            {
                this.tv.Items.Clear();
            }


            UpdateControls();
            dsString = "";
            Boolean first = true;
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                if (!first)
                {
                    dsString += ";";
                }
                else
                {
                    first = false;
                }
                dsString += p.Key + ":" + p.Value.ID;
            }

            _isInitialized = false;


            return true;

        }

        #endregion

        #region IDataSourceConsumer Members

        /// <summary>
        /// Returns the ID(s) of the data source(s) consumed by the widget.
        /// </summary>
        //[DataMember(Name = "dataSourceIds")]
        public string[] DataSourceIds
        {
            get { return _datasourceids; }
        }

        /// <summary>
        /// Called when a DataSource is removed from the configuration. 
        /// </summary>
        /// <param name="dataSource">The DataSource being removed.</param>
        public void OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            ForceDataSourceId = null;

        }

        private void InitOOBDataSource(DataSource ds, String odsstring)
        {

            OOBDataSource ods = OOBDataSource.marshalODS(ds, odsstring);
            OOBDataSources.Add(ods.Key, ods);
        }

        private void InitLayer(DataSource ds)
        {
            //DataSources.

            if (!string.IsNullOrEmpty(dsString))
            {
                String[] dspairs = dsString.Split(';');

                foreach (String dspair in dspairs)
                {
                    String[] pair = dspair.Split(':');
                    String key = pair[0];
                    String id = pair[1];
                    if (id.Equals(ds.Id))
                    {
                        if (OOBDsStrings.Count < 1)
                        {
                            parseOobDsStrings();
                            return;
                        }
                        if (!OOBDataSources.ContainsKey(key))
                        {
                            String odsstring = OOBDsStrings[key];
                            InitOOBDataSource(ds, odsstring);
                        }

                        if (!_dataSources.ContainsKey(key))
                        {
                            _dataSources.Add(key, ds);
                            _currentFLyr = _mapw.FindFeatureLayer(ds);
                            _currentFLyr.Refresh();
                        }
                    }

                }
            }
        }

        private void parseOobDsStrings()
        {
            String[] oodsStringArray = oobDsString.Split(';');
            foreach (String s in oodsStringArray)
            {
                String[] i = s.Split('%');
                OOBDsStrings.Add(i[0], i[1]);
            }
        }

        private void AddDatasourceToCache(OOBDataSource ods)
        {
            try
            {
                String Uid = ods.UIDField;
                String HF = ods.HFField;
                String DF = ods.DescField;
                String Labels = "";
                String DescFlds = "";
                String CacheName = ods.Key;
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = Uid;
                fields["HF"] = HF;

                Int32 numBaseFlds = 2;
                if (DF != null)
                {
                    fields["DESCFIELD"] = DF;
                    numBaseFlds = 3;
                }

                Int32 arraySize = ods.LabelFields.Count + ods.DescriptionFields.Count + numBaseFlds;
                string[] qfields = new string[arraySize];
                qfields[0] = Uid;
                qfields[1] = HF;
                if (DF != null)
                {
                    qfields[2] = DF;
                }
                Int32 c = numBaseFlds;
                Boolean first = true;
                foreach (String f in ods.LabelFields)
                {
                    if (!first)
                    {
                        Labels += ",";
                    }
                    else
                    {
                        first = false;
                    }
                    Labels += f;
                    qfields[c] = f;
                    ++c;

                }
                first = true;
                foreach (String f in ods.DescriptionFields)
                {
                    if (!first)
                    {
                        DescFlds += ",";
                    }
                    else
                    {
                        first = false;
                    }
                    DescFlds += f;
                    qfields[c] = f;
                    ++c;
                }
               
                
                fields["LABELS"] = Labels;
                fields["DESCFLDS"] = DescFlds;
                
                oobcache.AddFeatuereContainer(CacheName);

                DataSource ds = ods.DataSource;
               
                MapWidget mw = MapWidget.FindMapWidget(ds);
                client.FeatureLayer fl = mw.FindFeatureLayer(ds);
                client.UniqueValueRenderer r = fl.Renderer as client.UniqueValueRenderer;
                foreach (client.Graphic g in fl.Graphics)
                {
                    oobcache.AddFeature(CacheName, g, ods.BaseDescription, ods.BaseLabel, fields, r);
                }
                ods.IsCacheCreated = true;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }

        private void Update()
        {
            try
            {
                if (oobcache == null)
                {
                    oobcache = new OOBCache();
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        AddDatasourceToCache(p.Value);
                    }
                    return;
                }
                else
                {
                    _isInitialized = AllCachesCreated();
                    if (!_isInitialized)
                    {
                        return;
                    }

                    OOBTree tree = tv.Tag as OOBTree;
                    OOBNode root = tree.Root;
                    
                    if (!_updating)
                    {
                        _updating = true;

                        foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                        {
                            String type = p.Value.Key;
                            root = UpdateOrderOfBattle(type, root);
                        }
                        List<String> keys = new List<String>();
                        foreach (KeyValuePair<String, OOBNode> p in root.Children)
                        {
                            String pName = p.Value.ParentName;
                            OOBNode found = root.GetDescendant(pName);
                            if (found != null)
                            {
                                keys.Add(p.Value.Key);
                                found.addChild(p.Value);
                            }
                        }
                        foreach (String k in keys)
                        {
                            root.removeChild(k);
                        }
                        _updating = false;
                    }

                    UpdateTreeView(root, tv.Items);



                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }
        private void Initialize()
        {
            try
            {
                if (oobcache == null)
                {
                    oobcache = new OOBCache();
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        AddDatasourceToCache(p.Value);
                    }
                    return;
                }
                else
                {
                    _isInitialized = AllCachesCreated();
                    if (!_isInitialized)
                    {
                        return;
                    }

                }
                
                OOBTree tree = new OOBTree();
                this.tv.Tag = tree;
                if (tv.Items.Count > 0)
                {
                    tv.Items.Clear();
                }
                OOBNode root = tree.Root;
               
                if (!_updating)
                {
                    _updating = true;
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        String type = p.Value.Key;
                        tree.AddDataSource(p.Value.ID, p.Value.DataSource, type);
                        root = CreateOrderOfBattle(type, root);

                    }

                    List<String> keys = new List<String>();
                    foreach (KeyValuePair<String, OOBNode> p in root.Children)
                    {
                        String pName = p.Value.ParentName;
                        OOBNode found = root.GetDescendant(pName);
                        if (found != null)
                        {
                            keys.Add(p.Value.Key);
                            found.addChild(p.Value);
                        }
                    }
                    foreach (String k in keys)
                    {
                        root.removeChild(k);
                    }
                    _updating = false;
                }

                UpdateTreeView(root, tv.Items);

                CancelShowButton.IsEnabled = false;
                ShowFeaturesButton.IsEnabled = false;
                SelectFeaturesButton.IsEnabled = false;
                ZoomToSelectedButton.IsEnabled = false;
                PanToSelectedButton.IsEnabled = false;
                ClearSelectionButton.IsEnabled = false;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }

        private void SetDSIds()
        {
            _datasourceids = new string[numDs];
            Int32 c = 0;
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                _datasourceids[c] = p.Value.ID;
                ++c;
            }
        }
        private Boolean AllCachesCreated()
        {
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                if (!p.Value.IsCacheCreated)
                {
                    return false;
                }
            }
            return true;
        }
        private Boolean AllCachesUpdated()
        {
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                if (!p.Value.IsCacheUpdated)
                {
                    return false;
                }
            }
            return true;
        }

        private void InvalidateCaches()
        {
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                p.Value.IsCacheUpdated = false;
            }
        }

        private void fl_Initialized(object sender, EventArgs e)
        {
            if (_currentflname.Equals("UNITS"))
            {
                //_forcesInitialized = true;
            }
            else if (_currentflname.Equals("Equipment"))
            {
                //_equipmentInitialized = true;
            }

        }

        /// <summary>
        /// Called when a DataSource found in the DataSourceIds property is updated.
        /// </summary>
        /// <param name="dataSource">The DataSource being updated.</param>
        public void OnRefresh(DataSource dataSource)
        {
            try
            {
                if (counter > -1)
                {

                    client.AcceleratedDisplayLayers aclyrs = _map.Layers.FirstOrDefault(lyr => lyr is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;
                    foreach (client.FeatureLayer fl in aclyrs.ChildLayers.OfType<client.FeatureLayer>())
                    {
                        fl.Update();

                    }
                    refreshSelections();
                    counter = -1;

                }
                else
                {
                    if (!_isInitialized)
                    {
                        Boolean mapInit = MapInitialized();
                        if (mapInit)
                        {

                            InitLayer(dataSource);

                        }
                        if (OOBDataSources.Count > 0 && OOBDataSources.Count == numDs)
                        {
                            if (!_map.IsLoaded)
                            {
                                _map.Loaded += _map_Loaded;

                            }
                            else
                            {
                                Initialize();
                            }
                        }
                    }
                    else
                    {
                        if (!_blocking)
                        {
                            //_blocking = true;
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                UpdateCache(p.Value);
                            }

                            if (_cacheDirty && AllCachesUpdated())
                            {

                                //Initialize();
                                Update();
                                _cacheDirty = false;
                                InvalidateCaches();

                            }
                            //_blocking = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }


        void _map_ExtentChanged(object sender, client.ExtentEventArgs e)
        {
            counter = 0;
            
        }

        private void UpdateCache(OOBDataSource ods)
        {
            try
            {
                _blocking = true;
                String key = ods.Key;
                String Uid = ods.UIDField;
                String HF = ods.HFField;
                String DF = ods.DescField;
                String Labels = "";
                String DescFlds = "";
                DataSource ds = ods.DataSource;
                String CacheName = ods.Key;
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = Uid;
                fields["HF"] = HF;

                Int32 numBaseFlds = 2;
                if (DF != null)
                {
                    fields["DESCFIELD"] = DF;
                    numBaseFlds = 3;
                }

                Int32 arraySize = ods.LabelFields.Count + ods.DescriptionFields.Count + numBaseFlds;
                string[] qfields = new string[arraySize];
                qfields[0] = Uid;
                qfields[1] = HF;
                if (DF != null)
                {
                    qfields[2] = DF;
                }
                Int32 c = numBaseFlds;
                Boolean first = true;
                foreach (String f in ods.LabelFields)
                {
                    if (!first)
                    {
                        Labels += ",";
                    }
                    else
                    {
                        first = false;
                    }
                    Labels += f;
                    qfields[c] = f;
                    ++c;

                }
                first = true;
                foreach (String f in ods.DescriptionFields)
                {
                    if (!first)
                    {
                        DescFlds += ",";
                    }
                    else
                    {
                        first = false;
                    }
                    DescFlds += f;
                    qfields[c] = f;
                    ++c;
                }
                fields["LABELS"] = Labels;
                fields["DESCFLDS"] = DescFlds;
                
                Dictionary<String, Dictionary<String, object>> featureCache = oobcache.RetrieveFeatureCache(key);

                MapWidget mw = MapWidget.FindMapWidget(ds);
                client.FeatureLayer fl = mw.FindFeatureLayer(ds);
                //fl.Mode = client.FeatureLayer.QueryMode.OnDemand;
                client.UniqueValueRenderer r = fl.Renderer as client.UniqueValueRenderer;
                foreach (client.Graphic g in fl.Graphics)
                {
                    if (g.Attributes[Uid] != null)
                    {
                        String uidval = g.Attributes[Uid].ToString();
                        if (!featureCache.ContainsKey(uidval))
                        {
                            oobcache.AddFeature(key, g, ods.BaseDescription, ods.BaseLabel, fields, r);
                            _cacheDirty = true;

                        }
                        else
                        {
                            if (oobcache.UpdateFeature(key, uidval, ods.BaseDescription, ods.BaseLabel, g, fields))
                            {
                                _cacheDirty = true;
                            }
                        }
                    }
                }
                ods.IsCacheUpdated = true;
                _blocking = false;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }
        private void _map_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        private Boolean MapInitialized()
        {
            Boolean mapinit = false;
            if (!string.IsNullOrEmpty(MapWidgetId))
            {

                _mapw = OperationsDashboard.Instance.Widgets.Where(w => w.Id == MapWidgetId).FirstOrDefault() as MapWidget;
                if (_mapw != null)
                {
                    if (_mapw.IsInitialized)
                    {
                        _map = _mapw.Map;
                        _map.ExtentChanged += _map_ExtentChanged;
                        mapinit = true;
                    }
                    else
                    {
                        _mapw.Initialized += (s, e) =>
                        {
                            _map = _mapw.Map;
                            _map.ExtentChanged += _map_ExtentChanged;
                            mapinit = true;
                        };
                    }
                }
            }


            return mapinit;
        }
        /// <summary>
        /// Called when a DataSource found in the DataSourceIds property is updated.
        /// </summary>
        /// <param name="dataSource"></param>
        public void DataSourceUpdated(DataSource dataSource)
        {
            // Respond to the update from the selected data source using an async method to perform the query.
            //Initialize(dataSource);
        }


        #endregion
        private OOBNode UpdateOrderOfBattle(String updatetype, OOBNode root)
        {
            try
            {
                Dictionary<String, Dictionary<String, object>> fFeatures = oobcache.RetrieveFeatureCache("UNITS");
                String updatekey = updatetype + "_UPDATE";
                Dictionary<String, Dictionary<String, object>> updatefeatures = oobcache.RetrieveFeatureCache(updatekey);
                OOBDataSource ods = OOBDataSources[updatetype];
                Boolean useIcon = ods.UseIcon;
                DataSource ds = OOBDataSources[updatetype].DataSource;
                String uid = null;
                object hfuidobj = null;
                object flabelobj = null;
                object hflabelobj = null;
                object descobject = null;
                object hfdescobject = null;
                ImageSource uidimgsrc = null;
                ImageSource hfimgsrc = null;


                foreach (KeyValuePair<String, Dictionary<String, object>> pair in updatefeatures)
                {
                    String hfuid = null;
                    String flabel = null;
                    String hflabel = null;
                    String desclabel = null;
                    String hfdesclabel = null;
                    uidimgsrc = null;
                    hfimgsrc = null;
                    uid = pair.Key;
                    hfuidobj = pair.Value["HF"];
                    flabelobj = pair.Value["LABEL"];
                    descobject = pair.Value["DESCRIPTION"];
                    if (hfuidobj != null)
                    {
                        hfuid = hfuidobj.ToString();
                        if (fFeatures.ContainsKey(hfuid))
                        {
                            hflabelobj = fFeatures[hfuid]["LABEL"];
                            hfdescobject = fFeatures[hfuid]["DESCRIPTION"];
                            if (hflabelobj != null)
                            {
                                hflabel = hflabelobj.ToString();

                            }
                            else
                            {
                                hflabel = hfuid;
                            }
                            if (hfdescobject != null)
                            {
                                hfdesclabel = hfdescobject.ToString();
                            }
                            else
                            {
                                hfdesclabel = null;
                            }
                            if (fFeatures[hfuid]["ICON"] != null)
                            {
                                hfimgsrc = fFeatures[hfuid]["ICON"] as ImageSource;
                            }
                        }
                        else
                        {
                            hflabel = "No feature (" + hfuid + ") found in Units Datasource";
                        }

                    }
                    else
                    {
                        hfuid = "no_higher_formation";
                    }

                    if (flabelobj != null)
                    {
                        flabel = flabelobj.ToString();
                    }
                    if (descobject != null)
                    {
                        desclabel = descobject.ToString();
                    }
                    if (useIcon)
                    {
                        if (pair.Value["ICON"] != null)
                        {
                            uidimgsrc = pair.Value["ICON"] as ImageSource;
                        }
                    }
                    else
                    {
                        uidimgsrc = null;
                    }

                    root = this.AddNode(uid, flabel, desclabel, uidimgsrc, hfuid, hflabel, hfdesclabel, hfimgsrc, ods, root);
                }
                updatefeatures.Clear();
                return root;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
                return null;
            }
        }

        private OOBNode CreateOrderOfBattle(String updatetype, OOBNode root)
        {
            try
            {
                Dictionary<String, Dictionary<String, object>> fFeatures = oobcache.RetrieveFeatureCache("UNITS");
                Dictionary<String, Dictionary<String, object>> updatefeatures = oobcache.RetrieveFeatureCache(updatetype);
                OOBDataSource ods = OOBDataSources[updatetype];
                DataSource ds = OOBDataSources[updatetype].DataSource;
                Boolean useIcon = ods.UseIcon;
                String uid = null;
                object hfuidobj = null;
                object flabelobj = null;
                object hflabelobj = null;
                object descobject = null;
                object hfdescobject = null;
                ImageSource uidimgsrc = null;
                ImageSource hfimgsrc = null;


                foreach (KeyValuePair<String, Dictionary<String, object>> pair in updatefeatures)
                {
                    String hfuid = null;
                    String flabel = null;
                    String hflabel = null;
                    String desclabel = null;
                    String hfdesclabel = null;
                    uidimgsrc = null;
                    hfimgsrc = null;
                    uid = pair.Key;
                    hfuidobj = pair.Value["HF"];
                    flabelobj = pair.Value["LABEL"];
                    descobject = pair.Value["DESCRIPTION"];
                    if (hfuidobj != null)
                    {
                        hfuid = hfuidobj.ToString();
                        if (fFeatures.ContainsKey(hfuid))
                        {
                            hflabelobj = fFeatures[hfuid]["LABEL"];
                            hfdescobject = fFeatures[hfuid]["DESCRIPTION"];
                            if (hflabelobj != null)
                            {
                                hflabel = hflabelobj.ToString();

                            }
                            else
                            {
                                hflabel = hfuid;
                            }
                            if (hfdescobject != null)
                            {
                                hfdesclabel = hfdescobject.ToString();
                            }
                            else
                            {
                                hfdesclabel = null;
                            }
                            if (fFeatures[hfuid]["ICON"] != null)
                            {
                                hfimgsrc = fFeatures[hfuid]["ICON"] as ImageSource;
                            }
                        }
                        else
                        {
                            hflabel = "No feature (" + hfuid + ") found in Units Datasource";
                        }

                    }
                    else
                    {
                        hfuid = "no_higher_formation";
                    }

                    if (flabelobj != null)
                    {
                        flabel = flabelobj.ToString();
                    }
                    if (descobject != null)
                    {
                        desclabel = descobject.ToString();
                    }
                    if (useIcon)
                    {
                        if (pair.Value["ICON"] != null)
                        {
                            uidimgsrc = pair.Value["ICON"] as ImageSource;
                        }
                    }
                    else
                    {
                        uidimgsrc = null;
                    }

                    root = this.AddNode(uid, flabel, desclabel, uidimgsrc, hfuid, hflabel, hfdesclabel, hfimgsrc, ods, root);
                }
                return root;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
                return null;
            }
        }


        OOBNode AddNode(String uid, String label, String desc, ImageSource uidis, String hfuid, String hflabel, String hfdesc, ImageSource hfis, OOBDataSource ods, OOBNode root)
        {
            try
            {
                OOBTree tree = tv.Tag as OOBTree;
                DataSource ds = ods.DataSource;
                String dstype = ods.Key;
                OOBNode uidnode = root.GetDescendant(uid);
                OOBNode hfnode = root.GetDescendant(hfuid);

                if (uidnode == null && hfnode == null)
                {
                    uidnode = new OOBNode(uid, label, desc, ods, dstype, uidis);
                    uidnode.Parent = hfnode;
                    uidnode.ParentName = hfuid;
                    uidnode.UUID = GenerateId();
                    if (hfuid.Equals("no_higher_formation"))
                    {
                        root.addChild(uidnode);
                    }
                    else
                    {
                        hfnode = new OOBNode(hfuid, hflabel, hfdesc, OOBDataSources["UNITS"], "UNITS", hfis);
                        hfnode.UUID = GenerateId();
                        hfnode.addChild(uidnode);
                        root.addChild(hfnode);
                    }
                }
                else if (uidnode == null && hfnode != null)
                {
                    uidnode = new OOBNode(uid, label, desc, ods, dstype, uidis);
                    uidnode.UUID = GenerateId();
                    uidnode.Parent = hfnode;
                    uidnode.ParentName = hfuid;
                    hfnode.Description = hfdesc;
                    hfnode.Name = hflabel;
                    hfnode.Icon = hfis;
                    hfnode.addChild(uidnode);
                }
                else if (uidnode != null && hfnode == null)
                {

                    uidnode.Parent = hfnode;
                    uidnode.ParentName = hfuid;
                    uidnode.Icon = uidis;
                    uidnode.Description = desc;
                    if (!hfuid.Equals("no_higher_formation"))
                    {
                        hfnode = new OOBNode(hfuid, hflabel, hfdesc, OOBDataSources["UNITS"], "UNITS", hfis);
                        hfnode.UUID = GenerateId();
                        hfnode.addChild(uidnode);
                        root.addChild(hfnode);
                        root.removeChild(uidnode.Key);
                    }

                }
                else if (uidnode != null && hfnode != null)
                {
                    uidnode.Icon = uidis;
                    uidnode.Description = desc;
                    uidnode.Name = label;
                    hfnode.Name = hflabel;
                    hfnode.Description = hfdesc;
                    hfnode.Icon = hfis;
                    uidnode.ParentName = hfuid;
                    uidnode.Parent = hfnode;
                }
                return root;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
                return null;
            }
        }
        private String GenerateId()
        {
            return System.Guid.NewGuid().ToString().Replace("-", "");
        }

        private void FindItemById(String uuid, TreeViewItem item)
        {

        }

        private TreeViewItem CreateTreeViewItem(OOBNode n)
        {
            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = true;
            item.ContextMenu = _cm;
            item.Selected += set_Mode;
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            Image image = new Image();
            image.Source = n.Icon;
            Label lbl = new Label();
            lbl.Content = n.Name;
            stack.Children.Add(image);
            stack.Children.Add(lbl);
            if (!String.IsNullOrEmpty(n.Description))
            {
                StackPanel descStack = new StackPanel();
                descStack.Orientation = Orientation.Vertical;
                descStack.Children.Add(stack);
                Label descLabel = new Label();
                descLabel.Margin = new Thickness(30.0, 0, 0, 0);
                descLabel.Content = n.Description;
                descStack.Children.Add(descLabel);
                item.Header = descStack;
            }
            else
            {
                item.Header = stack;
            }
            item.Tag = n;
            return item;
        }

        private void UpdateTreeViewItem(TreeViewItem item, OOBNode n)
        {
            //Boolean isExpanded = item.IsExpanded;
            item.ContextMenu = _cm;
            item.Selected += set_Mode;
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            Image image = new Image();
            image.Source = n.Icon;
            Label lbl = new Label();
            lbl.Content = n.Name;
            stack.Children.Add(image);
            stack.Children.Add(lbl);
            if (!String.IsNullOrEmpty(n.Description))
            {
                StackPanel descStack = new StackPanel();
                descStack.Orientation = Orientation.Vertical;
                descStack.Children.Add(stack);
                Label descLabel = new Label();
                descLabel.Margin = new Thickness(30.0, 0, 0, 0);
                descLabel.Content = n.Description;
                descStack.Children.Add(descLabel);
                item.Header = descStack;
            }
            else
            {
                item.Header = stack;
            }
        }

        private ItemCollection UpdateTreeView(OOBNode root, ItemCollection ic)
        {

            foreach (KeyValuePair<String, OOBNode> pair in root.Children)
            {
                OOBNode n = pair.Value;
                TreeViewItem item = GetItemFromTreeView(n.UUID, ic);

                if (item == null)
                {
                    item = CreateTreeViewItem(n);
                    ic.Add(item);
                }
                else
                {
                    UpdateTreeViewItem(item, n);
                }

                if (pair.Value.numChildren > 0)
                {
                    this.UpdateTreeView(n, item.Items);
                }
            }
            return ic;
        }
        private TreeViewItem GetItemFromTreeView(String UUID, ItemCollection ic)
        {
            TreeViewItem tvitem = null;
            foreach (TreeViewItem i in ic)
            {
                OOBNode n = i.Tag as OOBNode;
                if (UUID.Equals(n.UUID))
                {
                    tvitem = i;
                    break;
                }
                else
                {
                    if (i.Items.Count > 0)
                    {
                        tvitem = GetItemFromTreeView(UUID, i.Items);
                    }

                }
            }
            return tvitem;
        }

        private void DataSourceBox_Loaded(object sender, RoutedEventArgs e)
        {

        }


        private client.FeatureLayer GetLayerFromMap(DataSource ds)
        {
            MapWidget mw = MapWidget.FindMapWidget(ds);
            client.FeatureLayer fl = mw.FindFeatureLayer(ds);
            if (mw != null)
            {
                var dataSourceFromSameWidget = OperationsDashboard.Instance.DataSources.Select((datasrc) =>
                {
                    client.FeatureLayer flyr = mw.FindFeatureLayer(datasrc);
                    return ((flyr != null) && (datasrc.IsSelectable)) ? flyr : null;
                });
                if (!dataSourceFromSameWidget.Contains(fl))
                    return null;

            }
            return fl;

        }
        private void show(object sender, RoutedEventArgs e)
        {
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            OOBDataSource ods = n.NodeDataSource;
            
            String wc = null;
            
            Dictionary<String, String> whereclauses = new Dictionary<String, String>();
            switch (mode)
            {
                case selectionmode.Unit:

                    String uidfld = ods.UIDField;
                    wc = uidfld + "= '" + n.Key + "'";
                    whereclauses.Add(n.NodeDataSource.Key, wc);
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals(n.NodeDataSource.Key))
                        {
                            wc = p.Value.UIDField + " = 'NO_FEATURE_SELECTED'";
                            whereclauses.Add(p.Key, wc);
                        }
                    }

                    break;
                case selectionmode.Child:
                    if (n.numChildren < 1)
                        return;
                    if (OOBDataSources.Count > 1)
                    {
                        if (n.CType.Equals("BOTH"))
                        {
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                wc = p.Value.HFField + "= '" + n.Key + "'";
                                whereclauses.Add(p.Key, wc);
                            }
                        }
                        else if (n.CType.Equals("DEPENDANTS"))
                        {
                            wc = n.NodeDataSource.UIDField + " = 'NO_FEATURE_SELECTED'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = p.Value.HFField + "= '" + n.Key + "'";
                                    whereclauses.Add(p.Key, wc);
                                }

                            }
                        }
                        else if (n.CType.Equals("UNITS"))
                        {
                            wc = ods.HFField + "= '" + n.Key + "'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                        }
                    }
                    else
                    {
                        wc = ods.HFField + "= '" + n.Key + "'";
                        whereclauses.Add(n.NodeDataSource.Key, wc);
                    }
                    break;
                case selectionmode.Dep:
                    if (n.numChildren < 1)
                        return;
                    if (OOBDataSources.Count > 1)
                    {
                        if (n.CType.Equals("BOTH"))
                        {
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                wc = ConstructDescendantWhereClause(n, "", p.Value.Key, true);
                                whereclauses.Add(p.Value.Key, wc);
                            }
                        }
                        if (n.CType.Equals("DEPENDANTS"))
                        {
                            wc = n.NodeDataSource.UIDField + " = 'NO_FEATURE_SELECTED'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = ConstructDescendantWhereClause(n, "", p.Value.Key, true);
                                    whereclauses.Add(p.Key, wc);
                                }
                                
                            }
                        }
                        if (n.CType.Equals("UNITS"))
                        {
                            wc = ConstructDescendantWhereClause(n, "", "UNITS", true);
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                        }
                    }
                    else
                    {
                        wc = ConstructDescendantWhereClause(n, "", n.CType, true);
                        whereclauses.Add(n.NodeDataSource.Key, wc);
                    }
                    break;
                case selectionmode.UnitChild:
                    if (OOBDataSources.Count > 1)
                    {
                        if (n.CType.Equals("BOTH"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "' OR " + ods.HFField + "= '" + n.Key + "'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = p.Value.HFField + "= '" + n.Key + "'";
                                    whereclauses.Add(p.Key, wc);
                                }
                            }
                        }
                        else if (n.CType.Equals("UNITS"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "' OR " + ods.HFField + "= '" + n.Key + "'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = p.Value.UIDField + " = 'NO_FEATURE_SELECTED'";
                                    whereclauses.Add(p.Key, wc);
                                }
                            }
                        }
                        else if (n.CType.Equals("DEPENDANTS"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = p.Value.HFField + "= '" + n.Key + "'";
                                    whereclauses.Add(p.Key, wc);
                                }
                            }
                        }
                    }
                    break;
                case selectionmode.UnitDep:

                    if (OOBDataSources.Count > 1)
                    {
                        if (n.CType.Equals("BOTH"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "'";
                            wc = ConstructDescendantWhereClause(n, wc, "UNITS", true);
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = ConstructDescendantWhereClause(n, "", p.Value.Key, true);
                                    whereclauses.Add(p.Value.Key, wc);
                                }
                            }
                        }
                        if (n.CType.Equals("UNITS"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "'";
                            wc = ConstructDescendantWhereClause(n, wc, "UNITS", true);
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                        }

                        if (n.CType.Equals("DEPENDANTS"))
                        {
                            wc = ods.UIDField + "= '" + n.Key + "'";
                            whereclauses.Add(n.NodeDataSource.Key, wc);
                            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                            {
                                if (!p.Key.Equals("UNITS"))
                                {
                                    wc = ConstructDescendantWhereClause(n, "", p.Value.Key, true);
                                    whereclauses.Add(p.Key, wc);
                                }
                            }
                        }

                    }
                    else
                    {
                        wc = ods.UIDField + "= '" + n.Key + "'";
                        wc = ConstructDescendantWhereClause(n, wc, "UNITS", true);
                        whereclauses.Add(n.NodeDataSource.Key, wc);
                    }
                    break;
                case selectionmode.All:

                    if (OOBDataSources.Count > 1)
                    {
                        foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                        {
                            wc = "";
                            whereclauses.Add(p.Key, wc);
                        }
                    }
                    break;
            }

            showFeatures(whereclauses);
        }
        private void clear_selection(object sender, RoutedEventArgs e)
        {
            clearSelection();
            ZoomToSelectedButton.IsEnabled = false;
            PanToSelectedButton.IsEnabled = false;
            ClearSelectionButton.IsEnabled = false;
            _queries = null;
        }
        private void select(object sender, RoutedEventArgs e)
        {
            OOBTree tree = tv.Tag as OOBTree;
            OOBNode n = ((TreeViewItem)tv.SelectedItem).Tag as OOBNode;
            Dictionary<String, Query> queries = null;
            switch (mode)
            {
                case selectionmode.Unit:
                    queries = queryUnit(n);
                    break;
                case selectionmode.Child:
                    if (n.numChildren < 1)
                        return;
                    queries = queryChildren(n);
                    break;
                case selectionmode.Dep:
                    if (n.numChildren < 1)
                        return;
                    queries = queryDescendants(n);
                    break;
                case selectionmode.UnitChild:
                    queries = queryUnitChildren(n);
                    break;
                case selectionmode.UnitDep:
                    queries = queryUnitDescendants(n);
                    break;
                case selectionmode.All:
                    
                    break;
            }
            _queries = queries;
            foreach (KeyValuePair<String, Query> pair in queries)
            {
                selectFeatures(pair.Value, OOBDataSources[pair.Key].DataSource);
            }
        }

        private void refreshSelections()
        {
            if (_queries == null)
                return;
            foreach (KeyValuePair<String, Query> pair in _queries)
            {
                selectFeatures(pair.Value, OOBDataSources[pair.Key].DataSource);
            }
        }


        private Dictionary<String, Query> queryUnit(OOBNode node)
        {
            //DataSource dataSrc = node.NodeDataSource.DataSource;
            OOBDataSource ods = node.NodeDataSource;
            Dictionary<String, Query> queries = new Dictionary<String, Query>();
            OOBTree tree = tv.Tag as OOBTree;

            String qtype = node.NodeDataSource.Key;
            Query q = new Query();
            q.ReturnGeometry = true;
            String oidfld = ods.DataSource.ObjectIdFieldName;
            String uidfld = ods.UIDField;
            String hffld = ods.HFField;
            string[] fields = new string[] { oidfld, uidfld, hffld };
            q.Fields = fields;
            String wc = uidfld + "= '" + node.Key + "'";
            q.WhereClause = wc;
            q.Fields = fields;
            queries.Add(qtype, q);

            return queries;
        }

        private Dictionary<String, Query> queryChildren(OOBNode node)
        {
            OOBDataSource ods = node.NodeDataSource;
            String ctype = node.CType;
            OOBTree tree = tv.Tag as OOBTree;
            Dictionary<String, Query> queries = new Dictionary<String, Query>();
            Query q = null;
            String oidfld = null;
            String uidfld = null;
            String hffld = null;
            String wc = null;

            switch (ctype)
            {
                case "UNITS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.HFField + "= '" + node.Key + "'";
                    q.WhereClause = wc;
                    queries.Add(ods.Key, q);
                    break;
                case "DEPENDANTS":
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = p.Value.HFField + "= '" + node.Key + "'";
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
                case "BOTH":
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        q = new Query();
                        q.ReturnGeometry = true;
                        oidfld = p.Value.DataSource.ObjectIdFieldName;
                        uidfld = p.Value.UIDField;
                        hffld = p.Value.HFField;
                        q.Fields = new string[] { oidfld, uidfld, hffld };
                        wc = p.Value.HFField + "= '" + node.Key + "'";
                        q.WhereClause = wc;
                        queries.Add(p.Key, q);
                    }
                    break;
            }
            
            return queries;
        }

        private Dictionary<String, Query> queryUnitChildren(OOBNode node)
        {
            OOBDataSource ods = node.NodeDataSource;
            String ctype = node.CType;
            OOBTree tree = tv.Tag as OOBTree;
            Dictionary<String, Query> queries = new Dictionary<String, Query>();
            Query q = null;
            String oidfld = null;
            String uidfld = null;
            String hffld = null;
            String wc = null;
            switch (ctype)
            {
                case "UNITS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "' OR " + ods.HFField + "= '" + node.Key + "'";
                    q.WhereClause = wc;
                    queries.Add(ods.Key, q);
                    break;
                case "DEPENDANTS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "'";
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = p.Value.HFField + "= '" + node.Key + "'";
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
                case "BOTH":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "' OR " + ods.HFField + "= '" + node.Key + "'";
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = p.Value.HFField + "= '" + node.Key + "'";
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
            }

            return queries;
        }

        private Dictionary<String, Query> queryDescendants(OOBNode node)
        {
            OOBDataSource ods = node.NodeDataSource;
            String ctype = node.CType;
            OOBTree tree = tv.Tag as OOBTree;
            Dictionary<String, Query> queries = new Dictionary<String, Query>();
            Query q = null;
            String oidfld = null;
            String uidfld = null;
            String hffld = null;
            String wc = null;
            switch (ctype)
            {
                case "UNITS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ConstructDescendantWhereClause(node, "", "UNITS", true);
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    break;
                case "DEPENDANTS":
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = ConstructDescendantWhereClause(node, "", p.Value.Key, true);
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
                case "BOTH":
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        q = new Query();
                        q.ReturnGeometry = true;
                        oidfld = p.Value.DataSource.ObjectIdFieldName;
                        uidfld = p.Value.UIDField;
                        hffld = p.Value.HFField;
                        q.Fields = new string[] { oidfld, uidfld, hffld };
                        wc = ConstructDescendantWhereClause(node, "", p.Value.Key, true);
                        q.WhereClause = wc;
                        queries.Add(p.Key, q);
                    }
                    break;
            }
            return queries;
        }

        String ConstructDescendantWhereClause(OOBNode node, String wc, String ntype, Boolean IsFirst)
        {

            String uidFieldname = null;
            if (ntype.Equals("UNITS"))
            {
                uidFieldname = node.NodeDataSource.UIDField;
            }
            else
            {
                uidFieldname = OOBDataSources[ntype].UIDField;
            }

            if (node.NType.Equals(ntype) && (!IsFirst))
            {
                if (wc != "")
                {
                    wc += " OR ";
                }

                wc += uidFieldname + " = '" + node.Key + "'";
            }

            if (node.numChildren > 0)
            {
                foreach (KeyValuePair<String, OOBNode> pair in node.Children)
                {
                    wc = ConstructDescendantWhereClause(pair.Value, wc, ntype, false);
                }
            }


            return wc;
        }

        private Dictionary<String, Query> queryUnitDescendants(OOBNode node)
        {
            OOBDataSource ods = node.NodeDataSource;
            String ctype = node.CType;
            OOBTree tree = tv.Tag as OOBTree;
            Dictionary<String, Query> queries = new Dictionary<String, Query>();
            Query q = null;
            String oidfld = null;
            String uidfld = null;
            String hffld = null;
            String wc = null;
            switch (ctype)
            {
                case "UNITS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "'";
                    wc = ConstructDescendantWhereClause(node, wc, "UNITS", true);
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    break;
                case "DEPENDANTS":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "'";
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = ConstructDescendantWhereClause(node, "", p.Value.Key, true);
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
                case "BOTH":
                    q = new Query();
                    q.ReturnGeometry = true;
                    oidfld = ods.DataSource.ObjectIdFieldName;
                    uidfld = ods.UIDField;
                    hffld = ods.HFField;
                    q.Fields = new string[] { oidfld, uidfld, hffld };
                    wc = ods.UIDField + "= '" + node.Key + "'";
                    wc = ConstructDescendantWhereClause(node, wc, "UNITS", true);
                    q.WhereClause = wc;
                    queries.Add("UNITS", q);
                    foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
                    {
                        if (!p.Key.Equals("UNITS"))
                        {
                            q = new Query();
                            q.ReturnGeometry = true;
                            oidfld = p.Value.DataSource.ObjectIdFieldName;
                            uidfld = p.Value.UIDField;
                            hffld = p.Value.HFField;
                            q.Fields = new string[] { oidfld, uidfld, hffld };
                            wc = ConstructDescendantWhereClause(node, "", p.Value.Key, true);
                            q.WhereClause = wc;
                            queries.Add(p.Key, q);
                        }
                    }
                    break;
            }
            return queries;
        }

        String ConstructUnitDescendantWhereClause(OOBNode node, String wc, String ntype)
        {
            String uidFieldname = null;
            if (ntype.Equals("UNITS"))
            {
                uidFieldname = node.NodeDataSource.UIDField;
            }
            else
            {
                uidFieldname = OOBDataSources[ntype].UIDField;
            }

            if (node.NType.Equals(ntype))
            {
                if (wc != "")
                {
                    wc += " OR ";
                }

                wc += uidFieldname + " = '" + node.Key + "'";
            }

            if (node.numChildren > 0)
            {
                foreach (KeyValuePair<String, OOBNode> pair in node.Children)
                {
                    wc = ConstructUnitDescendantWhereClause(pair.Value, wc, ntype);
                }
            }


            return wc;
        }

        
        private void showFeatures(Dictionary<String, String> whereclauses)
        {
            CancelShowButton.IsEnabled = true;
            OOBTree tree = tv.Tag as OOBTree;
            Dictionary<String, String> keys = tree.Keys;
            Dictionary<String, DataSource> datasources = tree.DataSources;
            foreach (KeyValuePair<String, OOBDataSource> p in OOBDataSources)
            {
                OOBDataSource ods = p.Value;
                DataSource ds = ods.DataSource;
                MapWidget mapW = MapWidget.FindMapWidget(ds);
                if (mapW != null)
                {
                    // Get the feature layer in the map for the data source.
                    client.FeatureLayer fl = mapW.FindFeatureLayer(ds);
                    foreach (client.Field f in ds.Fields)
                    {
                        fl.OutFields.Add(f.Name);
                    }
                    
                    if (whereclauses.ContainsKey(ods.Key))
                    {
                        fl.Where = whereclauses[ods.Key];
                        fl.Update();
                    }
                }
            }
            refreshSelections();
            
        }
        private void clear_showFeatures(object sender, RoutedEventArgs e)
        {
            OOBTree tree = tv.Tag as OOBTree;

            foreach (KeyValuePair<String, DataSource> pair in tree.DataSources)
            {
                MapWidget mw = MapWidget.FindMapWidget(pair.Value);
                client.FeatureLayer fl = mw.FindFeatureLayer(pair.Value);
                fl.Where = "";
            }
            refreshSelections();
            CancelShowButton.IsEnabled = false;
        }

        private async void selectFeatures(Query q, DataSource dataSrc)
        {
            if (!dsList.Contains(dataSrc))
            {
                dsList.Add(dataSrc);
            }
            //q.SpatialFilter = oobcache.CacheExtent;
            QueryResult result = await dataSrc.ExecuteQueryAsync(q);
            if (result.Features.Count > 0)
            {
                ClearSelectionButton.IsEnabled = true;
            }
            if ((result == null) || (result.Canceled) || (result.Features == null) || (result.Features.Count < 1))
                return;

            // Get the array of IDs from the query results.
            var resultOids = from feature in result.Features select System.Convert.ToInt32(feature.Attributes[dataSrc.ObjectIdFieldName]);
            
            IEnumerable<client.Geometry.MapPoint> ptcoll = from feature in result.Features select feature.Geometry as client.Geometry.MapPoint;

            selGeo = new client.Geometry.PointCollection(ptcoll);
            // Find the map layer in the map widget that contains the data source.
            MapWidget mapW = MapWidget.FindMapWidget(dataSrc);

            if (mapW != null)
            {
                // Get the feature layer in the map for the data source.
                client.FeatureLayer featureL = mapW.FindFeatureLayer(dataSrc);
                //featureL.Mode = client.FeatureLayer.QueryMode.SelectionOnly;
                featureL.ClearSelection();
                //selGeo.Clear();

                // NOTE: Can check here if the feature layer is selectable, using code shown above.

                // For each result feature, find the corresponding graphic in the map by OID and select it.

                foreach (client.Graphic feature in featureL.Graphics)
                {
                    int featureOid;
                    int.TryParse(feature.Attributes[dataSrc.ObjectIdFieldName].ToString(), out featureOid);
                    if (resultOids.Contains(featureOid))
                    {
                        //feature.Geometry
                        if (!ZoomToSelectedButton.IsEnabled)
                        {
                            ZoomToSelectedButton.IsEnabled = true;
                        }
                        if (!PanToSelectedButton.IsEnabled)
                        {
                            PanToSelectedButton.IsEnabled = true;
                        }

                        feature.Select();
                        //selGeo.Add(feature.Geometry as client.Geometry.MapPoint);
                    }
                }
            }
        }
        private void clearSelection()
        {
            foreach (DataSource d in dsList)
            {

                MapWidget mapW = MapWidget.FindMapWidget(d);

                if (mapW != null)
                {
                    // Get the feature layer in the map for the data source.
                    client.FeatureLayer featureL = mapW.FindFeatureLayer(d);
                    featureL.ClearSelection();
                }
            }
            dsList.Clear();
        }

        private void pan_to_selected(object sender, RoutedEventArgs e)
        {
            panToSelected();
        }

        private void panToSelected()
        {
            client.Geometry.MultiPoint selFeatGeo = new client.Geometry.MultiPoint();
            DataSource d = dsList[0];
            selFeatGeo = new client.Geometry.MultiPoint(selGeo);

            MapWidget mapW = MapWidget.FindMapWidget(d);

            if (mapW != null)
            {
                mapW.Map.PanTo(selFeatGeo.Extent.Expand(1.1));
            }
        }

        private void zoom_to_selected(object sender, RoutedEventArgs e)
        {
            zoomToSelected();
        }


        private void zoomToSelected()
        {
            client.Geometry.MultiPoint selFeatGeo = new client.Geometry.MultiPoint(selGeo);
            DataSource d = dsList[0];
            MapWidget mapW = MapWidget.FindMapWidget(d);

            if (mapW != null)
            {
                client.Geometry.Envelope extent = null;
                double xmin, ymin, xmax, ymax;
                if (selFeatGeo.Points.Count > 0)
                {
                    if (selFeatGeo.Points.Count == 1)
                    {
                        xmin = CoordinateHelper.expandCoord(selFeatGeo.Extent.XMin, extentType.min);
                        ymin = CoordinateHelper.expandCoord(selFeatGeo.Extent.YMin, extentType.min);
                        xmax = CoordinateHelper.expandCoord(selFeatGeo.Extent.XMax, extentType.max);
                        ymax = CoordinateHelper.expandCoord(selFeatGeo.Extent.YMax, extentType.max);
                        extent = new client.Geometry.Envelope(xmin, ymin, xmax, ymax);
                        extent.SpatialReference = selFeatGeo.Extent.SpatialReference;
                    }
                    else
                    {
                        extent = selFeatGeo.Extent.Expand(1.1);
                    }
                }
                mapW.Map.ZoomTo(extent);
            }

        }
        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (mode != selectionmode.None)
            {
                SelectFeaturesButton.IsEnabled = true;
                ShowFeaturesButton.IsEnabled = true;
            }
        }
        private void set_subordinates(object sender, RoutedEventArgs e)
        {
            if ((Boolean)chkSubordinate.IsChecked)
            {
                rbAll.IsEnabled = true;
                rbImmediate.IsEnabled = true;
            }
            else
            {
                rbAll.IsEnabled = false;
                rbImmediate.IsEnabled = false;
            }
        }
        private void set_Mode(object sender, RoutedEventArgs e)
        {

            if ((Boolean)chkSubordinate.IsChecked)
            {
                rbAll.IsEnabled = true;
                rbImmediate.IsEnabled = true;
            }
            else
            {
                rbAll.IsEnabled = false;
                rbImmediate.IsEnabled = false;
            }
            Boolean u = (Boolean)chkUnit.IsChecked;
            Boolean c = false;
            Boolean d = false;
            if ((Boolean)chkSubordinate.IsChecked)
            {
                c = (Boolean)rbImmediate.IsChecked;
                d = (Boolean)rbAll.IsChecked;
            }
            else
            {
                c = false;
                d = false;
            }
            String selMode = "Selection Mode: ";
            if (u && !c && !d)
            {
                mode = selectionmode.Unit;
                txtSelectMode.Text = selMode + "Unit only";
            }
            else if (u && c && !d)
            {
                mode = selectionmode.UnitChild;
                txtSelectMode.Text = selMode + "Unit and Immediate subordinates";
            }
            else if (!u && c && !d)
            {
                mode = selectionmode.Child;
                txtSelectMode.Text = selMode + "Immediate subordinates only";
            }
            else if (!u && !c && d)
            {
                mode = selectionmode.Dep;
                txtSelectMode.Text = selMode + "All subordinates";
            }
            else if (!u && c && d)
            {
                mode = selectionmode.Dep;
                txtSelectMode.Text = selMode + "All subordinates";
            }
            else if (u && !c && d)
            {
                mode = selectionmode.UnitDep;
                txtSelectMode.Text = selMode + "Unit and all subordinates";
            }
            else if (u && c && d)
            {
                mode = selectionmode.UnitDep;
                txtSelectMode.Text = selMode + "Unit and all subordinates";
            }
            else if (!u && !c && !d)
            {
                mode = selectionmode.None;
                txtSelectMode.Text = selMode + "No selection mode";
            }
            if (mode == selectionmode.None)
            {
                ShowFeaturesButton.IsEnabled = false;
                SelectFeaturesButton.IsEnabled = false;
            }
            else
            {
                if (tv.SelectedItem == null)
                {
                    ShowFeaturesButton.IsEnabled = false;
                    SelectFeaturesButton.IsEnabled = false;
                }
                else
                {
                    ShowFeaturesButton.IsEnabled = true;
                    SelectFeaturesButton.IsEnabled = true;
                }
            }

        }


        //
        private void treeView_DragOver(object sender, DragEventArgs e)
        {
            Point currentPosition = e.GetPosition(tv);

            if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
               (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
            {
                // Verify that this is a valid drop and then store the drop target
                TreeViewItem item = GetNearestContainer(e.OriginalSource as UIElement);
                if (CheckDropTarget(draggedItem, item))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }
        private void treeView_Drop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;

            // Verify that this is a valid drop and then store the drop target
            TreeViewItem TargetItem = GetNearestContainer
                (e.OriginalSource as UIElement);
            if (TargetItem != null && draggedItem != null)
            {
                _target = TargetItem;
                e.Effects = DragDropEffects.Move;
            }
        }
        private void treeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseOverScrollbar(sender, e.GetPosition(sender as IInputElement)))
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {

                System.Windows.Point currentPos = e.GetPosition(tv);
                if ((Math.Abs(currentPos.X - _lastMouseDown.X) > 10.0) || (Math.Abs(currentPos.Y - _lastMouseDown.Y) > 10.0))
                {
                    draggedItem = (TreeViewItem)tv.SelectedItem;
                    if (draggedItem != null)
                    {
                        DragDropEffects finalDropEffect = DragDrop.DoDragDrop(tv, tv.SelectedValue, DragDropEffects.Move);
                        if ((finalDropEffect == DragDropEffects.Move) && (_target != null))
                        {
                            if (CheckDropTarget(draggedItem, _target))
                            {
                                CopyItem(draggedItem, _target);
                                UpdateOOBTree(draggedItem, _target);
                                draggedItem = null;
                            }
                        }
                    }

                }
            }
        }

        private void treeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(tv);
            }
        }

        private static bool IsMouseOverScrollbar(object sender, Point mousePosition)
        {
            if (sender is Visual)
            {
                HitTestResult hit = VisualTreeHelper.HitTest(sender as Visual, mousePosition);

                if (hit == null) return false;

                DependencyObject dObj = hit.VisualHit;
                while (dObj != null)
                {
                    if (dObj is ScrollBar) return true;

                    if ((dObj is Visual) || (dObj is Visual3D)) dObj = VisualTreeHelper.GetParent(dObj);
                    else dObj = LogicalTreeHelper.GetParent(dObj);
                }
            }

            return false;
        }
        private bool CheckDropTarget(TreeViewItem _sourceItem, TreeViewItem _targetItem)
        {
            //Check whether the target item is meeting your condition
            String sLabel = GetNameFromHeader(_sourceItem);
            String tLabel = GetNameFromHeader(_targetItem);
            bool _isEqual = false;
            if (!sLabel.Equals(tLabel))
            {
                _isEqual = true;
            }
            return _isEqual;

        }
        private String GetNameFromHeader(TreeViewItem item)
        {
            StackPanel stack = item.Header as StackPanel;

            if (item == null)
                return null;
            foreach (object o in stack.Children)
            {
                if (o is Label)
                {
                    return ((Label)o).Content.ToString();
                }
            }
            return null;
        }
        private TreeViewItem GetNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            TreeViewItem container = element as TreeViewItem;
            while ((container == null) && (element != null))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }
            return container;
        }
        private void CopyItem(TreeViewItem _sourceItem, TreeViewItem _targetItem)
        {

            //Asking user wether he want to drop the dragged TreeViewItem here or not

            //adding dragged TreeViewItem in target TreeViewItem
            addChild(_sourceItem, _targetItem);

            //finding Parent TreeViewItem of dragged TreeViewItem 
            TreeViewItem ParentItem = FindVisualParent<TreeViewItem>(_sourceItem);
            // if parent is null then remove from TreeView else remove from Parent TreeViewItem
            if (ParentItem == null)
            {
                tv.Items.Remove(_sourceItem);
            }
            else
            {
                ParentItem.Items.Remove(_sourceItem);
            }


        }
        public void addChild(TreeViewItem _sourceItem, TreeViewItem _targetItem)
        {
            // add item in target TreeViewItem 
            TreeViewItem item1 = new TreeViewItem();
            StackPanel stack = CreateHeader(_sourceItem);
            item1.Header = stack;
            item1.Tag = _sourceItem.Tag;
            _targetItem.Items.Add(item1);
            foreach (TreeViewItem item in _sourceItem.Items)
            {
                addChild(item, item1);
            }
        }

        private StackPanel CreateHeader(TreeViewItem _sourceItem)
        {
            StackPanel stack = _sourceItem.Header as StackPanel;
            StackPanel newStack = new StackPanel();
            newStack.Orientation = Orientation.Horizontal;
            int count = stack.Children.Count;
            while (stack.Children.Count > 0)
            {
                UIElement u = stack.Children[0];
                stack.Children.Remove(u);
                newStack.Children.Add(u);
            }
            return newStack;
        }

        static TObject FindVisualParent<TObject>(UIElement child) where TObject : UIElement
        {
            if (child == null)
            {
                return null;
            }

            UIElement parent = VisualTreeHelper.GetParent(child) as UIElement;

            while (parent != null)
            {
                TObject found = parent as TObject;
                if (found != null)
                {
                    return found;
                }
                else
                {
                    parent = VisualTreeHelper.GetParent(parent) as UIElement;
                }
            }

            return null;
        }
        private void UpdateOOBTree(TreeViewItem dragged, TreeViewItem target)
        {
            OOBNode dragnode = dragged.Tag as OOBNode;
            OOBNode parent = dragnode.Parent;
            OOBNode targetnode = target.Tag as OOBNode;
            targetnode.addChild(dragnode);
            parent.removeChild(dragnode.Key);
            dragnode.Parent = targetnode;
            //UpdateFeature(dragnode, targetnode);

        }
        //Doesnt work yet.  Waiting until we can get editing in dashboard
        /*private async void UpdateFeature(OOBNode n, OOBNode t)
        {
            
            DataSource d = n.NodeDataSource.DataSource;
            client.Editor edit = new client.Editor();
            ICommand save = edit.Save;
            
            client.FeatureLayer fl = _mapw.FindFeatureLayer(d);
            //fl.Mode = client.FeatureLayer.QueryMode.OnDemand;
            String token = GenerateToken(fl.Url, "arcgis", "GISpr0d9");
            //fl.Token = token;
            String UIDField = null;
            String HFField = null;
            if (n.NType.Equals("Forces"))
            {
                UIDField = ForceUIDFieldName;
                HFField = ForceHigherFormationFieldName;
            }
            else if (n.NType.Equals("Equipment"))
            {
                UIDField = EquipmentUIDFieldName;
                HFField = EquipmentHigherFormationFieldName;
            }
            
            client.Tasks.OutFields of = new client.Tasks.OutFields();
            //of.Add(fl.
            of.Add(d.ObjectIdFieldName);
            of.Add(UIDField);
            of.Add(HFField);
            Dictionary<String, Dictionary <String, String>> parameters = new Dictionary<String, Dictionary <String, String>>();
            RestClient rc = new RestClient();
            if (!fl.IsReadOnly)
            {
                fl.AutoSave = true;
                //client.Tasks.Query q = new client.Tasks.Query();
                Query q = new Query();
                String wc = ForceUIDFieldName + " = '" + n.Key + "'";
                q.ReturnGeometry = true;
                q.WhereClause = wc;
                QueryResult res = await d.ExecuteQueryAsync(q);
                var resultOids = from feat in res.Features select System.Convert.ToInt32(feat.Attributes[d.ObjectIdFieldName]);

                foreach (client.Graphic feat in fl.Graphics)
                {
                    int featureOid;
                    
                    int.TryParse(feat.Attributes[d.ObjectIdFieldName].ToString(), out featureOid);
                    if (resultOids.Contains(featureOid))
                    {
                        if (fl.IsUpdateAllowed(feat))
                        {
                            Dictionary<String, String> oid = new Dictionary<String, String>();
                            oid["type"] = "NUMBER";
                            oid["value"] = feat.Attributes[d.ObjectIdFieldName].ToString();
                            parameters[d.ObjectIdFieldName] = oid;
                            Dictionary<String, String> hf = new Dictionary<String, String>();
                            hf["type"] = "STRING";
                            hf["value"] = t.Key;
                            parameters[HFField] = hf;
                            client.Geometry.MapPoint p = feat.Geometry as client.Geometry.MapPoint;
                            String jsonstring = parseToString(p, parameters);
                            string endpoint = fl.Url + "/applyEdits/";
                            //string endpoint = fl.Url + "/updateFeatures/";
                            //feat.Attributes[HFField] = t.Key;
                            rc.EndPoint = endpoint;
                            rc.Method = HttpVerb.POST;
                            //var json = rc.MakeRequest("?token=" + token +  "&f=json" + "&features=" + jsonstring);
                            var json = rc.MakeRequest("?token" + token + "&f=json" + "&updates=" + jsonstring + "&gdbVersion = SMARTS.patr5136");
                            //var json = rc.MakeRequest("?f=json" + "&features=" + jsonstring);
                            //var json = rc.MakeRequest();

                            //fl.SaveEdits();
                            
                            
                        }
                    }
                }
                fl.Update();
            }

        }*/

        private String GenerateToken(String serviceurl, String user, String pw)
        {

            int i = serviceurl.IndexOf("arcgis") + 7;
            String baseurl = serviceurl.Substring(0, serviceurl.Length - i);
            //IEnumerable<client.IdentityManager.Credential> credentials = client.IdentityManager.Current.Credentials;
            client.IdentityManager identityManager = client.IdentityManager.Current;
            identityManager.ChallengeMethod = (myurl, callback, options) => identityManager.GenerateCredentialAsync(myurl, "patr5136", "IlmbgK!1", callback, options);
            IEnumerable<client.IdentityManager.Credential> credentials = identityManager.Credentials;
            String token = credentials.Where(c => c.Url == "https://afmcomstaging.esri.com/arcgis/sharing/rest/").First().Token;
            return token;
            /*String newurl = baseurl + "/tokens?username=" + user + "&password=" + pw;
            RestClient rc = new RestClient();
            rc.Method= HttpVerb.POST;
            var response = rc.MakeRequest(newurl);
            String token = response.ToString();
            token = token.Substring(0, token.Length - 1);
            return token;*/

        }
        private void parseResponse(string response)
        {
            //part of editing workflow
            /*JObject jsonResponse = JObject.Parse(response);
            var error = jsonResponse["error"];
            if (error == null)
            {
                JArray updateResults = (JArray)jsonResponse["updateResults"];
                if (updateResults == null || updateResults.Count == 0)
                    throw new Exception("Operation encountered an error");
                var success = from p in updateResults.Children()
                              select (string)p["success"];
                if (updateResults.Any(r => (string)r["success"] == "false"))
                    throw new Exception("Operation encountered an error");



            }
            else
                throw new Exception((string)jsonResponse["error"]["message"]);*/

        }

        private String parseToString(client.Geometry.MapPoint p, Dictionary<String, Dictionary<String, String>> parameters)
        {
            String jsonstring = "[{";
            jsonstring += parsePointToString(p) + ", ";
            jsonstring += parseattributes(parameters);

            jsonstring += "}]";
            return jsonstring;
        }

        private String parseattributes(Dictionary<String, Dictionary<String, String>> parameters)
        {
            String jsonstring = "\"attributes\": {";

            Boolean firstit = true;
            foreach (KeyValuePair<String, Dictionary<String, String>> pair in parameters)
            {
                String fld = pair.Key;
                String type = pair.Value["type"];
                String val = pair.Value["value"];
                String typeDilineators = null;
                String quote = "\"";

                if (type.Equals("STRING"))
                {
                    typeDilineators = quote;
                }
                else if (type.Equals("NUMBER"))
                {
                    typeDilineators = "";
                }
                if (!firstit)
                {
                    jsonstring += ", ";
                }
                else
                {
                    firstit = false;
                }
                jsonstring += quote + fld + quote + ": " + typeDilineators + val + typeDilineators;
            }
            jsonstring += "}";
            return jsonstring;
        }

        private String parsePointToString(client.Geometry.MapPoint p)
        {
            String jsonstring = "\"geometry\": {" + createcoord("x", p.X);
            jsonstring += ", ";
            jsonstring += createcoord("y", p.Y);
            jsonstring += "}";
            return jsonstring;
        }

        private String createcoord(String axis, Double x)
        {
            String quote = "\"";
            return quote + axis + quote + ":" + x.ToString();
        }
    }

    public class CoordinateHelper
    {
        public static double expandCoord(double c, extentType t)
        {
            if (t == extentType.max)
            {
                if (c < 0)
                    return c * 0.9999;
                else
                    return c * 1.0001;

            }
            else
            {
                if (c < 0)
                    return c * 1.0001;
                else
                    return c * 0.9999;
            }
        }
    }
}