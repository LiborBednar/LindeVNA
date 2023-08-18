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

using Noris.WS.ServiceGate;
using Noris.Clients.ServiceGate;


namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void urlLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            profilComboBox.Items.Clear();
            jazykComboBox.Items.Clear();
            using (new WaitCursorIndicator(this))
                try
                {
                    Globals.SgConnector = ServiceGateConnector.Create(((Label)sender).Content.ToString());
                    if (Globals.SgConnector != null)
                    {
                        foreach (DbProfile p in Globals.SgConnector.DbProfiles)
                            profilComboBox.Items.Add(p.ProfileName);
                        foreach (LanguageInfo l in Globals.SgConnector.Languages)
                            jazykComboBox.Items.Add(l.Code);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba: " + ex.Message.ToString());
                }
            if (profilComboBox.SelectedIndex < 0)
                profilComboBox.SelectedIndex = 0;
            if (jazykComboBox.SelectedIndex < 0)
                jazykComboBox.SelectedIndex = 0;
        }

        private void PrihlasitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = loginTextBox.Text;
                string profil = "";
                if (profilComboBox.SelectedIndex >= 0)
                    profil = profilComboBox.SelectedItem.ToString();
                string heslo = hesloTextBox.Text;

                if (String.IsNullOrWhiteSpace(login))
                    throw new ApplicationException("Login musí být zadán");

                if (String.IsNullOrWhiteSpace(profil))
                    throw new ApplicationException("Profil musí být zadán");

                LogOnInfo li = new LogOnInfo(profil, login, heslo);

                if (Globals.SgConnector.LoggedOn)
                {
                    try
                    {
                        Globals.SgConnector.LogOff();
                    }
                    catch (Noris.Clients.ServiceGate.ConnectorException) { }
                }

                Globals.SgConnector.LogOn(li);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message.ToString());
            }

            if (Globals.SgConnector != null && Globals.SgConnector.LoggedOn)
            {
                TerminalVNA terminal = null;
                try
                {
                    terminal = new TerminalVNA();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                if (terminal != null)
                {
                    this.Hide();
                    terminal.ShowDialog();
                }
            }
        }

        private void LoginWindow1_Closed(object sender, EventArgs e)
        {
            if (Globals.SgConnector != null && Globals.SgConnector.LoggedOn)
            {
                try
                {
                    Globals.SgConnector.LogOff();
                }
                catch { }
            }
        }
    }
}

