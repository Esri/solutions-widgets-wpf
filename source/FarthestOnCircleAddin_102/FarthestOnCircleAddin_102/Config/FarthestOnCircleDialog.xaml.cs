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

namespace FarthestOnCircleAddin_102.Config
{

    /// <summary>
    /// Interaction logic for FarthestOnCircleDialog.xaml
    /// </summary>
    /// 
    public partial class FarthestOnCircleDialog : Window
    {
        public string serviceURL { get; private set; }

        public FarthestOnCircleDialog()
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
