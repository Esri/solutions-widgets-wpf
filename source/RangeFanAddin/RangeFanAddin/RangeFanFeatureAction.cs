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
using System.ComponentModel.Composition;
using System.Windows.Media;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace RangeFanAddin
{
    /// <summary>
    /// A FeatureAction is an extension to Operations Dashboard for ArcGIS which can be shown when a user right-clicks on
    /// a feature in a widget.
    /// </summary>
    [Export("ESRI.ArcGIS.OperationsDashboard.FeatureAction")]
    [ExportMetadata("DisplayName", "Create Range Fan")]
    [ExportMetadata("Description", "Creates a range fan for a feature")]
    [ExportMetadata("ImagePath", "/RangeFanAddin;component/Images/RangeFan-16x.png")]
    public class RangeFanFeatureAction : IFeatureAction
    {

        public RangeFanFeatureAction()
        {

        }

        #region IFeatureAction

        /// <summary>
        ///  Determines if a Configure button is shown for the feature action.
        ///  Provides an opportunity to gather user-defined settings.
        /// </summary>
        /// <value>True if the Configure button should be shown, otherwise false.</value>
        public bool CanConfigure
        {
            get { return false; }
        }

        /// <summary>
        ///  Provides functionality for the feature action to be configured by the end user through a dialog.
        ///  Called when the user clicks the Configure button next to the feature action.
        /// </summary>
        /// <param name="owner">The application window which should be the owner of the dialog.</param>
        /// <returns>True if the user clicks ok, otherwise false.</returns>
        public bool Configure(System.Windows.Window owner)
        {
            // Implement this method if CanConfigure returned true.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines if the feature action can be executed based on the specified data source and feature, before the option to execute
        /// the feature action is displayed to the user.
        /// </summary>
        /// <param name="dataSource">The data source which will be subsequently passed to the Execute method if CanExecute returns true.</param>
        /// <param name="feature">The data source which will be subsequently passed to the Execute method if CanExecute returns true.</param>
        /// <returns>True if the feature action should be enabled, otherwise false.</returns>
        public bool CanExecute(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource, client.Graphic feature)
        {
            // Check if the data source and feature can be used with this feature action.

            // For example, identify if a MapWidget is ultimately providing the data source, and enable the feature action if that 
            // MapWidget also has its Show Popups setting enabled.
            return true;
        }

        /// <summary>
        /// Execute is called when the user chooses the feature action from the feature actions context menu. Only called if
        /// CanExecute returned true.
        /// </summary>
        public void Execute(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource, client.Graphic feature)
        {
            try
            {
                // For example, in the MapWidget that ultimately provides the data source, show the popup window for the feature.
                MapWidget mw = MapWidget.FindMapWidget(dataSource);
                foreach (var widget in OperationsDashboard.Instance.Widgets)
                    if (widget is RangeFanWid)
                    {
                        RangeFanWid pWidget = (RangeFanWid)widget;
                        pWidget.addToList(feature);
                        return;
                    }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        #endregion

    }
}
