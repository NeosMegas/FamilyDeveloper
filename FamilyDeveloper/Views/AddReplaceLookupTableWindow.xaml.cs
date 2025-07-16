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
using System.Windows.Navigation;
using System.Windows.Shapes;
using FamilyDeveloper.ViewModels;

namespace FamilyDeveloper.Views
{
    /// <summary>
    /// Interaction logic for AddReplaceLookupTableWindow.xaml
    /// </summary>
    public partial class AddReplaceLookupTableWindow : Window
    {
        public AddReplaceLookupTableWindow()
        {
            InitializeComponent();
        }

        private void cmbOk_Click(object sender, RoutedEventArgs e)
        {
            if (txt.Text.Length > 0)
                DialogResult = true;
        }
    }
}
