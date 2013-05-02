using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace RangeFanAddinUpdate.Config
{
    /// <summary>
    /// Interaction logic for RangeFanWidgetDialog.xaml
    /// </summary>
    public partial class RangeFanWidgetDialog : Window
    {

        public DataSource DataSource { get; private set; }
        public ESRI.ArcGIS.Client.Field Field { get; private set; }
        public string Caption { get; private set; }
        public string Bearing { get; private set; }
        public string Traversal { get; private set; }
        public string Range { get; private set; }
        public string MapWidgetId { get; private set; }
        public IEnumerable<IFeatureAction> SelectedFeatureActions { get; private set; }

        readonly static IFeatureAction[] _configFeatureActions = { new FollowFeatureAction(), new ShowPopupFeatureAction(), new ZoomToFeatureAction(), new HighlightFeatureAction(), new RemoveFanFA() };

        public RangeFanWidgetDialog(IList<DataSource> dataSources, string initialCaption, string initialDataSourceId, string initialField, IEnumerable<IFeatureAction> selectedFeatureActions, string currentMapWidgetId)
        {
            InitializeComponent();

            // When re-configuring, initialize the widget config dialog from the existing settings.
            CaptionTextBox.Text = initialCaption;
            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                if (dataSource != null)
                {
                    DataSourceSelector.SelectedDataSource = dataSource;
                    if (!string.IsNullOrEmpty(initialField))
                    {
                        client.Field field = dataSource.Fields.FirstOrDefault(fld => fld.FieldName == initialField);
                        FieldComboBox.SelectedItem = field;
                    }
                }
            }
            //Initialize the DataSourceSelector and the filed combo box
            InitializeDataSource(initialDataSourceId, initialField);

            //Initialize the FeatureActionsList with the feature actions
            InitializeFeatureActions(selectedFeatureActions);

            // Retrieve a list of all map widgets from the application and bind this to a combo box 
            // for the user to select a map from.
            IEnumerable<ESRI.ArcGIS.OperationsDashboard.IWidget> mapws = OperationsDashboard.Instance.Widgets.Where(w => w is MapWidget);
            MapWidgetComboBox.ItemsSource = mapws;

            // Disable the combo box if no map widgets found.
            if (mapws.Count() < 1)
                MapWidgetComboBox.IsEnabled = false;
            else
            {
                // If an existing MapWidgetId is already set, select this in the list. If not set, then select the first in the list.
                MapWidget currentWidget = OperationsDashboard.Instance.Widgets.FirstOrDefault(widget => widget.Id == currentMapWidgetId) as MapWidget;
                if (currentWidget == null)
                    MapWidgetComboBox.SelectedItem = mapws.First();
                else
                    MapWidgetComboBox.SelectedItem = currentWidget;
            }

            ValidateInput();

        }

        private void ValidateInput()
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;
            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            if (MapWidgetComboBox.SelectedItem == null)
                return;

            OKButton.IsEnabled = true;
        }

        private void MapWidgetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateInput();
        }

        private void InitializeDataSource(string initialDataSourceId, string initialField)
        {
            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                if (dataSource != null)
                {
                    DataSourceSelector.SelectedDataSource = dataSource;
                    if (!string.IsNullOrEmpty(initialField))
                    {
                        client.Field field = dataSource.Fields.FirstOrDefault(fld => fld.FieldName == initialField);
                        //FieldComboBox.SelectedItem = field;
                    }
                }
            }
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource = DataSourceSelector.SelectedDataSource;
            Caption = CaptionTextBox.Text;
            Field = (ESRI.ArcGIS.Client.Field)FieldComboBox.SelectedItem;
            Bearing = BearingTextBox.Text;
            Range = RangeTextBox.Text;
            Traversal = TraversalTextBox.Text;
            SelectedFeatureActions = FeatureActionList.SelectedFeatureActions;

            // If there is a map widget selected, get the ID.
            if (MapWidgetComboBox.SelectedItem != null)
                MapWidgetId = ((IWidget)MapWidgetComboBox.SelectedItem).Id;

            if (MapWidgetComboBox.IsEnabled && (MapWidgetComboBox.SelectedItem != null))
            {
                // If there is any problem with the map widget ID, report that configuration has failed.
                if (string.IsNullOrEmpty(MapWidgetId))
                    DialogResult = false;
                else
                    DialogResult = true;
            }
            else
                DialogResult = false;

        }

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = DataSourceSelector.SelectedDataSource;
            FieldComboBox.ItemsSource = dataSource.Fields;
            FieldComboBox.SelectedItem = dataSource.Fields[0];
            List<ESRI.ArcGIS.Client.Field> numericFields = new List<ESRI.ArcGIS.Client.Field>();
            foreach (var field in dataSource.Fields)

                ValidateInput(sender, null);
        }

        private void ValidateInput(object sender, TextChangedEventArgs e)
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;
            if (string.IsNullOrEmpty(CaptionTextBox.Text))
                return;

            OKButton.IsEnabled = true;
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
