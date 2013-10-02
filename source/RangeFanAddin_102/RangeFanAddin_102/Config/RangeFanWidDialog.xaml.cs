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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace RangeFanAddin_102.Config
{
    /// <summary>
    /// Interaction logic for RangeFanWidDialog.xaml
    /// </summary>
    public partial class RangeFanWidDialog : Window
    {

        public DataSource DataSource { get; private set; }
        public ESRI.ArcGIS.Client.Field Field { get; private set; }
        public string Caption { get; private set; }

        public RangeFanWidDialog(IList<DataSource> dataSources, string initialCaption, string initialDataSourceId, string initialField)
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
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource = DataSourceSelector.SelectedDataSource;
            Caption = CaptionTextBox.Text;
            Field = (ESRI.ArcGIS.Client.Field)FieldComboBox.SelectedItem;

            DialogResult = true;
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
}
