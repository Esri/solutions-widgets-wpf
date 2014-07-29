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
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace BombThreatAddin
{
    /// <summary>
    /// A MapTool is an extension to Operations Dashboard for ArcGIS which can be configured to appear in the toolbar 
    /// of the map widget, providing a custom map tool.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
    [ExportMetadata("DisplayName", "Bomb Threat Map Tool")]
    [ExportMetadata("Description", "Show evacuation areas for bomb threat stand-off distances")]
    [ExportMetadata("ImagePath", "/BombThreatAddin;component/Images/bomb16.png")]
    [DataContract]
    public partial class BombThreatMapTool : UserControl, IMapTool
    {
        BombThreatToolbar _bombToolbar = null;

        [DataMember(Name = "serviceURL")]
        public string ServiceURL { get; set; }
        public BombThreatMapTool()
        {
            InitializeComponent();
            ServiceURL = "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer";
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
            Config.ConfigWin dialog = new Config.ConfigWin();
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
            // Provide map tool behaviour, for example, add mouse handling to the Map, 
            // or install a custom toolbar using MapWidget.SetToolbar
            // MapWidget.SetToolbar(new MyMapToolbar(MapWidget));

            // Set the Checked property of the ToggleButton to false after work is complete.
            if ((MapWidget != null) && (MapWidget.Map != null))
            {
                _bombToolbar = new BombThreatToolbar(MapWidget, ServiceURL);
                MapWidget.SetToolbar(_bombToolbar);
            }
        }

    }
}
