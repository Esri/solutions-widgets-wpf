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

namespace BufferMapTool
{
    /// <summary>
    /// A MapTool is an extension to Operations Dashboard for ArcGIS which can be configured to appear in the toolbar 
    /// of the map widget, providing a custom map tool.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
    [ExportMetadata("DisplayName", "Buffer Map Tool")]
    [ExportMetadata("Description", "Buffers selected points on the map")]
    [ExportMetadata("ImagePath", "/BufferMapTool;component/Images/MapTool16.png")]
    
    [DataContract]
    public partial class BufferMapTool : UserControl, IMapTool, IDataSourceConsumer
    {

        // Gets the configured data source.
        public DataSource DataSource
        {
            get
            {
                return OperationsDashboard.Instance.DataSources.FirstOrDefault((dataSource) => dataSource.Id == DataSourceIds[0]);
            }
        }


        // The name of a field within the selected data source. This property is set during widget configuration.
        [DataMember(Name = "field")]
        public string Field { get; set; }

        [DataMember(Name = "bufferDataSource")]
        public string BufferDataSource { get; set; }

        public BufferMapTool()
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
            // Show the configuration dialog.
            Config.BufferMapToolConfig dialog = new Config.BufferMapToolConfig(DataSourceIds != null ? DataSourceIds[0] : null, Field) { Owner = owner };
            if (dialog.ShowDialog() != true)
                return false;

            // Retrieve the selected values for the properties from the configuration dialog.
            DataSourceIds = new string[] { dialog.DataSource.Id };
            BufferDataSource = dialog.DataSource.Id; 
            Field = dialog.Field.Name;

            return true;
        }

        #endregion

     
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MapWidget.SetToolbar(new BufferMapToolBar(MapWidget, BufferDataSource, Field));

            // Set the Checked property of the ToggleButton to false after work is complete.
            ToggleButton.IsChecked = false;
        }

        #region IDataSourceConsumer Members

        [DataMember(Name = "dataSourceIds")]
        public string[] DataSourceIds { get; set; }

        public void OnRemove(DataSource dataSource)
        {
            // Respond to data source being removed.
            DataSourceIds = null;
        }

        public async void OnRefresh(DataSource dataSource)
        {
            var result = await dataSource.ExecuteQueryAsync(new Query());
            if (result == null || result.Features == null)
                return;

        }

        #endregion

    }
}
