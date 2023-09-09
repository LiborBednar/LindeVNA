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

namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for NewConnection.xaml
    /// </summary>
    public partial class NewConnection : Window
    {
        public NewConnection()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtName.SelectAll();
            txtName.Focus();
        }

        public string ConnectionName
        {
            get { return txtName.Text; }
        }

        public string ConnectionUrl
        {
            get { return txtUrl.Text; }
        }
    }
}
