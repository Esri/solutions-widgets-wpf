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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace BufferMapTool.Config
{
    /// <summary>
    /// Interaction logic for BufferMapToolConfig.xaml
    /// </summary>
    public partial class BufferMapToolConfig : Window
    {
        public DataSource DataSource { get; private set; }
        public ESRI.ArcGIS.Client.Field Field { get; private set; }

        public BufferMapToolConfig(string initialDataSourceId, string initialField)
        {
            InitializeComponent();

            //Initialize the DataSourceSelector and the field combo box
            InitializeDataSource(initialDataSourceId, initialField);
        }


        private void InitializeDataSource(string initialDataSourceId, string initialField)
        {
            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                if (dataSource != null)
                {
                    BufferLayer_DataSourceSelector.SelectedDataSource = dataSource;
                    if (!string.IsNullOrEmpty(initialField))
                    {
                        client.Field field = dataSource.Fields.FirstOrDefault(fld => fld.FieldName == initialField);
                        FieldComboBox.SelectedItem = field;
                    }
                }
            }
        }


        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = BufferLayer_DataSourceSelector.SelectedDataSource;
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
            OKButton.IsEnabled = true;
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource = BufferLayer_DataSourceSelector.SelectedDataSource;
            Field = (ESRI.ArcGIS.Client.Field)FieldComboBox.SelectedItem;

            DialogResult = true;
        }
    }
}
