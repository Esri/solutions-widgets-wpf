using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace LiveWebCams.Config
{
    /// <summary>
    /// Interaction logic for WebCamWidgetDialog.xaml
    /// </summary>
    public partial class WebCamWidgetDialog : Window
    {

        public DataSource DataSource { get; private set; }
        public string cameraURL { get; private set; }
        public string Caption { get; private set; }

        public WebCamWidgetDialog(IList<DataSource> dataSources, string initialCaption, string initialDataSourceId, string cameraurl)
        {
            InitializeComponent();

            // When re-configuring, initialize the widget config dialog from the existing settings.
            CaptionTextBox.Text = initialCaption;
            if (!string.IsNullOrEmpty(initialDataSourceId))
            {
                DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
                URLTextBox.Text = cameraurl;
            }
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DataSource = DataSourceSelector.SelectedDataSource;
            Caption = CaptionTextBox.Text;
            cameraURL = URLTextBox.Text;

            DialogResult = true;
        }

        private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
        {
            DataSource dataSource = DataSourceSelector.SelectedDataSource;

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
