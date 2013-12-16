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
using ESRI.ArcGIS.OperationsDashboard;
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
using client = ESRI.ArcGIS.Client;
using System.Collections.ObjectModel;

namespace FindClosestResource.Config
{
    public partial class FindClosestResourceDialog : Window
    {
        public DataSource DataSource { get; private set; }
        
        public ESRI.ArcGIS.Client.Field Field { get; private set; }

        public ObservableCollection<DataSource> BarrierDataSources {get; private set;}
        private ObservableCollection<DataSource> barriersToAdd;

        public FindClosestResourceDialog(IList<DataSource> dataSources, string initialDataSourceId, string initialField, ObservableCollection<DataSource> selectedBarriers)
        {
            InitializeComponent();
            this.DataContext = this;

            //Initialize the DataSourceSelector and the field combo box
            InitializeDataSource(initialDataSourceId, initialField);

            InitializeBarrierDataSources(selectedBarriers);
        }

        // ***********************************************************************************
        // * Initialize Barriers DataSource Layer(s) 
        // ***********************************************************************************
        private void InitializeBarrierDataSources(ObservableCollection<DataSource> selectedBarriers)
        {
            if (selectedBarriers != null)
                barriersToAdd = new ObservableCollection<DataSource>(selectedBarriers);
            else
                barriersToAdd = new ObservableCollection<DataSource> { };

            lbBarrierLayers.ItemsSource = barriersToAdd;
        }

        // ***********************************************************************************
        // * Initialize Resources/Facilities DataSource Layer  
        // ***********************************************************************************
        private void InitializeDataSource(string initialDataSourceId, string initialField)
        {
            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                if (dataSource != null)
                {
                    ResourceLayer_DataSourceSelector.SelectedDataSource = dataSource;
                    if (!string.IsNullOrEmpty(initialField))
                    {
                        client.Field field = dataSource.Fields.FirstOrDefault(fld => fld.FieldName == initialField);
                        FieldComboBox.SelectedItem = field;
                    }
                }
            }
        }

        // ***********************************************************************************
        // * User clicked on OK button. Collect the parameters and set their values to be passed
        // * to the tool. 
        // ***********************************************************************************
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource = ResourceLayer_DataSourceSelector.SelectedDataSource;
            Field = (ESRI.ArcGIS.Client.Field)FieldComboBox.SelectedItem;

            BarrierDataSources = barriersToAdd; 

            DialogResult = true;
        }

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = ResourceLayer_DataSourceSelector.SelectedDataSource;
            FieldComboBox.ItemsSource = dataSource.Fields;
            FieldComboBox.SelectedItem = dataSource.Fields[0];
            List<ESRI.ArcGIS.Client.Field> numericFields = new List<ESRI.ArcGIS.Client.Field>();
            foreach (var field in dataSource.Fields)

                ValidateInput(sender, null);
        }

        private void BarriersDataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = BarriersLayer_DataSourceSelector.SelectedDataSource;
            var q = barriersToAdd.Where(ds => ds.Id == dataSource.Id).FirstOrDefault();
            if (q == null)
                 barriersToAdd.Add(dataSource);
        }

        private void ValidateInput(object sender, TextChangedEventArgs e)
        {
            if (OKButton == null)
                return;

            OKButton.IsEnabled = false;
            OKButton.IsEnabled = true;
        }

        void DeleteRow(object sender, RoutedEventArgs e)
        {
            ListBoxItem local = ((sender as Button).Tag as ListBoxItem);
            var button = sender as Button;
            if (button != null)
            {

                var dataSource = button.DataContext as DataSource; 
                ((ObservableCollection<DataSource>)lbBarrierLayers.ItemsSource).Remove(dataSource);
            }
            else
                return;
        }
    }
}
