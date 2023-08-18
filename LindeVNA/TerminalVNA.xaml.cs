using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Noris.WS.ServiceGate;
using Noris.Clients.ServiceGate;

namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for TerminalVNA.xaml
    /// </summary>
    public partial class TerminalVNA : Window
    {
        private SerialPort _SerialPort;
        private String _DataBuffer = "";

        private int? _BaterryLevel;
        public int? BaterryLevel
        {
            set
            {
                _BaterryLevel = value;
                if (_BaterryLevel.HasValue)
                {
                    baterryProgressBar.Visibility = Visibility.Visible;
                    baterryProgressBar.Value = _BaterryLevel.Value;
                    if (_BaterryLevel >= 50)
                        baterryProgressBar.Foreground = Brushes.Green;
                    else if (_BaterryLevel >= 15)
                        baterryProgressBar.Foreground = Brushes.Orange;
                    else
                        baterryProgressBar.Foreground = Brushes.Red;
                }
                else
                {
                    baterryProgressBar.Visibility = Visibility.Hidden;
                }
            }
        }

        public string _Operation;
        public string Operation
        {
            set
            {
                _Operation = value;
                if (!String.IsNullOrEmpty(_Operation))
                {
                    lblOperation.Visibility = Visibility.Visible;
                    string nazevOperace = "????";
                    switch (_Operation)
                    {
                        case "H":
                            nazevOperace = "Naložení";
                            break;
                        case "B":
                            nazevOperace = "Vyložení";
                            break;
                        case "K":
                            nazevOperace = "Picking";
                            break;
                    }
                    lblOperation.Content = String.Format("Operace: {0}", nazevOperace);
                }
                else
                {
                    lblOperation.Visibility = Visibility.Hidden;
                }
            }
        }

        public string _CurrentPosition;
        public string CurrentPosition
        {
            set
            {
                _CurrentPosition = value;
                if (!String.IsNullOrEmpty(_CurrentPosition))
                {
                    lblCurrentPosition.Visibility = Visibility.Visible;
                    lblCurrentPosition.Content = String.Format("Aktuální pozice: {0}", _CurrentPosition);
                }
                else
                {
                    lblCurrentPosition.Visibility = Visibility.Hidden;
                }
            }
        }

        public string _NominalPosition;
        public string NominalPosition
        {
            set
            {
                _NominalPosition = value;
                if (!String.IsNullOrEmpty(_NominalPosition))
                {
                    lblNominalPosition.Visibility = Visibility.Visible;
                    lblNominalPosition.Content = String.Format("Zadaná pozice: {0}", _NominalPosition);
                }
                else
                {
                    lblNominalPosition.Visibility = Visibility.Hidden;
                }
            }
        }

        public string _Status;
        public string Status
        {
            set
            {
                _Status = value;

                string popis = "Neznámý";
                Brush pozadi = Brushes.Orange;

                if (!String.IsNullOrEmpty(_Status))
                {
                    if (_Status == "000")
                    {
                        popis = "OK";
                        pozadi = Brushes.Green;
                    }
                    else
                    {
                        popis = _Status;
                        pozadi = Brushes.Red;
                    }
                }
                lblStatus.Content = String.Format("Status: {0}", popis);
                lblStatus.Background = pozadi;
            }
        }

        private int? _OperationTime;
        public int? OperationTime
        {
            set
            {
                _OperationTime = value;
                if (_OperationTime.HasValue)
                {
                    lblOperationTime.Visibility = Visibility.Visible;
                    lblOperationTime.Content = String.Format("Operační čas: {0:0.0} hod. ", _OperationTime);
                }
                else
                {
                    lblOperationTime.Visibility = Visibility.Hidden;
                }
            }
        }

        private string _LastComPort;
        public string LastComPort
        {
            set
            {
                _LastComPort = value;
                Srv.SetSetting("ComPort", _LastComPort);

                if (_SerialPort != null)
                    _SerialPort.Dispose();
                _SerialPort = new SerialPort(_LastComPort, 9600, Parity.None, 8, StopBits.One);
                _SerialPort.ReadTimeout = 1000;
                _SerialPort.WriteTimeout = 1000;
                _SerialPort.DataReceived += _SerialPort_DataReceived;
            }
            get
            {
                if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>("ComPort"))
                    _LastComPort = ConfigurationManager.AppSettings["ComPort"];
                return _LastComPort;
            }
        }

        private int _HeliosVNA;
        public int HeliosVNA
        {
            set
            {
                _HeliosVNA = value;
                Srv.SetSetting("HeliosVNA", _HeliosVNA.ToString());
            }
            get
            {
                _HeliosVNA = 0;
                if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>("HeliosVNA"))
                    _HeliosVNA = Convert.ToInt32(ConfigurationManager.AppSettings["HeliosVNA"]);
                return _HeliosVNA;
            }
        }

        private string _SynchroID;
        public string SynchroID
        {
            set
            {
                _SynchroID = value;
                Srv.SetSetting("SynchroID", _SynchroID);
            }
            get
            {
                _SynchroID = "";
                if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>("SynchroID"))
                    _SynchroID = ConfigurationManager.AppSettings["SynchroID"].ToString();
                return _SynchroID;
            }
        }

        public string _SkladRefer;
        public string SkladRefer
        {
            set
            {
                _SkladRefer = value;
                tbxSklad.Text = _SkladRefer;
            }
        }



        private void _ResetStatus()
        {
            BaterryLevel = null;
            OperationTime = null;
            CurrentPosition = null;
            NominalPosition = null;
            Status = null;
        }

        public TerminalVNA()
        {
            InitializeComponent();

            foreach (string s in SerialPort.GetPortNames())
                cbxComPorts.Items.Add(s);
            if (cbxComPorts.Items.Count > 0)
            {
                int index = 0;

                if (!String.IsNullOrWhiteSpace(LastComPort))
                {
                    index = cbxComPorts.Items.IndexOf(LastComPort);
                    if (index >= 0)
                        cbxComPorts.SelectedIndex = index;
                }
            }
            lblComPorts.Visibility = Visibility.Hidden;
            cbxComPorts.Visibility = Visibility.Hidden;
            BtnConnect.Visibility = Visibility.Hidden;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (HeliosVNA == 0)
            {
                NactiVozikyProParovani();
                MessageBox.Show("VNA vozík není spárován s Heliosem. Proveďte spárování.");
            }
            else
            {
                _KontrolaSparovani(HeliosVNA, SynchroID);
            }
            _ResetStatus();
        }

        // delegate is used to write to a UI control from a non-UI thread
        private delegate void SetTextDeleg(string text);

        private void _SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _SerialPort.ReadExisting();
            // Invokes the delegate on the UI thread, and sends the data that was received to the invoked method.
            // ---- The "si_DataReceived" method will be executed on the UI thread, which allows populating the textbox.
            this.Dispatcher.BeginInvoke(new SetTextDeleg(_ProcesData), new object[] { data });
        }

        private void _ProcesData(string data)
        {
            //Thread.Sleep(1500);
            _DataBuffer += data;
            int telegramStart = _DataBuffer.IndexOf("<");
            if (telegramStart < 0)
                _DataBuffer = "";
            else
                _DataBuffer = _DataBuffer.Remove(0, telegramStart);
            int telegramEnd = _DataBuffer.IndexOf(">");
            while (telegramEnd >= 0)
            {
                telegramStart = _DataBuffer.IndexOf("<");
                if (telegramStart < 0)
                {
                    _DataBuffer = "";
                    telegramEnd = 0;
                }
                else if (telegramStart > 0)
                {
                    _DataBuffer = _DataBuffer.Remove(0, telegramStart);
                }
                else
                {
                    int secStart = _DataBuffer.IndexOf("<", 1);
                    if (secStart > 0 && secStart < telegramEnd)
                    {
                        _DataBuffer = _DataBuffer.Remove(0, secStart);
                    }
                    else
                    {
                        string telegram = _DataBuffer.Substring(0, telegramEnd + 1);
                        _DataBuffer = _DataBuffer.Remove(0, telegramEnd + 1);
                        OutputTextBox.Text += telegram + "\r\n";
                        OutputTextBox.ScrollToEnd();
                        try
                        {
                            _ProcesTelegram(telegram);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                telegramEnd = _DataBuffer.IndexOf(">");
            }
        }

        private void _ProcesTelegram(string telegram)
        {
            int delka = 0;
            try
            {
                delka = Convert.ToInt32(telegram.Substring(1, 2));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Nepodařilo se zjistit délku telegramu: " + ex.Message);
            }
            if (delka != telegram.Length)
                throw new ApplicationException("Deklarovaná délka telegramu neodpovídá jeho skutečné délce.");
            string command = telegram.Substring(3, 1);
            switch (command)
            {
                case "p":
                    break;
                case "a":
                    break;
                case "s":
                    string lastOrder = telegram.Substring(4, 1);
                    int firstSC = telegram.IndexOf(";");
                    int lastSC = telegram.LastIndexOf(";");
                    if (firstSC < 0 || firstSC == lastSC)
                        throw new ApplicationException("Chybný formát status telegramu.");
                    string[] split = telegram.Substring(firstSC + 1, lastSC - firstSC - 1).Split(';');
                    if (split.Length != 10 && split.Length != 11)
                        throw new ApplicationException("Chybný formát status telegramu.");
                    string nominalArea = split[0];
                    string nominalRow = split[1];
                    string nominalBays = split[2];
                    string nominalLocation = split[3];
                    string nominalLevel = split[4];

                    string pom = split[5];
                    string restart = pom.Substring(0, 1);
                    string fault = pom.Substring(1, 3);

                    //Takto to vrací emulátor vozíku
                    string currentArea = pom.Substring(4);
                    string currentRow = split[6];
                    string currentBays = split[7];
                    string currentLocation = split[8];
                    string currentLevel = split[9];

                    //Takto by to mělo být podle dokumentace
                    if (split.Length == 11)
                    {
                        currentArea = split[6];
                        currentRow = split[7];
                        currentBays = split[8];
                        currentLocation = split[9];
                        currentLevel = split[10];
                    }
                    Operation = lastOrder;
                    CurrentPosition = currentArea + " " + currentRow + "-" + currentLevel + "-" + currentLocation;
                    NominalPosition = nominalArea + " " + nominalRow + "-" + nominalLevel + "-" + nominalLocation;
                    Status = fault;
                    break;
                case "b":
                    try
                    {
                        OperationTime = Convert.ToInt32(telegram.Substring(4, 4));
                        BaterryLevel = Convert.ToInt32(telegram.Substring(8, 3));
                    }
                    catch
                    {
                        throw new ApplicationException("Chybný formát status telegramu.");
                    }
                    break;
                default:
                    throw new ApplicationException("Nepodporovaný typ telegramu.");
            }
        }

        private void TerminalWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Globals.SgConnector != null && Globals.SgConnector.LoggedOn)
            {
                try
                {
                    Globals.SgConnector.LogOff();
                }
                catch { }
            }

            try
            {
                if (_SerialPort != null && _SerialPort.IsOpen)
                    _SerialPort.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }

            Application.Current.MainWindow.Show();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_SerialPort != null)
            {
                _DeOpenSerialPort();
            }
            else
            {
                lblComPorts.Foreground = Brushes.Red;
                MessageBox.Show("Není vybrán komunikační port.");
            }
        }

        private void _DeOpenSerialPort()
        {
            try
            {
                if (_SerialPort.IsOpen)
                    _SerialPort.Close();
                else
                    _SerialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            finally
            {
                if (_SerialPort.IsOpen)
                {
                    BtnConnect.Content = "Odpojit";
                    BtnConnect.Foreground = Brushes.Black;
                    cbxComPorts.IsEnabled = false;
                }
                else
                {
                    BtnConnect.Content = "Připojit";
                    BtnConnect.Foreground = Brushes.Red;
                    cbxComPorts.IsEnabled = true;
                    _ResetStatus();
                }
            }
        }

        private void StatusRequestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SendTelegram("<06CS>");
                SendTelegram("<06CB>");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void SendTelegram(string telegram)
        {
            if (_SerialPort.IsOpen)
                _SerialPort.Write(telegram);
            else
                throw new ApplicationException("Seriový port není otevřen.");
            InputTextBox.Text += telegram + "\r\n";
            InputTextBox.ScrollToEnd();

        }

        private void BtnClearOutputBox_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
        }

        private void BtnClearInputBox_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Clear();
        }

        private void CbxComPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LastComPort = cbxComPorts.Items[cbxComPorts.SelectedIndex].ToString();
            lblComPorts.Foreground = Brushes.Black;
        }

        private void CbxHeliosVNA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_HeliosVNA == 0 && cbxHeliosVNA.SelectedValue != null)
            {
                Int32 vozik = (Int32)cbxHeliosVNA.SelectedValue;
                string newSID = DateTime.Now.ToString("yyMMdd") + "-" + Guid.NewGuid();

                RunFunctionRequest request = new RunFunctionRequest();
                request.Function.FunctionId = 23480; //  VNA client request
                request.UserData = new FunctionUserData();

                InputTable inputTable = new InputTable("InputParams");
                inputTable.AddColumn("command_key", typeof(string));
                inputTable.AddColumn("vozik", typeof(int));
                inputTable.AddColumn("new_sid", typeof(string));
                int row = inputTable.AddRow();
                inputTable.SetItem(0, "command_key", "sparuj_vna_vozik");
                inputTable.SetItem(0, "vozik", vozik);
                inputTable.SetItem(0, "new_sid", newSID);
                List<InputTable> inputTables = new List<InputTable>();
                inputTables.Add(inputTable);
                request.UserData.SetDatastores<InputTable>(inputTables);

                try
                {
                    RunFunctionResponse response = request.Process(Globals.SgConnector);
                    Srv.ResponseStateFailure(response);
                    _KontrolaSparovani(vozik, newSID);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nepodařilo se zjistit spárování vozíku v Heliosu: " + ex.Message);
                }
            }
        }

        private void _KontrolaSparovani(int vozik, string sid)
        {
            string newSID = DateTime.Now.ToString("yyMMdd") + "-" + Guid.NewGuid();

            RunFunctionRequest request = new RunFunctionRequest();
            request.Function.FunctionId = 23480; //  VNA client request
            request.UserData = new FunctionUserData();

            InputTable inputTable = new InputTable("InputParams");
            inputTable.AddColumn("command_key", typeof(string));
            inputTable.AddColumn("vozik", typeof(int));
            inputTable.AddColumn("old_sid", typeof(string));
            inputTable.AddColumn("new_sid", typeof(string));
            int row = inputTable.AddRow();
            inputTable.SetItem(0, "command_key", "kontrola_sparovani");
            inputTable.SetItem(0, "vozik", vozik);
            inputTable.SetItem(0, "old_sid", sid);
            inputTable.SetItem(0, "new_sid", newSID);
            List<InputTable> inputTables = new List<InputTable>();
            inputTables.Add(inputTable);
            request.UserData.SetDatastores<InputTable>(inputTables);

            try
            {
                RunFunctionResponse response = request.Process(Globals.SgConnector);
                Srv.ResponseStateFailure(response);

                //Uložení do konfigurace
                HeliosVNA = vozik;
                SynchroID = newSID;
                cbxHeliosVNA.ItemsSource = response.OutputParams.Table.DefaultView;
                cbxHeliosVNA.DisplayMemberPath = "vna_refer";
                cbxHeliosVNA.SelectedValuePath = "vna";
                cbxHeliosVNA.IsEnabled = false;
                cbxHeliosVNA.SelectedIndex = 0;

                lblHeliosVNA.Content = "ID vozíku";
                lblHeliosVNA.Foreground = Brushes.Black;

                SkladRefer = response.OutputParams.Table.Rows[0]["sklad_refer"].ToString();

                lblComPorts.Visibility = Visibility.Visible;
                cbxComPorts.Visibility = Visibility.Visible;
                BtnConnect.Visibility = Visibility.Visible;

                if (_SerialPort != null && !String.IsNullOrEmpty(LastComPort) && !_SerialPort.IsOpen)
                    _DeOpenSerialPort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba při kontrole spárování vozíku v Heliosu: " + ex.Message);
                _HeliosVNA = 0;
                NactiVozikyProParovani();
            }
        }

        private void NactiVozikyProParovani()
        {
            RunFunctionRequest request = new RunFunctionRequest();
            request.Function.FunctionId = 23480; //  VNA client request
            request.UserData = new FunctionUserData();

            InputTable inputTable = new InputTable("InputParams");
            inputTable.AddColumn("command_key", typeof(string));
            int row = inputTable.AddRow();
            inputTable.SetItem(0, "command_key", "get_free_vna_list");
            List<InputTable> inputTables = new List<InputTable>();
            inputTables.Add(inputTable);
            request.UserData.SetDatastores<InputTable>(inputTables);

            try
            {
                RunFunctionResponse response = request.Process(Globals.SgConnector);
                Srv.ResponseStateFailure(response);
                if (response.OutputParams.Table.Rows.Count > 0)
                {
                    cbxHeliosVNA.ItemsSource = response.OutputParams.Table.DefaultView;
                    cbxHeliosVNA.DisplayMemberPath = "vna_refer";
                    cbxHeliosVNA.SelectedValuePath = "vna";
                    cbxHeliosVNA.IsEnabled = true;
                    lblHeliosVNA.Content = "Vyber ID pro spárování";
                    lblHeliosVNA.Foreground = Brushes.Red;
                }
                else
                    throw new ApplicationException("Nejsou k dispozi žádné volné VNA vozíky v Heliosu.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nepodařilo se zjistit seznam nespárovaných zařízení v Heliosu:" + ex.Message);
            }
        }
    }
}
