using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Xml.Serialization;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for ConnectionManagerWindow.xaml
    /// </summary>
    public partial class ConnectionManagerWindow : Window
    {
        private ConnectionManager _ConnectionManager;
        public ConnectionManagerWindow(ConnectionManager manager)
        {
            InitializeComponent();
            _ConnectionManager = manager;
            DataContext = _ConnectionManager;
            if (_ConnectionManager.Connections.Count > 0 && !String.IsNullOrEmpty(_ConnectionManager.ActualConnection))
            {
                try
                {
                    var c = _ConnectionManager.Connections.First(x => x.Name == _ConnectionManager.ActualConnection);
                    if (c != null)
                        ConnectionsListBox.SelectedItem = c;
                }
                catch
                {
                    ConnectionsListBox.SelectedIndex = 0;
                }
            }
            else
                _ConnectionManager.ActualConnection = null;
        }

        private void NovyButton_Click(object sender, RoutedEventArgs e)
        {
            NewConnection newConnection  = new NewConnection();
            if (newConnection.ShowDialog() == true)
            {
                string name = newConnection.ConnectionName;
                string url = newConnection.ConnectionUrl;
                try
                {
                    _ConnectionManager.Add(name, url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void SmazaButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsListBox.SelectedItem != null)
                _ConnectionManager.Remove((Connection)ConnectionsListBox.SelectedItem);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _ConnectionManager.Save();
        }

        private void ConnectionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConnectionsListBox.SelectedItem != null)
                _ConnectionManager.ActualConnection = ConnectionsListBox.SelectedItem.ToString();
        }

        private void LoginWindow1_Closed(object sender, EventArgs e)
        {

        }
    }
}
