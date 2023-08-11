using System;
using System.Collections.Generic;
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

namespace LindeVNA
{
    /// <summary>
    /// Interaction logic for TerminalVNA.xaml
    /// </summary>
    public partial class TerminalVNA : Window
    {
        private SerialPort _SerialPort;
        String _DataBuffer = "";

        public TerminalVNA()
        {
            _SerialPort = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            _SerialPort.DataReceived += _SerialPort_DataReceived;
            InitializeComponent();
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
                MessageBox.Show("Nepodařilo se zjistit délku telegramu: " + ex.Message);
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
                    lblAkce.Content = "Akce: " + lastOrder;
                    lblAktualniPozice.Content = "Aktuální pozice: " + currentArea + " " + currentRow + "-" + currentLevel + "-" + currentLocation;
                    lblCilovaPozice.Content = "Cílová pozice: " + nominalArea + " " + nominalRow + "-" + nominalLevel + "-" + nominalLocation;
                    lblStatus.Content = fault;
                    if (fault == "000")
                        lblStatus.Foreground = Brushes.Green;
                    else
                        lblStatus.Foreground = Brushes.Red;
                    break;
                case "b":
                    break;
                default:
                        throw new ApplicationException("Nepodporovaný typ telegramu.");
                    break;
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
            Application.Current.MainWindow.Show();
        }

        private void TerminalWindow_Activated(object sender, EventArgs e)
        {
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
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
                    ((Button)sender).Content = "Disconnect";
                else
                    ((Button)sender).Content = "Connect";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StatusRequestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_SerialPort.IsOpen)
                    _SerialPort.Write("<06CS>");
                else
                    MessageBox.Show("PortClosed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
    }
}
