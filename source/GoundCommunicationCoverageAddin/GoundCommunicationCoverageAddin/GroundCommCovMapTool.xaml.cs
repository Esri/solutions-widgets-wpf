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

namespace GoundCommunicationCoverageAddin
{
    /// <summary>
    /// A MapTool is an extension to Operations Dashboard for ArcGIS which can be configured to appear in the toolbar 
    /// of the map widget, providing a custom map tool.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
    [ExportMetadata("DisplayName", "Ground Communication Coverage Map Tool")]
    [ExportMetadata("Description", "Calculates the ground coverage for a forward operating base")]
    [ExportMetadata("ImagePath", "/GoundCommunicationCoverageAddin;component/Images/Receiver.png")]
    [DataContract]
    public partial class GroundCommCovMapTool : UserControl, IMapTool
    {
        private GroundCommCovToolbar _gccToolbar = null;

        [DataMember(Name = "serviceUrl")]
        public string ServiceURL { get; set; }

        public GroundCommCovMapTool()
        {
            InitializeComponent();
            ServiceURL = @"http://ec2-107-20-210-202.compute-1.amazonaws.com:6080/arcgis/rest/services/STKServer/GroundCommunicationCoverage_Power/GPServer/GroundCommunicationCoverage";
        }

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
            Config.ConfigDialog dialog = new Config.ConfigDialog();
            if (dialog.ShowDialog() != true)
                return false;

            ServiceURL = dialog.ServiceTextBox.Text;
            return true;
        }

        #endregion

        /// <summary>
        /// Provides the behaviour when the user clicks the map tool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ((MapWidget != null) && (MapWidget.Map != null))
            {
                _gccToolbar = new GroundCommCovToolbar(MapWidget, ServiceURL);
                MapWidget.SetToolbar(_gccToolbar);
            }
        }

    }
}
