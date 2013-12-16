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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace ERGChemicalAddIn
{

    /// <summary>
    /// A MapTool is an extension to Operations Dashboard for ArcGIS which can be configured to appear in the toolbar 
    /// of the map widget, providing a custom map tool.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
    [ExportMetadata("DisplayName", "ERG Chemical")]
    [ExportMetadata("Description", "ERGChemicalAddIn ERGChemicalMapTool description")]
    [ExportMetadata("ImagePath", "/ERGChemicalAddIn;component/Images/HAZMAT_Icon16.png")]
    [DataContract]
    public partial class ERGChemicalMapTool : UserControl, IMapTool, IDataSourceConsumer
    {
        ERGChemicalMapToolbar _ergChemicalToolbar = null;
       
        //persisted members 
        private string _ergChemicalURL = "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/ERG/ERGByChemical/GPServer/ERG%20By%20Chemical";

        [DataMember(Name = "ERGChemicalURL")]
        public string ERGChemicalURL
        {
            get
            {
                return _ergChemicalURL;
            }

            set
            {
                if (value != _ergChemicalURL)
                {
                    _ergChemicalURL = value;
                }
            }
        }

        private string _ergPlacardURL = "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/ERG/ERGByPlacard/GPServer/ERG%20By%20Placard";

        [DataMember(Name = "ERGPlacardURL")]
        public string ERGPlacardURL
        {
            get
            {
                return _ergPlacardURL;
            }

            set
            {
                if (value != _ergPlacardURL)
                {
                    _ergPlacardURL = value;
                }
            }
        }

        private string _findNearestWSURL = "http://arcgis-emergencymanagement-2057568539.us-east-1.elb.amazonaws.com/arcgis/rest/services/ERG/FindNearestWS/GPServer/FindNearestWeatherStation";

        [DataMember(Name = "FindNearestWSURL")]
        public string FindNearestWSURL
        {
            get
            {
                return _findNearestWSURL;
            }

            set
            {
                if (value != _findNearestWSURL)
                {
                    _findNearestWSURL = value;
                }
            }
        }

        //weather station layer and field names
        [DataMember(Name = "WeatherStationLayerURL")]
        public string WindDirectionLayerDataSource {get;set;}
       
        [DataMember(Name = "windDirectionFieldName")]
        public string WindDirectionFieldName { get; set; }

        [DataMember(Name = "windWeatherStationFieldName")]
        public string WindWeatherStationFieldName { get; set; }

        [DataMember(Name = "windRecordedDateFieldName")]
        public string WindRecordedDateFieldName { get; set; }

        [DataMember(Name = "DefaultERGChemicalName")]
        public string DefaultERGChemicalName { get; set; }

        [DataMember(Name = "DefaultPlacardName")]
        public string DefaultPlacardName { get; set; }



        public ERGChemicalMapTool()
        {
            InitializeComponent();
        }

        #region IMapTool

        public MapWidget MapWidget { get; set; }

        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
        }

        public bool CanConfigure
        {
            get { return true; }
        }

        public bool Configure(System.Windows.Window owner)
        {
            var configureDialog = new Config.ERGChemicalConfigDialog(ERGChemicalURL, ERGPlacardURL,
                WindDirectionLayerDataSource, FindNearestWSURL, WindDirectionFieldName, WindWeatherStationFieldName, WindRecordedDateFieldName, DefaultERGChemicalName, DefaultPlacardName) { Owner = owner };
            if (configureDialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            //configurable attributes
            ERGChemicalURL = configureDialog.ERGChemicalURL;
            ERGPlacardURL = configureDialog.ERGPlacardURL;
            FindNearestWSURL = configureDialog.FindNearestWSURL;
           
            WindDirectionLayerDataSource = configureDialog.WindDirectionLayerDataSource; 
            WindDirectionFieldName = configureDialog.WindDirectionFieldName; 
            WindWeatherStationFieldName = configureDialog.WindStationNameFieldName;
            WindRecordedDateFieldName = configureDialog.WindDateFieldName;

            DefaultERGChemicalName = configureDialog.DefaultChemicalName;
            DefaultPlacardName = configureDialog.DefaultPlacardName;

            return true;
        }

        #endregion
      
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Provide map tool behaviour, for example, add mouse handling to the Map, 
            // or install a custom toolbar using MapWidget.SetToolbar

            if ((MapWidget != null) && (MapWidget.Map != null))
            {
                _ergChemicalToolbar = new ERGChemicalMapToolbar(MapWidget, ERGChemicalURL, ERGPlacardURL, FindNearestWSURL,
                    WindDirectionLayerDataSource, WindDirectionFieldName, WindWeatherStationFieldName, WindRecordedDateFieldName, DefaultERGChemicalName, DefaultPlacardName);
                
                MapWidget.SetToolbar(_ergChemicalToolbar);
            }
            // Set the Checked property of the ToggleButton to false after work is complete.
            ToggleButton.IsChecked = false;
        }

        #region IDataSourceConsumer Members

        [DataMember(Name = "dataSourceIds")]
        public string[] DataSourceIds { get; set; }

        async void IDataSourceConsumer.OnRefresh(DataSource dataSource)
        {
            var result = await dataSource.ExecuteQueryAsync(new Query());
            if (result == null || result.Features == null)
                return;
        }

        void IDataSourceConsumer.OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            DataSourceIds = null;
        }
        #endregion
    }
}
