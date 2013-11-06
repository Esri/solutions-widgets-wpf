using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ESRI.ArcGIS.OperationsDashboard;
using ESRI.ArcGIS.OperationsDashboard.Controls;
using client = ESRI.ArcGIS.Client;

namespace ERGChemicalAddIn.Config
{
    /// <summary>
    /// Interaction logic for ERGChemicalConfigDialog.xaml
    /// </summary>
    public partial class ERGChemicalConfigDialog : Window
    {
        //GP Service URLs
        public string ERGChemicalURL { get; private set; }
        public string ERGPlacardURL { get; private set; }
        public string FindNearestWSURL { get; private set; }

        //wind directions lookup data - must be exist in a webmap
        public string WindDirectionLayerDataSource { get; private set; }
        public string WindDirectionFieldName { get; private set; }
        public string WindStationNameFieldName { get; private set; }
        public string WindDateFieldName { get; private set; }

        private string _selectedDataSourceName;

        public ERGChemicalConfigDialog(string ERGChemicalURL, string ERGPlacardURL, string WindDirectionLayerDataSource, 
            string FindNearestWSURL, string windDirectionFieldName, string windWeatherStationFieldName, string windRecordedDateFieldName)
        {
            InitializeComponent();
            this.DataContext = this;

            // When re-configuring, initialize the widget config dialog from the existing settings.
            txtERGChemicalURL.Text = ERGChemicalURL;
            txtERGPlacardURL.Text = ERGPlacardURL;
            txtFindNearestWSURL.Text = FindNearestWSURL;

            if (!string.IsNullOrEmpty(windDirectionFieldName))
                WindDirectionFieldName = windDirectionFieldName;

            if (!string.IsNullOrEmpty(windWeatherStationFieldName))
                WindStationNameFieldName = windWeatherStationFieldName;

            if (!string.IsNullOrEmpty(windRecordedDateFieldName))
                WindDateFieldName = windRecordedDateFieldName;

            if (!string.IsNullOrEmpty(WindDirectionLayerDataSource))
                InitializeDataSource(WindDirectionLayerDataSource);
        }

        private void InitializeDataSource(string initialDataSourceId)
        {
            DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
            if (dataSource != null)
            {
                WindDirection_DataSourceSelector.SelectedDataSource = dataSource;
                WindDirectionLayerDataSource = dataSource.Name;
                
                setUpQueryFields(dataSource);
            }
        }

        private void WindDataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
                DataSource dataSource = WindDirection_DataSourceSelector.SelectedDataSource;
                _selectedDataSourceName = dataSource.Name;

                setUpQueryFields(dataSource);
        }

        private void setUpQueryFields(DataSource dataSource)
        {
            //wind direction 
            client.Field windDirField = dataSource.Fields.FirstOrDefault(fld => fld.Name == WindDirectionFieldName);
            WindDirectionFieldList.ItemsSource = dataSource.Fields;
         
            if (windDirField != null)
                WindDirectionFieldList.SelectedItem = windDirField;
            else
                WindDirectionFieldList.SelectedItem = dataSource.Fields[1];

            //station name 
            client.Field windStationFld = dataSource.Fields.FirstOrDefault(fld => fld.Name == WindStationNameFieldName);
            stationFieldList.ItemsSource = dataSource.Fields;

            if (windStationFld != null)
                stationFieldList.SelectedItem = windStationFld;
            else
                stationFieldList.SelectedItem = dataSource.Fields[1];

            //Recorded Date Field
            client.Field recordedDateFld = dataSource.Fields.FirstOrDefault(fld => fld.Name == WindDateFieldName);
            dateFieldList.ItemsSource = dataSource.Fields;

            if (recordedDateFld != null)
                dateFieldList.SelectedItem = recordedDateFld;
            else
                dateFieldList.SelectedItem = dataSource.Fields[1];
        }

        // ***********************************************************************************
        // * User clicked on OK button. Collect the parameters and set their values to be passed
        // * to the tool. 
        // ***********************************************************************************
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ERGChemicalURL = txtERGChemicalURL.Text;
            ERGPlacardURL = txtERGPlacardURL.Text;
            FindNearestWSURL = txtFindNearestWSURL.Text;

            if (WindDirection_DataSourceSelector.IsEnabled)
            {
                if (_selectedDataSourceName != null && _selectedDataSourceName != "")
                {
                    WindDirectionLayerDataSource = _selectedDataSourceName;
                    WindDirectionFieldName = ((ESRI.ArcGIS.Client.Field)WindDirectionFieldList.SelectedItem).Name;
                    WindStationNameFieldName = ((ESRI.ArcGIS.Client.Field)stationFieldList.SelectedItem).Name;
                    WindDateFieldName = ((ESRI.ArcGIS.Client.Field)dateFieldList.SelectedItem).Name;
                }
            }

            DialogResult = true;
        }
    }
}
