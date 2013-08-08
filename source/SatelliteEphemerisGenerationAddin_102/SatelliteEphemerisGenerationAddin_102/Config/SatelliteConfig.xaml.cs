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

namespace SatelliteEphemerisGenerationAddin_102.Config
{
    /// <summary>
    /// Interaction logic for SatelliteConfig.xaml
    /// </summary>
    public partial class SatelliteConfig : Window
    {
        public string serviceURL { get; private set; }

        public SatelliteConfig()
        {
            InitializeComponent();
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            serviceURL = ServiceTextBox.Text;
            DialogResult = true;

        }

    }
}
