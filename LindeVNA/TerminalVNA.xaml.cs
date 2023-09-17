using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data;
using System.Threading;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using Noris.WS.ServiceGate;
using Noris.Clients.ServiceGate;

namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for TerminalVNA.xaml
    /// </summary>
    public partial class TerminalVNA : Window
    {
        DispatcherTimer _Timer;

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
                _PrekresliUkol();
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
            get
            {
                return _CurrentPosition;
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
            get
            {
                return _Status;
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
                Srv.SetSettings("ComPort", _LastComPort);

                if (_SerialPort != null)
                    _SerialPort.Dispose();
                _SerialPort = new SerialPort(_LastComPort, 9600, Parity.None, 8, StopBits.One);
                _SerialPort.ReadTimeout = 1000;
                _SerialPort.WriteTimeout = 1000;
                _SerialPort.DataReceived += _SerialPort_DataReceived;
            }
            get
            {
                return Srv.GetSettings("ComPort");
            }
        }

        private int? _HeliosVNA = null;
        public int HeliosVNA
        {
            set
            {
                _HeliosVNA = value;
                Srv.SetSettings("HeliosVNA", _HeliosVNA.ToString());
            }
            get
            {
                if (!_HeliosVNA.HasValue)
                    _HeliosVNA = Srv.GetSettings("HeliosVNA", 0);
                return _HeliosVNA.Value;
            }
        }

        private string _SynchroID;
        public string SynchroID
        {
            get
            {
                _SynchroID = "";
                if (ConfigurationManager.AppSettings.HasKeys() && ConfigurationManager.AppSettings.AllKeys.Contains<String>("SynchroID"))
                    _SynchroID = ConfigurationManager.AppSettings["SynchroID"].ToString();
                if (String.IsNullOrEmpty(_SynchroID))
                    _SynchroID = DateTime.Now.ToString("yyMMdd") + "-" + Guid.NewGuid();
                Srv.SetSettings("SynchroID", _SynchroID);
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

        private bool _OrderConfirmed;
        public bool OrderConfirmed
        {
            set
            {
                _OrderConfirmed = value;
            }
            get
            {
                return _OrderConfirmed;
            }
        }

        private bool _StatusConfirmed;
        public bool StatusConfirmed
        {
            set
            {
                _StatusConfirmed = value;
            }
            get
            {
                return _StatusConfirmed;
            }
        }

        private DateTime _AktualizaceAktualniPozice = DateTime.Now;
        private string _AktualniPozice = "";
        public string AktualniPozice
        {
            get
            {
                return _AktualniPozice;
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && (_AktualniPozice != value || DateTime.Now.Subtract(_AktualizaceAktualniPozice) > TimeSpan.FromMinutes(9)))
                {
                    _AktualizaceAktualniPozice = DateTime.Now;

                    InputTable inputTable = new InputTable("InputParams");
                    inputTable.AddColumn("command_key", typeof(string));
                    inputTable.AddColumn("vozik", typeof(int));
                    inputTable.AddColumn("aktualni_pozice", typeof(string));
                    int row = inputTable.AddRow();
                    inputTable.SetItem(0, "command_key", "zmena_aktualni_pozice");
                    inputTable.SetItem(0, "vozik", HeliosVNA);
                    inputTable.SetItem(0, "aktualni_pozice", value.Substring(2));
                    try
                    {
                        RunFunctionResponse response = _VnaClientRequest(inputTable);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Nepodařilo se zjistit spárování vozíku v Heliosu: " + ex.Message);
                    }

                }
                _AktualniPozice = value;
                if (lblAktualniPoziceVal.Content.ToString() != _AktualniPozice)
                {
                    lblAktualniPoziceVal.Content = _AktualniPozice;
                    _PrekresliUkol();
                }
            }
        }

        public int VybranyUkolVNA { get; set; } = 0;

        public int NovyUkolVNA { get; set; } = 0;

        public string StavUkoluVNA { get; set; } = "";

        private void _ResetStatus()
        {
            BaterryLevel = null;
            OperationTime = null;
            CurrentPosition = null;
            NominalPosition = null;
            Status = null;
            AktualniPozice = "";
        }

        public TerminalVNA()
        {
            InitializeComponent();

            if (Globals.SgConnector.LogOnInfo.DbProfile.ToUpper().Contains("TEST"))
                Background = Brushes.LightPink;

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
            lblStavUkolu.Content = "Inicializace";

        }

        private void _Timer_Tick(object sender, EventArgs e)
        {
            if (VybranyUkolVNA == 0)
                _NactiUkol();
        }

        private void _NactiUkol()
        {
            string stavUkoluVna = "";
            VybranyUkolVNA = 0;
            NovyUkolVNA = 0;

            if (HeliosVNA == 0)
            {
                lblStavUkolu.Content = "Nespárováno Helios";
                lblStavUkolu.Foreground = Brushes.Red;
            }
            else if (_SerialPort == null)
            {
                lblStavUkolu.Content = "Není vybrán komunikační port";
                lblStavUkolu.Foreground = Brushes.Red;
            }
            else if (!_SerialPort.IsOpen)
            {
                lblStavUkolu.Content = "Nepřipojeno k VNA";
                lblStavUkolu.Foreground = Brushes.Red;
            }
            else if (String.IsNullOrEmpty(AktualniPozice))
            {
                if (!_HandShakeS())
                    lblStavUkolu.Content = Status;
                else
                    lblStavUkolu.Content = "Neznámá pozice";
                lblStavUkolu.Foreground = Brushes.Red;
            }
            else
            {
                DataTable table = null;
                try
                {
                    BrowseRequest request = new BrowseRequest(22567, new BrowseId(BrowseType.Template, 22645));
                    request.Bounds.LowerBound = 1;
                    request.Bounds.UpperBound = 10;
                    request.BaseFilterArguments = new FilterArgumentList();
                    request.BaseFilterArguments.Add("aktualni_pozice", AktualniPozice.Substring(2));
                    request.BaseFilterArguments.Add("vna_vozik", HeliosVNA);
                    BrowseResponse response = request.Process(Globals.SgConnector);
                    table = response.Data.MainTable;
                }
                catch { }

                if (table == null)
                {
                    lblStavUkolu.Content = "Nepodařilo se načíst úkol";
                    lblStavUkolu.Foreground = Brushes.Red;
                }
                else if (table.Rows.Count == 0)
                {
                    lblStavUkolu.Content = "Seznam úkolů je prázdný";
                    lblStavUkolu.Foreground = Brushes.Red;
                }
                else
                {
                    stavUkoluVna = table.Rows[0]["stav_ukolu_vna"].ToString();
                    //string typOperaceVna = table.Rows[0]["typ_operace_vna"].ToString();

                    lblStavUkolu.Content = table.Rows[0]["typ_operace_vna_es"].ToString() + ": " + table.Rows[0]["stav_ukolu_vna_es"].ToString();
                    lblAktualniPoziceVal.Content = AktualniPozice;
                    lblOdkudVal.Content = table.Rows[0]["odkud"].ToString();
                    lblKamVal.Content = table.Rows[0]["kam"].ToString();
                    lblBaleniVal.Content = table.Rows[0]["sarze_refer"].ToString();
                    lblPrioritaVal.Content = table.Rows[0]["priorita"].ToString();
                    lblStavUkolu.Foreground = Brushes.Green;

                    lblAktualniPoziceVal.Foreground = Brushes.Black;
                    lblOdkudVal.Foreground = Brushes.Black;
                    lblKamVal.Foreground = Brushes.Black;

                    if (stavUkoluVna == "P")
                        NovyUkolVNA = (Int32)table.Rows[0]["vna_ukol"];
                    else
                        VybranyUkolVNA = (Int32)table.Rows[0]["vna_ukol"];

                    switch (stavUkoluVna)
                    {
                        case "P": //Plán
                            btnDefault.Content = "ZAHÁJIT ÚKOL";
                            btnDefault.IsEnabled = true;
                            break;
                        case "Z": // Zahájeno
                            btnDefault.Content = "ZAHÁJIT NAKLÁDÁNÍ";
                            btnDefault.IsEnabled = true;
                            break;
                        case "N": // Nakládání
                            {
                                string odkud = lblOdkudVal.Content.ToString();
                                _ParsePozice(odkud, out string oblast, out string rada, out string uroven, out string regal);
                                SendTelegram("F", "H", $"{ oblast};{rada};*;{regal};{uroven};0");
                                SendTelegram("C", "S", "");
                                btnDefault.Content = "POTVRDIT NALOŽENÍ";
                                btnDefault.IsEnabled = false;
                                break;
                            }
                        case "V": // Vykládání
                            {
                                string kam = lblKamVal.Content.ToString();
                                _ParsePozice(kam, out string oblast, out string rada, out string uroven, out string regal);
                                SendTelegram("F", "B", $"{ oblast};{rada};*;{regal};{uroven};0");
                                SendTelegram("C", "S", "");
                                btnDefault.Content = "POTVRDIT VYLOŽENÍ";
                                btnDefault.IsEnabled = false;
                                break;
                            }
                        case "K": // Kontrola
                            {
                                string kam = lblKamVal.Content.ToString();
                                _ParsePozice(kam, out string oblast, out string rada, out string uroven, out string regal);
                                SendTelegram("F", "B", $"{ oblast};{rada};*;{regal};{uroven};0");
                                SendTelegram("C", "S", "");
                                btnDefault.Content = "VYLOŽENO";
                                btnDefault.IsEnabled = false;
                                break;
                            }
                        default:
                            MessageBox.Show("Nepodporovaný stav VNA úkolu.");
                            break;
                    }
                }

                tbUkoly.Inlines.Clear();
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    Brush foreground = Brushes.DarkViolet;
                    FontStyle fontStyle = FontStyles.Normal;
                    FontWeight fontWeight = FontWeights.Normal; 

                    if (table.Rows[i]["typ_operace_vna"].ToString() == "V")
                        foreground = Brushes.DarkBlue;
                    if (table.Rows[i]["vynechano"].ToString() != "0")
                        fontStyle = FontStyles.Italic;
                    if (VybranyUkolVNA == (Int32)table.Rows[i]["vna_ukol"])
                        fontWeight = FontWeights.Bold;

                    Run header = new Run((i + 1).ToString() + ": " + table.Rows[i]["typ_operace_vna_es"].ToString() + ": " + table.Rows[i]["stav_ukolu_vna_es"].ToString() + "\r\n");
                    header.Foreground = foreground;
                    header.FontStyle = fontStyle;
                    header.FontWeight = fontWeight;

                    tbUkoly.Inlines.Add(header);
                    tbUkoly.Inlines.Add(new Run(table.Rows[i]["odkud"].ToString() + " ---> " + table.Rows[i]["kam"].ToString() + "\r\n\r\n") { });
                }
                UkolyScrollViewer.ScrollToHome();
            }
            StavUkoluVNA = stavUkoluVna;
            _PrekresliUkol();
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
            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromMilliseconds(5 * 1000);
            _Timer.Tick += _Timer_Tick;
            _Timer.Start();
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

                        tbTelegramy.Inlines.Add(new Run(telegram + "\r\n") { Foreground = Brushes.DarkViolet });
                        TelegramyScrollViewer.ScrollToEnd();

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

        private string[] _SplitSC(string text)
        {
            int firstSC = text.IndexOf(";");
            int lastSC = text.LastIndexOf(";");
            if (firstSC < 0 || firstSC == lastSC)
                throw new ApplicationException("Chybný formát status telegramu.");
            return text.Substring(firstSC + 1, lastSC - firstSC - 1).Split(';');
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
            string[] split;

            string currentArea = "";
            string currentRow = "";
            string currentBays = "";
            string currentLocation = "";
            string currentLevel = "";

            switch (command)
            {
                case "p": //dosažení pozice
                    split = _SplitSC(telegram);
                    if (split.Length != 5)
                        throw new ApplicationException("Chybný formát status telegramu.");
                    currentArea = split[0];
                    currentRow = split[1];
                    currentBays = split[2];
                    currentLocation = split[3];
                    currentLevel = split[4];

                    Operation = telegram.Substring(4, 1);
                    break;
                case "a": //naloženo /vyloženo
                    split = _SplitSC(telegram);
                    if (split.Length != 5)
                        throw new ApplicationException("Chybný formát status telegramu.");
                    currentArea = split[0];
                    currentRow = split[1];
                    currentBays = split[2];
                    currentLocation = split[3];
                    currentLevel = split[4];

                    Operation = telegram.Substring(4, 1);
                    btnDefault.IsEnabled = true;
                    break;
                case "s":
                    {
                        split = _SplitSC(telegram);
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
                        currentArea = pom.Substring(4);
                        currentRow = split[6];
                        currentBays = split[7];
                        currentLocation = split[8];
                        currentLevel = split[9];

                        //Takto by to mělo být podle dokumentace
                        if (split.Length == 11)
                        {
                            currentArea = split[6];
                            currentRow = split[7];
                            currentBays = split[8];
                            currentLocation = split[9];
                            currentLevel = split[10];
                        }
                        Operation = telegram.Substring(4, 1);
                        NominalPosition = nominalArea + " " + nominalRow + "-" + nominalLevel + "-" + nominalLocation;
                        Status = fault;
                        if (!StatusConfirmed)
                        {
                            StatusConfirmed = true;
                        }
                        else
                        {
                            SendTelegram("Q", "s!", "");
                        }
                        break;
                    }
                case "b":
                    {
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
                    }
                case "q":
                    {
                        string replyCommand = telegram.Substring(4, 1);
                        string replyResult = telegram.Substring(5, 1);
                        if (replyResult == "!")
                        {
                            switch (replyCommand)
                            {
                                case "F":
                                    OrderConfirmed = true;
                                    break;
                            }
                        }
                        break;
                    }
                default:
                    throw new ApplicationException("Nepodporovaný typ telegramu.");
            }

            if (!String.IsNullOrEmpty(currentArea))
            {

                //currentLevel = (Convert.ToUInt32(currentLevel) - 1).ToString("00");

                string s = currentRow + "-" + currentLevel + "-" + currentLocation;
                CurrentPosition = currentArea + " " + s;
                if (Regex.Match(s, @"^([0-9][0-9])-[0-9][0-9]-[0-9][0-9]$").Success)
                    AktualniPozice = CurrentPosition;
            }
        }

        private void TerminalWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (VybranyUkolVNA > 0)
            {
                if (MessageBox.Show("Chcete aplikaci opravdu ukončit, přestože máte rozpracovaný VNA úkol?", "Dotaz", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                 }
            }

            _Timer.Stop();
            if (Globals.SgConnector != null && Globals.SgConnector.LoggedOn)
            {
                if (HeliosVNA > 0)
                    try
                    {
                        InputTable inputTable = new InputTable("InputParams");
                        inputTable.AddColumn("command_key", typeof(string));
                        inputTable.AddColumn("vozik", typeof(int));
                        int row = inputTable.AddRow();
                        inputTable.SetItem(0, "command_key", "odhlaseni_voziku");
                        inputTable.SetItem(0, "vozik", HeliosVNA);
                        try
                        {
                            RunFunctionResponse response = _VnaClientRequest(inputTable);
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show("Nepodařilo se zjistit spárování vozíku v Heliosu: " + ex.Message);
                        }

                    }
                    catch
                    {

                    }

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
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _ResetStatus();
                if (_SerialPort.IsOpen)
                {
                    BtnConnect.Content = "Odpojit";
                    BtnConnect.Foreground = Brushes.Black;
                    cbxComPorts.IsEnabled = false;
                    if (!_HandShakeS())
                        MessageBox.Show(Status);
                    if (!_HandShakeB())
                        MessageBox.Show(Status);
                }
                else
                {
                    BtnConnect.Content = "Připojit";
                    BtnConnect.Foreground = Brushes.Red;
                    cbxComPorts.IsEnabled = true;
                    Status = "Odpojeno";
                }
            }
        }

        private async void StatusRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_HandShakeS())
                MessageBox.Show(Status);
            await Task.Delay(200);
            if (!_HandShakeB())
                MessageBox.Show(Status);
        }

        private bool _HandShakeS()
        {
            bool ret = false;
            try
            {
                SendTelegram("C", "S", "");
                ret = true;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            return ret;
        }

        private bool _HandShakeB()
        {
            bool ret = false;
            try
            {
                SendTelegram("C", "B", "");
                ret = true;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
            return ret;
        }

        private void SendTelegram(string typ, string action, string parametry)
        {
            string telegram = typ + action;
            if (!String.IsNullOrWhiteSpace(parametry))
                telegram += ";" + parametry;
            telegram = "<" + (telegram.Length + 4).ToString("00") + telegram + ">";
            if (_SerialPort.IsOpen)
            {
                _SerialPort.Write(telegram);
                switch (typ)
                {
                    case "C":
                        StatusConfirmed = false;
                        break;
                    case "F":
                        OrderConfirmed = false;
                        break;
                }
            }
            else
                throw new ApplicationException("Seriový port není otevřen.");

            tbTelegramy.Inlines.Add(new Run(telegram + "\r\n") { Foreground = Brushes.Blue });
            TelegramyScrollViewer.ScrollToEnd();
        }

        //private void BtnClearOutputBox_Click(object sender, RoutedEventArgs e)
        //{
        //    OutputTextBox.Clear();
        //}

        private void BtnClearTelegramy_Click(object sender, RoutedEventArgs e)
        {
            tbTelegramy.Text = "";
        }

        private void CbxComPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LastComPort = cbxComPorts.Items[cbxComPorts.SelectedIndex].ToString();
            lblComPorts.Foreground = Brushes.Black;
        }

        private void CbxHeliosVNA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeliosVNA == 0 && cbxHeliosVNA.SelectedValue != null)
            {
                Int32 vozik = (Int32)cbxHeliosVNA.SelectedValue;
                _KontrolaSparovani(vozik, SynchroID);
            }
        }

        private void _KontrolaSparovani(int vozik, string sid)
        {
            InputTable inputTable = new InputTable("InputParams");
            inputTable.AddColumn("command_key", typeof(string));
            inputTable.AddColumn("vozik", typeof(int));
            inputTable.AddColumn("new_sid", typeof(string));
            int row = inputTable.AddRow();
            inputTable.SetItem(0, "command_key", "kontrola_sparovani");
            inputTable.SetItem(0, "vozik", vozik);
            inputTable.SetItem(0, "new_sid", sid);
            try
            {
                RunFunctionResponse response = _VnaClientRequest(inputTable);
                HeliosVNA = vozik;

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
                MessageBox.Show("Chyba spárování vozíku v Heliosu: " + ex.Message);
                HeliosVNA = 0;
                NactiVozikyProParovani();
            }
        }

        private void NactiVozikyProParovani()
        {

            InputTable inputTable = new InputTable("InputParams");
            inputTable.AddColumn("command_key", typeof(string));
            int row = inputTable.AddRow();
            inputTable.SetItem(0, "command_key", "get_free_vna_list");

            try
            {
                RunFunctionResponse response = _VnaClientRequest(inputTable);
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

        private RunFunctionResponse _VnaClientRequest(InputTable inputTable)
        {
            RunFunctionRequest request = new RunFunctionRequest();
            request.Function.FunctionId = 23480; //  VNA client request
            request.UserData = new FunctionUserData();
            List<InputTable> inputTables = new List<InputTable>();
            inputTables.Add(inputTable);
            request.UserData.SetDatastores<InputTable>(inputTables);
            RunFunctionResponse response = request.Process(Globals.SgConnector);
            bool failure = response.State == ResponseState.Failure;
            if (failure)
            {
                if (response.Error.Level == ErrorLevel.Application)
                {
                    throw new Exception(response.Error.Message);
                }
                else
                {
                    throw new Exception(response.Error.ToString());
                }
            }
            return response;
        }

        private void _ParsePozice(string pozice, out string oblast, out string rada, out string uroven, out string regal)
        {
            oblast = rada = uroven = regal = "";

            Regex r = new Regex(@"^(?<oblast>[AB]) (?<rada>[0-9][0-9])-(?<uroven>[0-9][0-9])-(?<regal>[0-9][0-9])$", RegexOptions.Multiline | RegexOptions.Compiled);
            Match m = r.Match(pozice);
            if (m.Success)
            {
                oblast = m.Groups["oblast"].Value;
                rada = m.Groups["rada"].Value;
                uroven = m.Groups["uroven"].Value;
                //uroven = (Convert.ToInt32( m.Groups["uroven"].Value) + 1).ToString("00");
                regal = m.Groups["regal"].Value;
            }
            else
                throw new ApplicationException("Nepodporovaná regálová pozice.");
        }

        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            if (_HandShakeS())
            {
                InputTable inputTable = new InputTable("InputParams");
                inputTable.AddColumn("command_key", typeof(string));
                inputTable.AddColumn("vozik", typeof(int));
                inputTable.AddColumn("novy_ukol_vna", typeof(int));
                inputTable.AddColumn("vybrany_ukol_vna", typeof(int));
                int row = inputTable.AddRow();
                inputTable.SetItem(0, "command_key", "default_click");
                inputTable.SetItem(0, "vozik", HeliosVNA);
                inputTable.SetItem(0, "novy_ukol_vna", NovyUkolVNA);
                inputTable.SetItem(0, "vybrany_ukol_vna", VybranyUkolVNA);
                try
                {
                    _Timer.Stop();
                    RunFunctionResponse response = _VnaClientRequest(inputTable);
                    _NactiUkol();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _Timer.Start();
                }
            }
            else
                MessageBox.Show(Status);
        }

        private void _PrekresliUkol()
        {
            Brush aktualniPoziceBrush = Brushes.Black;
            Brush odkudBrush = Brushes.Black;
            Brush kamBrush = Brushes.Black;

            Visibility lblAktualniPoziceVisibility = Visibility.Hidden; 
            Visibility lblAktualniPoziceValVisibility = Visibility.Hidden;
            Visibility lblOdkudVisibility = Visibility.Hidden;
            Visibility lblOdkudValVisibility = Visibility.Hidden;
            Visibility lblKamVisibility = Visibility.Hidden;
            Visibility lblOdKamValVisibility = Visibility.Hidden;
            Visibility lblPrioritaVisibility = Visibility.Hidden;
            Visibility lblPrioritaValVisibility = Visibility.Hidden;
            Visibility lblBaleniVisibility = Visibility.Hidden;
            Visibility lblBaleniValVisibility = Visibility.Hidden;

            Visibility btnDefaultVisibility = Visibility.Hidden;
            Visibility btnChybaVisibility = Visibility.Collapsed;
            Visibility btnNenalezenoVisibility = Visibility.Collapsed;
            Visibility btnVynechatVisibility = Visibility.Collapsed;
            Visibility btnZrusitVisibility = Visibility.Collapsed;
            Visibility btnObsazenoVisibility = Visibility.Collapsed;

            if (!String.IsNullOrEmpty(StavUkoluVNA))
            {
                lblAktualniPoziceVisibility = Visibility.Visible;
                lblAktualniPoziceValVisibility = Visibility.Visible;
                lblOdkudVisibility = Visibility.Visible;
                lblOdkudValVisibility = Visibility.Visible;
                lblKamVisibility = Visibility.Visible;
                lblOdKamValVisibility = Visibility.Visible;
                lblPrioritaVisibility = Visibility.Visible;
                lblPrioritaValVisibility = Visibility.Visible;
                lblBaleniVisibility = Visibility.Visible;
                lblBaleniValVisibility = Visibility.Visible;

                btnDefaultVisibility = Visibility.Visible;
                btnChybaVisibility = Visibility.Visible;
            }

            switch (StavUkoluVNA)
            {
                case "P": //Plán
                    btnVynechatVisibility = Visibility.Visible;
                    btnChybaVisibility = Visibility.Collapsed;
                    break;
                case "Z": // Zahájeno
                    break;
                case "N": // Nakládání
                    //btnNenalezenoVisibility = Visibility.Visible;
                    btnZrusitVisibility = Visibility.Visible;
                    if (lblAktualniPoziceVal.Content.ToString() == lblOdkudVal.Content.ToString())
                    {
                        aktualniPoziceBrush = Brushes.Green;
                        odkudBrush = Brushes.Green;
                    }
                    else
                    {
                        aktualniPoziceBrush = Brushes.Red;
                        odkudBrush = Brushes.Red;
                    }
                    break;
                case "V": // Vykládání
                    //btnObsazenoVisibility = Visibility.Visible;
                    if (lblAktualniPoziceVal.Content.ToString() == lblKamVal.Content.ToString())
                    {
                        aktualniPoziceBrush = Brushes.Green;
                        kamBrush = Brushes.Green;
                    }
                    else
                    {
                        aktualniPoziceBrush = Brushes.Red;
                        kamBrush = Brushes.Red;
                    }
                    break;
                case "K": // Kontrola
                    //btnNenalezenoVisibility = Visibility.Visible;
                    break;
            }

            lblAktualniPoziceVal.Foreground = aktualniPoziceBrush;
            lblOdkudVal.Foreground = odkudBrush;
            lblKamVal.Foreground = kamBrush;

            lblAktualniPozice.Visibility = lblAktualniPoziceVisibility;
            lblAktualniPoziceVal.Visibility = lblAktualniPoziceValVisibility;
            lblOdkud.Visibility = lblOdkudVisibility;
            lblOdkudVal.Visibility = lblOdkudValVisibility;
            lblKam.Visibility = lblKamVisibility;
            lblKamVal.Visibility = lblOdKamValVisibility;
            lblPriorita.Visibility = lblPrioritaVisibility;
            lblPrioritaVal.Visibility = lblPrioritaValVisibility;
            lblBaleni.Visibility = lblBaleniVisibility;
            lblBaleniVal.Visibility = lblBaleniValVisibility;

            btnDefault.Visibility = btnDefaultVisibility;
            btnChyba.Visibility = btnChybaVisibility;
            btnNenalezeno.Visibility = btnNenalezenoVisibility;
            btnVynechat.Visibility = btnVynechatVisibility;
            btnZrusit.Visibility = btnZrusitVisibility;
            btnObsazeno.Visibility = btnObsazenoVisibility;
        }

        private void BtnDefault_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Button b = (Button)sender;
            if (b.IsEnabled)
                b.Foreground = Brushes.Green;
            else
                b.Foreground = Brushes.LightGray;
                
        }

        private void LblStavUkolu_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _NactiUkol();
        }

        private void CbFullScreen_Checked(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
            Visibility = Visibility.Visible;
        }

        private void CbFullScreen_Unchecked(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
        }

        private void BtnUkoncit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TerminalWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void BtnChyba_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Opravdu chcete aktuální úkol ukončit chybovým stavem?", "Dotaz", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                InputTable inputTable = new InputTable("InputParams");
                inputTable.AddColumn("command_key", typeof(string));
                inputTable.AddColumn("vozik", typeof(int));
                inputTable.AddColumn("vybrany_ukol_vna", typeof(int));
                int row = inputTable.AddRow();
                inputTable.SetItem(0, "command_key", "chyba_click");
                inputTable.SetItem(0, "vozik", HeliosVNA);
                inputTable.SetItem(0, "vybrany_ukol_vna", VybranyUkolVNA);
                try
                {
                    _Timer.Stop();
                    RunFunctionResponse response = _VnaClientRequest(inputTable);
                    _NactiUkol();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _Timer.Start();
                }
            }
        }

        private void BtnZrusit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Opravdu chcete zrušit aktuální úkol?", "Dotaz", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                InputTable inputTable = new InputTable("InputParams");
                inputTable.AddColumn("command_key", typeof(string));
                inputTable.AddColumn("vozik", typeof(int));
                inputTable.AddColumn("vybrany_ukol_vna", typeof(int));
                int row = inputTable.AddRow();
                inputTable.SetItem(0, "command_key", "zrusit_click");
                inputTable.SetItem(0, "vozik", HeliosVNA);
                inputTable.SetItem(0, "vybrany_ukol_vna", VybranyUkolVNA);
                try
                {
                    _Timer.Stop();
                    RunFunctionResponse response = _VnaClientRequest(inputTable);
                    _NactiUkol();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _Timer.Start();
                }
            }
        }

        private void BtnVynechat_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Opravdu chcete vynechat aktuální úkol?", "Dotaz", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                InputTable inputTable = new InputTable("InputParams");
                inputTable.AddColumn("command_key", typeof(string));
                inputTable.AddColumn("vozik", typeof(int));
                inputTable.AddColumn("novy_ukol_vna", typeof(int));
                int row = inputTable.AddRow();
                inputTable.SetItem(0, "command_key", "vynechat_click");
                inputTable.SetItem(0, "vozik", HeliosVNA);
                inputTable.SetItem(0, "novy_ukol_vna", NovyUkolVNA);
                try
                {
                    _Timer.Stop();
                    RunFunctionResponse response = _VnaClientRequest(inputTable);
                    _NactiUkol();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _Timer.Start();
                }
            }
        }
    }
}
