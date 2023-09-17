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
using NLog;


namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private ConnectionManager _ConnectionManager;

        public LoginWindow()
        {
            LogManager.Configuration.Variables["LogDir"] = Srv.LocalApplicationDataPath();
            Globals.Logger = LogManager.GetLogger("file");
            Globals.Logger.Info("Program started");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();
            Title = "Linde VNA klient v. " + Srv.GetRunningVersion();

            _ConnectionManager = ConnectionManager.Load();
            DataContext = _ConnectionManager;
            if (_ConnectionManager.Connections.Count > 0 && !String.IsNullOrEmpty(_ConnectionManager.ActualConnection))
            {
                try
                {
                    var c = _ConnectionManager.Connections.First(x => x.Name == _ConnectionManager.ActualConnection);
                    if (c != null)
                        prihlasenComboBox.SelectedItem = c;
                }
                catch
                {
                    prihlasenComboBox.SelectedIndex = 0;
                }
            }
            else
                _ConnectionManager.ActualConnection = null;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Globals.Logger.Error("Neoštřená výjimka.");
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Globals.Logger.Error(ex);
                Globals.Logger.Trace(ex.ToString());
            }
        }

        private void urlLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            Connection c = prihlasenComboBox.SelectedItem as Connection;
            if (c != null)
            {
                using (new WaitCursorIndicator(this))
                {
                    try
                    {
                        Globals.SgConnector = ServiceGateConnector.Create(c.Url);
                        profilComboBox.Items.Clear();
                        jazykComboBox.Items.Clear();
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
                        MessageBox.Show("Chyba: " + ex.ToString(), ex.Message);
                    }
                    finally
                    {
                    }
                }

                profilComboBox.SelectedIndex = profilComboBox.Items.IndexOf(c.Profil);
                jazykComboBox.SelectedIndex = jazykComboBox.Items.IndexOf(c.Language);
                if (c.RememberPassword)
                    hesloTextBox.Password = c.Password;

                if (profilComboBox.SelectedIndex < 0)
                    profilComboBox.SelectedIndex = 0;
                if (jazykComboBox.SelectedIndex < 0)
                    jazykComboBox.SelectedIndex = 0;

                if (String.IsNullOrEmpty(loginTextBox.Text))
                    loginTextBox.Focus();
                else if (String.IsNullOrEmpty(hesloTextBox.Password))
                    hesloTextBox.Focus();
                else
                    prihlasitButton.Focus();
            }
        }

        private void PrihlasitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = loginTextBox.Text;
                string profil = "";
                if (profilComboBox.SelectedIndex >= 0)
                    profil = profilComboBox.SelectedItem.ToString();
                string heslo = hesloTextBox.Password;

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
                Connection c = prihlasenComboBox.SelectedItem as Connection;
                if (c != null)
                {
                    c.Profil = profilComboBox.SelectedItem.ToString();
                    c.Language = jazykComboBox.SelectedItem.ToString();
                    if (RememberPasswordCheckBox.IsChecked ?? false)
                        c.Password = hesloTextBox.Password;
                    else
                        hesloTextBox.Password = null;
                    _ConnectionManager.Save();
                }

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

        private void PripojeniButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionManagerWindow cm = new ConnectionManagerWindow(_ConnectionManager);
            cm.ShowDialog();
        }

        private void PrihlasenComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (profilComboBox.SelectedItem != null)
                _ConnectionManager.ActualConnection = prihlasenComboBox.SelectedItem.ToString();
            Connect();
        }

    }
}

