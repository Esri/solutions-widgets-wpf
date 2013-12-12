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
using System.Configuration;
using System.Collections.ObjectModel;

namespace FindClosestResource
{
    /// <summary>
    /// A MapTool is an extension to Operations Dashboard for ArcGIS which can be configured to appear in the toolbar 
    /// of the map widget, providing a custom map tool.
    /// </summary>
    /// 
    [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
    [ExportMetadata("DisplayName", "Find Closest Facility")]
    [ExportMetadata("Description", "Finds closest facilties from a given location.")]
    [ExportMetadata("ImagePath", "/FindClosestResource;component/Images/MapTool16.png")]
    [ExportMetadata("DataSourceRequired", true)]
   
    [DataContract]
    public partial class FindClosestResource : UserControl, IMapTool, IDataSourceConsumer
    {
        private FindClosestResourceToolbar _findClosestResourceToolbar = null;
  
        public ObservableCollection<DataSource> ResourcesDatasource {get; set;}
        
        [DataMember (Name="BarrierDataSourcesIds")]
        public List<String> PersistedBarriersDataSources {get; set;}

        [DataMember(Name = "ResourceDataSourcesIds")]
        public String PersistedResourceDataSource { get; set; }

        public ObservableCollection<DataSource> BarriersDataSources { get; set; }


        public FindClosestResource()
        {
            //var settings = ConfigurationManager.AppSettings; 
            InitializeComponent();
        }

        // Gets the configured data source.
        public DataSource DataSource
        {
            get
            {
                return OperationsDashboard.Instance.DataSources.FirstOrDefault((dataSource) => dataSource.Id == DataSourceIds[0]);
            }
        }

        // The name of a field within the selected data source. This property is set during widget configuration.
        [DataMember(Name = "ResourceTypeField")]
        public string ResourceTypeField { get; set; }


        #region IMapTool

        /// <summary>
        /// The MapWidget property is set by the MapWidget that hosts the map tools. The application ensures that this property is set when the
        /// map widget containing this map tool is initialized.
        /// </summary>
        public MapWidget MapWidget { get; set; }

        /// <summary>
        /// OnActivated is called when the map tool is added to the toolbar of the map widget in which it is configured to appear. 
        /// Called when the operational view is opened, and also after a custom toolbar is reverted to the configured toolbar,
        /// and during toolbar configuration.
        /// </summary>
        public void OnActivated()
        {
            InitializeResourceDataSource(); 
            InitializeBarrierDataSources();
        }

        /// <summary>
        ///  OnDeactivated is called before the map tool is removed from the toolbar. Called when the operational view is closed,
        ///  and also before a custom toolbar is installed, and during toolbar configuration.
        /// </summary>
        public void OnDeactivated()
        {
        }

        /// <summary>
        ///  Determines if a Configure button is shown for the map tool.
        ///  Provides an opportunity to gather user-defined settings.
        /// </summary>
        /// <value>True if the Configure button should be shown, otherwise false.</value>
        public bool CanConfigure
        {
            get { return true; }
        }

        /// <summary>
        ///  Provides functionality for the map tool to be configured by the end user through a dialog.
        ///  Called when the user clicks the Configure button next to the map tool.
        /// </summary>
        /// <param name="owner">The application window which should be the owner of the dialog.</param>
        /// <returns>True if the user clicks ok, otherwise false.</returns>
        public bool Configure(System.Windows.Window owner)
        {
            // Implement this method if CanConfigure returned true.
            //throw new NotImplementedException();
             IList<DataSource> dataSources = new List<DataSource>{};

             var dialog = new Config.FindClosestResourceDialog(dataSources, DataSourceIds != null ? DataSourceIds[0] : null, ResourceTypeField, BarriersDataSources) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            DataSourceIds = new string[] { dialog.DataSource.Id };
            ResourceTypeField = dialog.Field.Name;
            ResourcesDatasource = new ObservableCollection<DataSource>();
            ResourcesDatasource.Add(dialog.DataSource);

            BarriersDataSources = new ObservableCollection<DataSource>();
            foreach(DataSource dt in dialog.BarrierDataSources)
            {
                BarriersDataSources.Add(dt); 
            }

            InitializePersistedResources();
            InitializePersistedBarriers(); 

            return true;
        }
        #endregion


        private void InitializeResourceDataSource()
        {
            // Check the persisted information.
            ResourcesDatasource = null;
            if (PersistedResourceDataSource == null)
                return;

            // For each feature action saved, create the appropriate feature action class and set any properties.
            ObservableCollection<DataSource> resources = new ObservableCollection<DataSource>();
            var persistedResource = OperationsDashboard.Instance.DataSources.FirstOrDefault((dataSource) => dataSource.Id == PersistedResourceDataSource);
            resources.Add(persistedResource);
            ResourcesDatasource = resources;
        }

        // Initializes the PersistedFeatureActions property used to persist the feature actions selected in the config dialog.
        private void InitializePersistedResources()
        {
            // Clear any current information
            PersistedResourceDataSource = null;

            if (ResourcesDatasource == null)
                return;

            PersistedResourceDataSource = ResourcesDatasource[0].Id; 
        }

        // Set up feature actions from persisted data.
        private void InitializeBarrierDataSources()
        {
            // Check the persisted information.
            BarriersDataSources = null;
            if (PersistedBarriersDataSources == null)
                return;

            // For each feature action saved, create the appropriate feature action class and set any properties.
            ObservableCollection<DataSource> barriers = new ObservableCollection<DataSource>();
            foreach (var persistedBarrierId in PersistedBarriersDataSources)
            {
                var persistedBarrier = OperationsDashboard.Instance.DataSources.FirstOrDefault((dataSource) => dataSource.Id == persistedBarrierId);
                barriers.Add(persistedBarrier);
            }
            BarriersDataSources = barriers;
        }

        // Initializes the PersistedFeatureActions property used to persist the feature actions selected in the config dialog.
        private void InitializePersistedBarriers()
        {
            // Clear any current information
            PersistedBarriersDataSources = null;

            if (BarriersDataSources == null)
                return;

            // For each feature action object, create persistence helper and set any properties.
            List<String> persistedBarriersIds = new List<String>();
            foreach (var barrier in BarriersDataSources)
            {
                persistedBarriersIds.Add(barrier.Id);
            }

            PersistedBarriersDataSources = persistedBarriersIds;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // When the user clicks the map tool button, begin installing zoom code.
            // Ensure the tool has a valid map to work with.
            if (ResourcesDatasource == null)
                return;
            
            if (MapWidget != null && MapWidget.Map != null && ResourcesDatasource != null && ResourceTypeField != null && BarriersDataSources != null)
            {
                // Provide a way for the user to cancel the operation, by installing a temporary toolbar.
                // This also prevents other tools from being used in the meantime.
                _findClosestResourceToolbar = new FindClosestResourceToolbar(MapWidget, ResourcesDatasource, ResourceTypeField, BarriersDataSources);
                MapWidget.SetToolbar(_findClosestResourceToolbar);
            }
            else
            {
                MessageBox.Show("Please configure this tool first!", "Error");
                return;
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
