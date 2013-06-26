using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using System.Windows.Input;
using System.Windows.Controls.Primitives;


namespace OOB.Config
{
    /// <summary>
    /// Interaction logic for OOBWidgetDialog.xaml
    /// </summary>
    
    public partial class OOBWidgetDialog : Window
    {
        public OOBCache cache = new OOBCache();
        public List<DataSource> DataSources { get; private set; }
        //public ESRI.ArcGIS.Client.Field ForceUIDField { get; private set; }
        //public ESRI.ArcGIS.Client.Field ForceHFField { get; private set; }
        //public ESRI.ArcGIS.Client.Field ForceLabelField { get; private set; }
        //public ESRI.ArcGIS.Client.Field EquipmentUIDField { get; private set; }
        //public ESRI.ArcGIS.Client.Field EquipmentHFField { get; private set; }
        //public ESRI.ArcGIS.Client.Field EquipmentLabelField { get; private set; }
        public String OOBName {get; private set;}
        public string Caption { get; private set; }
        private Dictionary<String, OOBDataSource> _datasources = new Dictionary<String, OOBDataSource>();
        public Dictionary<String, OOBDataSource> OOBDataSources { get { return _datasources; } }
        private String curLabelVal = "";
        private DescriptionType _currentDescType = DescriptionType.None;
        private String _currentBaseDesc = "";
        private DataSource _currentDS;
        private List<String> _currentLabelList = new List<String>();
        private List<String> _currentDescList = new List<String>();
        private String curDescVal = "";
        private String _currentDescFied;
        public Dictionary<String, String> OOBDsStrings = null;
        public String oobdsstring = null;
        public IEnumerable<IFeatureAction> SelectedFeatureActions { get; private set; }
        public Boolean ShowIcon {
            get
            { 
                return _showIcon; 
            }
            set {
                _showIcon = value; 
            }
        }
        private Boolean _showIcon = false;
        readonly static IFeatureAction[] _configFeatureActions = { new ZoomToFeatureAction(), new PanToFeatureAction(), new FollowFeatureAction(), new HighlightFeatureAction(), new ShowPopupFeatureAction()};
        public OOBWidgetDialog(Dictionary<String, OOBDataSource> oobdatasources, string initialCaption,  IEnumerable<IFeatureAction>selectedFeatureActions,  OOBCache oobcache)
        {
            InitializeComponent();
            DataSourceSelector.IsEnabled = true;
            OKButton.IsEnabled = false;
            if (oobcache != null)
            {
                cache = oobcache;
            }
            if (initialCaption != null)
            {
                OOBName = initialCaption;
                tb_title.Text = OOBName;
            }
            if (oobdatasources.Count > 0)
            {
                InitializeDataSources(oobdatasources);
            }
            
            
            InitializeFeatureActions(selectedFeatureActions);
        }

        private void InitializeDataSources(Dictionary<String, OOBDataSource> oobdatasources)
        {
            _datasources = oobdatasources;
            foreach (KeyValuePair<String, OOBDataSource> p in oobdatasources)
            {
                listDs.Items.Add(p.Key);
            }
            tb_dsname.Text = "";
            tb_dsname.IsEnabled = true;
            listDs.SelectedIndex = -1;

        }

        private void lb_label_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_label.SelectedItem == null)
                return;
            String n = lb_label.SelectedItem.ToString();
            
            
            String lval = "{" + n + "} ";
            
            if (curLabelVal.Length > 0)
            {
                curLabelVal += " ";
            }
            curLabelVal += lval;
            tbLabel.Text = curLabelVal;
            if(!_currentLabelList.Contains(n))
            {
                _currentLabelList.Add(n);
            }
            if (listDs.SelectedIndex > -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    if (!ods.LabelFields.Contains(n))
                    {
                        ods.LabelFields.Add(n);
                    }
                    if (ods.UseIcon)
                    {
                        rb_Symbol.IsChecked = true;
                    }
                    if (ods.DescType != null)
                    {
                        setDescType(ods.DescType);
                    }
                }
            }
        }

        private void setDescType(DescriptionType dtype)
        {
            if (dtype == DescriptionType.None)
            {
                rb_descNone.IsChecked = true;
            }
            else if (dtype == DescriptionType.SingleField)
            {
                rb_descSinglefld.IsChecked = true;
            }
            else if (dtype == DescriptionType.Custom)
            {
                rb_descCustom.IsChecked = true;
            }
        }

        private void updateCurrentDatasource(object sender, RoutedEventArgs e)
        {
            if (listDs.SelectedIndex > -1)
            {
                if (!listDs.SelectedItem.ToString().Equals("UNITS"))
                {
                    btnRemoveDS.IsEnabled = true;
                }
                else
                {
                    btnRemoveDS.IsEnabled = false;
                }
                String selectedDs = listDs.SelectedItem.ToString();
                OOBDataSource ods = OOBDataSources[selectedDs];
                DataSource ds = ods.DataSource;
                DataSourceSelector.SelectedDataSource = ds;
                _currentDS = ds;

                foreach (client.Field f in ds.Fields)
                {
                    if (f.Name.Equals(ods.UIDField))
                    {
                        UIDComboBox.SelectedItem = f;
                    }
                    if (f.Name.Equals(ods.HFField))
                    {
                        HFComboBox.SelectedItem = f;
                    }
                }
                _currentLabelList = ods.LabelFields;
                _currentDescList = ods.DescriptionFields;
                rb_Symbol.IsChecked = ods.UseIcon;
                setDescType(ods.DescType);
                rtDesc.Text = ods.BaseDescription;
                tbLabel.Text = ods.BaseLabel;
                //tbLabel.Text = curLabelVal;
            }
            else
            {
                btnRemoveDS.IsEnabled = false;
            }
 
        }
        private void setDescriptionType(object sender, RoutedEventArgs e)
        {
            if ((Boolean)rb_descNone.IsChecked)
            {
                _currentDescType = DescriptionType.None;
                cbItemDesc.IsEnabled = false;
                singleFieldGrid.Visibility = System.Windows.Visibility.Collapsed;
                CustomDescGrid.Visibility = System.Windows.Visibility.Collapsed;
                rtDesc.IsEnabled = false;
                
            }
            else if ((Boolean)rb_descSinglefld.IsChecked)
            {
                _currentDescType = DescriptionType.SingleField;
                cbItemDesc.IsEnabled = true;
                singleFieldGrid.Visibility = System.Windows.Visibility.Visible;
                CustomDescGrid.Visibility = System.Windows.Visibility.Collapsed;
                rtDesc.IsEnabled = false;
            }
            else if ((Boolean)rb_descCustom.IsChecked)
            {
                _currentDescType = DescriptionType.Custom;
                singleFieldGrid.Visibility = System.Windows.Visibility.Collapsed;
                CustomDescGrid.Visibility = System.Windows.Visibility.Visible;
                cbItemDesc.IsEnabled = false;
                rtDesc.IsEnabled = true;
            }
            if (listDs.SelectedIndex > -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    ods.DescType = _currentDescType;
                }
            }
        }

        private void set_ShowIcon(object sender, RoutedEventArgs e)
        {
            if ((Boolean)rb_none.IsChecked)
            {
                _showIcon = false;
            }
            else if ((Boolean)rb_Symbol.IsChecked)
            {
                _showIcon = true;
            }
            if (listDs.SelectedIndex > -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    ods.UseIcon = _showIcon;
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            OOBDsStrings = new Dictionary<String, String>();
            oobdsstring = "";
            Boolean first = true;
            foreach(KeyValuePair<String, OOBDataSource> p in _datasources)
            {
                if (!first)
                {
                    oobdsstring += ";";
                }
                else
                {
                    first = false;
                }
                String s = p.Value.Serialize();
                oobdsstring += p.Value.Key + "%" + s;
                OOBDsStrings.Add(p.Value.ID, s);
            }
            OOBName = tb_title.Text;
            SelectedFeatureActions = FeatureActionList.SelectedFeatureActions;
            DialogResult = true;
        }
        private void InitializeFeatureActions(IEnumerable<IFeatureAction> selectedFeatureActions)
        {
            List<IFeatureAction> featureActionsToAdd = null;
            if (selectedFeatureActions != null)
            {
                //first add the selected feature actions
                featureActionsToAdd = new List<IFeatureAction>(selectedFeatureActions);

                //add the remaining non-selected feature actions
                featureActionsToAdd.AddRange(_configFeatureActions.Except(selectedFeatureActions, new FeatureActionComparer()));
            }
            else
                featureActionsToAdd = new List<IFeatureAction>(_configFeatureActions);

            FeatureActionList.FeatureActions = featureActionsToAdd;
            FeatureActionList.SelectedFeatureActions = selectedFeatureActions;
        }
        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                DataSource ds = DataSourceSelector.SelectedDataSource;
                _currentDS = ds;
                lb_label.Items.Clear();
                lb_desc.Items.Clear();
                foreach (client.Field f in ds.Fields)
                {
                    lb_label.Items.Add(f.Name);
                    lb_desc.Items.Add(f.Name);
                }

                ResetComboBoxes(ds, sender);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "/n" + ex.Source
                    + "/n" + ex.StackTrace);
            }
        }



        private void addDS(object sender, RoutedEventArgs e)
        {
            String key = tb_dsname.Text.ToUpper();
            DataSource ds = DataSourceSelector.SelectedDataSource;
            
            listDs.Items.Add(key);
            if (!tb_dsname.IsEnabled)
            {
                tb_dsname.IsEnabled = true;
            }
            tb_dsname.Clear();
            OOBDataSource ods = new OOBDataSource(ds, key);
            ods.UIDField = ((client.Field)UIDComboBox.SelectedItem).Name;
            ods.HFField = ((client.Field)HFComboBox.SelectedItem).Name;
            if (!(Boolean)rb_descNone.IsChecked)
            {
                ods.DescField = ((client.Field)cbItemDesc.SelectedItem).Name;
            }
            else
            {
                ods.DescField = "";
            }
            foreach (String l in _currentLabelList)
            {
                ods.LabelFields.Add(l);
            }
            foreach (String d in _currentDescList)
            {
                ods.DescriptionFields.Add(d);
            }
            ods.DescType = _currentDescType;
            ods.BaseDescription=_currentBaseDesc;
            ods.BaseLabel = curLabelVal;
            ods.UseIcon = _showIcon;
            _datasources.Add(ods.Key, ods);
            AddDatasourceToCache(ods);
            ResetComboBoxes(ds, sender);
            btnAddDS.IsEnabled = false;
        }

        private void RemoveDS(object sender, RoutedEventArgs e)
        {
            String key = listDs.SelectedItem.ToString();
            _datasources.Remove(key);
            listDs.Items.Remove(key); 
        }

        private void openLabelSelector(object sender, MouseButtonEventArgs e)
        {
            popLabel.IsOpen = true;
        }

        
        private void ResetComboBoxes(DataSource dataSource, object sender)
        {
            UIDComboBox.ItemsSource = dataSource.Fields;
            UIDComboBox.SelectedItem = dataSource.Fields[0];
            HFComboBox.ItemsSource = dataSource.Fields;
            HFComboBox.SelectedItem = dataSource.Fields[0];
            cbItemDesc.ItemsSource = dataSource.Fields;
            cbItemDesc.SelectedItem = dataSource.Fields[0];
            _currentDescList = new List<String>();
            _currentLabelList = new List<String>();
            //tbItemDesc.Text = "";
            tbLabel.Text = "";
            curLabelVal = "";
            rb_none.IsChecked = true;
            rb_descNone.IsChecked = true;
            List<ESRI.ArcGIS.Client.Field> numericFields = new List<ESRI.ArcGIS.Client.Field>();
            foreach (var field in dataSource.Fields)

                ValidateInput(sender, null);

        }




        private void ValidateInput(object sender, TextChangedEventArgs e)
        {
            if (tbLabel == null || tb_dsname == null || tb_title == null)
                return;
            if (listDs != null)
            {
                if (listDs.Items.Count > 0)
                {
                    OKButton.IsEnabled = true;
                }
                if (!String.IsNullOrEmpty(tb_dsname.Text) && !String.IsNullOrEmpty(tbLabel.Text) && !String.IsNullOrEmpty(tb_title.Text))
                {
                    btnAddDS.IsEnabled = true;
                }
            }
        }

        /*private void set_equipment(object sender, RoutedEventArgs e)
        {
            if ((Boolean)((CheckBox)sender).IsChecked)
            {
                DataSourceSelectorEquipment.IsEnabled = true;
                //EquipmentUIDComboBox.IsEnabled = true;
                //EquipmentHFComboBox.IsEnabled = true;
                //EquipmentLabelComboBox.IsEnabled = true;
            }
            else
            {
                if (DataSources.Count > 1)
                {
                    DataSources.Remove(DataSources[1]);
                }
                DataSourceSelectorEquipment.IsEnabled = false;
                //EquipmentUIDComboBox.IsEnabled = false;
                //EquipmentHFComboBox.IsEnabled = false;
                //EquipmentLabelComboBox.IsEnabled = false;
            }
        }*/

        private async void AddDatasourceToCache(OOBDataSource ods)
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
                Query q = new Query();



                q.Fields = qfields;
                cache.AddFeatuereContainer(CacheName);

                DataSource ds = ods.DataSource;
                q.ReturnGeometry = true;
                QueryResult res = await ds.ExecuteQueryAsync(q);
                var resultOids = from feature in res.Features select System.Convert.ToInt32(feature.Attributes[ds.ObjectIdFieldName]);
                MapWidget mw = MapWidget.FindMapWidget(ds);
                client.FeatureLayer fl = mw.FindFeatureLayer(ds);
                //fl.Update();
                foreach (client.Graphic g in fl.Graphics)
                {
                    cache.AddFeature(CacheName, g, ods.BaseDescription, ods.BaseLabel, fields);
                }
                ods.IsCacheCreated = true;
                
                //cache.AddFeature
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message + "/n" + e.Source
                    + "/n" + e.StackTrace);
            }
        }

        private void tb_dsname_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(tb_dsname.Text))
            {
                ValidateInput(sender, e);
            }
        }

        private void tb_title_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(tb_title.Text))
            {
                ValidateInput(sender, e);
            }
        }

        

        private void tbLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            curLabelVal  = tbLabel.Text;
            if (listDs.SelectedIndex> -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    ods.BaseLabel = curLabelVal;
                }
            }
            if (!String.IsNullOrEmpty(tbLabel.Text))
            {
                ValidateInput(sender, e);
            }
        }

        private void openDescSelector(object sender, MouseButtonEventArgs e)
        {
            popDesc.IsOpen = true;
        }

        void lb_desc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_desc.SelectedItem == null)
                return;

            String n = lb_desc.SelectedItem.ToString();
            String dval = "{" + n + "} ";
            if (_currentBaseDesc.Length > 0)
            {
                _currentBaseDesc += " ";
            }
            _currentBaseDesc += dval;
            rtDesc.Text = _currentBaseDesc;
            _currentDescList.Add(n);
            if (listDs.SelectedIndex > -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    if (!ods.DescriptionFields.Contains(n))
                    {
                        ods.DescriptionFields.Add(n);
                    }
                }
            }
        }

        private void rtDesc_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentBaseDesc = rtDesc.Text;
            if (listDs.SelectedIndex > -1)
            {
                OOBDataSource ods = OOBDataSources[listDs.SelectedItem.ToString()];
                if (ods != null)
                {
                    ods.BaseDescription = curLabelVal;
                }
            }
        }
       
        

        
    }
    // Helper class to compare feature actions based on their type.
    class FeatureActionComparer : IEqualityComparer<IFeatureAction>
    {
        public bool Equals(IFeatureAction x, IFeatureAction y)
        {
            return x.GetType() == y.GetType();
        }

        public int GetHashCode(IFeatureAction obj)
        {
            return obj.GetType().GetHashCode();
        }
    }
}
