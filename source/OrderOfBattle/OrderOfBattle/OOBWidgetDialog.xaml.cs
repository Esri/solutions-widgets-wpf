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
        private String curDescVal = "";
        private DataSource _currentDS;
        private Grid _currentGrid;
        private List<String> _currentLabelList = new List<String>();
        private List<String> _currentDescList = new List<String>();
        public Dictionary<String, String> OOBDsStrings = null;
        public String oobdsstring = null;
        public IEnumerable<IFeatureAction> SelectedFeatureActions { get; private set; }
        readonly static IFeatureAction[] _configFeatureActions = { new ZoomToFeatureAction(), new PanToFeatureAction(), new FollowFeatureAction(), new HighlightFeatureAction()};
        public OOBWidgetDialog(Dictionary<String, OOBDataSource> oobdatasources, string initialCaption,  IEnumerable<IFeatureAction>selectedFeatureActions)
        {
            InitializeComponent();
            DataSourceSelector.IsEnabled = true;
            OKButton.IsEnabled = false;
            if (initialCaption != null)
            {
                OOBName = initialCaption;
                tb_title.Text = OOBName;
            }
            if (oobdatasources != null)
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
        }
        void lb_desc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_desc.SelectedItem == null)
                return;
            String n = lb_desc.SelectedItem.ToString();
            String dval = "{" + n + "} ";
            tbItemDesc.Text = curDescVal += dval;
            _currentDescList.Add(n);
        }

        private void lb_label_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lb_label.SelectedItem == null)
                return;
            String n = lb_label.SelectedItem.ToString();
            String lval = "{" + n + "} ";
            tbLabel.Text = curLabelVal += lval;
            _currentLabelList.Add(n);
        }

        private void updateCurrentDatasource(object sender, RoutedEventArgs e)
        {
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
            
            Boolean first = true;
            curLabelVal = "";
            foreach (String l in _currentLabelList)
            {
                if (!first)
                {
                    curLabelVal += " ";
                }
                else
                {
                    first = false;
                }
                curLabelVal += "{" + l + "}";
            }
            tbLabel.Text = curLabelVal;
            _currentDescList = ods.DescriptionFields;
            curDescVal = "";
            first = true;
            foreach (String l in _currentDescList)
            {
                if (!first)
                {
                    curDescVal += " ";
                }
                else
                {
                    first = false;
                }
                curDescVal += "{" + l + "}";
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
                OOBName = tb_title.Text;
            }
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



        private void addDS(object sender, EventArgs e)
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
            foreach (String l in _currentLabelList)
            {
                ods.LabelFields.Add(l);
            }
            foreach (String l in _currentDescList)
            {
                ods.DescriptionFields.Add(l);
            }

            _datasources.Add(ods.Key, ods);
            AddDatasourceToCache(ods);
            ResetComboBoxes(ds, sender);
            btnAddDS.IsEnabled = false;
        }
        private void openLabelSelector(object sender, MouseButtonEventArgs e)
        {
            popLabel.IsOpen = true;
        }

        

        private void openDescSelector(object sender, MouseButtonEventArgs e)
        {
            popDesc.IsOpen = true;
        }


        private void ResetComboBoxes(DataSource dataSource, object sender)
        {
            UIDComboBox.ItemsSource = dataSource.Fields;
            UIDComboBox.SelectedItem = dataSource.Fields[0];
            HFComboBox.ItemsSource = dataSource.Fields;
            HFComboBox.SelectedItem = dataSource.Fields[0];
            _currentDescList = new List<String>();
            _currentLabelList = new List<String>();
            tbItemDesc.Text = "";
            tbLabel.Text = "";
            curDescVal = "";
            curLabelVal = "";
            List<ESRI.ArcGIS.Client.Field> numericFields = new List<ESRI.ArcGIS.Client.Field>();
            foreach (var field in dataSource.Fields)

                ValidateInput(sender, null);

        }




        private void ValidateInput(object sender, TextChangedEventArgs e)
        {
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
                /*String Uid = ((client.Field)UIDComboBox.SelectedItem).Name;
                String HF = ((client.Field)HFComboBox.SelectedItem).Name;
                //String Name = ((client.Field)LabelComboBox.SelectedItem).Name;

                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = Uid;
                fields["HF"] = HF;
                fields["NAME"] = Name;
                client.Tasks.OutFields outFlds = new client.Tasks.OutFields();
                outFlds.Add(((client.Field)UIDComboBox.SelectedItem).Name);
                if (!outFlds.Contains(HF))
                {
                    outFlds.Add(HF);
                }
                if (!outFlds.Contains(Name))
                {
                    outFlds.Add(Name);
                }
                //client.Tasks.QueryTask qt = new client.Tasks.QueryTask(fl.Url);
                //client.Tasks.Query q = new client.Tasks.Query();
                //q.OutFields = outFlds;
                Query q = new Query();
                q.Fields = new string[] { Uid, HF, Name };
                cache.AddFeatuereContainer(key);

                QueryResult res = await ds.ExecuteQueryAsync(q);
                //client.Tasks.FeatureSet fset = qt.Execute(q);
                var resultOids = from feature in res.Features select System.Convert.ToInt32(feature.Attributes[ds.ObjectIdFieldName]);
                MapWidget mw = MapWidget.FindMapWidget(ds);
                client.FeatureLayer fl = mw.FindFeatureLayer(ds);
                foreach (client.Graphic g in fl.Graphics)
                {
                    cache.AddFeature(key, g, fields);
                }*/
                String Uid = ods.UIDField;
                String HF = ods.HFField;
                String Labels = "";
                String Description = "";
                String CacheName = ods.Key;
                Dictionary<String, String> fields = new Dictionary<String, String>();
                fields["UID"] = Uid;
                fields["HF"] = HF;
                Boolean first = true;
                Int32 arraySize = ods.LabelFields.Count + ods.DescriptionFields.Count + 2;
                string[] qfields = new string[arraySize];
                qfields[0] = Uid;
                qfields[1] = HF;
                Int32 c = 2;
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
                        Description += ",";
                    }
                    else
                    {
                        first = false;
                    }
                    Description += f;
                    qfields[c] = f;
                    ++c;
                }
                fields["LABELS"] = Labels;
                fields["DESCRIPTION"] = Description;
                Query q = new Query();



                q.Fields = qfields;
                cache.AddFeatuereContainer(CacheName);

                DataSource ds = ods.DataSource;
                QueryResult res = await ds.ExecuteQueryAsync(q);
                var resultOids = from feature in res.Features select System.Convert.ToInt32(feature.Attributes[ds.ObjectIdFieldName]);
                MapWidget mw = MapWidget.FindMapWidget(ds);
                client.FeatureLayer fl = mw.FindFeatureLayer(ds);
                //fl.Update();
                foreach (client.Graphic g in fl.Graphics)
                {
                    cache.AddFeature(CacheName, g, fields);
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
            if (!String.IsNullOrEmpty(tbLabel.Text))
            {
                ValidateInput(sender, e);
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
