using System;
using System.IO.Ports;
using System.Reflection;
using System.Windows;

namespace RemoteControl
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SerialPort _port;
        private bool _portIsOpen;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Считываем версию ПО
            LbVersion.Content = "Версия: " + Assembly.GetExecutingAssembly().GetName().Version;
            // Считываем доступные COM порты
            foreach (var portName in SerialPort.GetPortNames())
            {
                CbPortName.Items.Add(portName);
            }
        }
        //Подключение по COM
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!_portIsOpen)
            {
                _port = new SerialPort();

                try
                {
                    _port.PortName = Convert.ToString(CbPortName.SelectionBoxItem);
                    _port.BaudRate = Convert.ToInt32(CbBaudRate.SelectionBoxItem);
                    _port.StopBits = StopBits.One;
                    _port.DataBits = 8;
                    _port.StopBits = StopBits.One;
                    _port.Parity = Parity.Even;
                    _port.Open();

                    BtnConnect.Content = "Отключить";
                    LbConnectionStatus.Content = "Статус: Подключено";

                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                _port.Close();
                BtnConnect.Content = "Подключить";
                LbConnectionStatus.Content = "Статус: Отключено";
            }

            _portIsOpen = !_portIsOpen;
            CbPortName.IsEnabled = !CbPortName.IsEnabled;
            CbBaudRate.IsEnabled = !CbBaudRate.IsEnabled;
        }
    }
}
