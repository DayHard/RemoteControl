using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RemoteControl
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SerialPort _port;
        private bool _portIsOpen;
        private readonly Multimedia.Timer _timer;
        private short _command, _shift;
        private ushort _counter;
        private List<ToggleButton> _tlgBtnList;
        private const int Delay = 50;
        private const int RecieveTimeOut = 50;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private byte[] _package = {0x5a, 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        public MainWindow()
        {
            InitializeComponent();
            // Добавление кнопок в коллекцию объектов
            CreateTlgBtnCollection();
            // Создание таймера
            _timer = new Multimedia.Timer
            {
                Resolution = 1,
                Period = Delay
            };
            _timer.Started += _timer_Started;
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Started(object sender, EventArgs e)
        {
            // Сбрасываем счетчик пакетов
            _counter = 1;
            //По имени кнопки определяем какую команду отправлять
            foreach (var btn in _tlgBtnList)
            {
                if (!btn.IsEnabled) continue;
                switch (btn.Name)
                {
                    case "TglBtnZUp":
                        _command = 0x0101;
                        break;
                    case "TglBtnZDown":
                        _command = 0x0102;
                        break;
                    case "TglBtnYLeft":
                        _command = 0x0201;
                        break;
                    case "TglBtnYRight":
                        _command = 0x0202;
                        break;
                    case "TglBtnZCentre":
                        _command = 0x0103;
                        break;
                    case "TglBtnYCentre":
                        _command = 0x0203;
                        break;
                    case "TglBtnZCentreDelta":
                        _command = 0x0103;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnYInCentreDelta":
                        _command = 0x0203;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnZInStartDelta":
                        _command = 0x0104;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnStartScanZDelta":
                        _command = 0x0105;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnYStartScanDelta":
                        _command = 0x0204;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnStartScanYDelta":
                        _command = 0x0205;
                        _shift = Convert.ToInt16(TbShift.Text);
                        break;
                    case "TglBtnBpoStartInY":
                        _command = 0x0301;
                        break;
                    case "TglBtnBpoStartInZ":
                        _command = 0x0302;
                        break;
                    default:
                        throw new Exception("Something gone wrong.");
                }
            }
        }
        private void _timer_Tick(object sender, EventArgs e)
        {
            DataExchange(_command,_counter, _shift);
            _counter++;
            if (_counter == 65535) _counter = 1;
        }
        //Создаем коллекцию кнопок
        private void CreateTlgBtnCollection()
        {
            _tlgBtnList = new List<ToggleButton>
            {
                TglBtnZUp,
                TglBtnZDown,
                TglBtnYLeft,
                TglBtnYRight,
                TglBtnZCentre,
                TglBtnYCentre,
                TglBtnZCentreDelta,
                TglBtnYInCentreDelta,
                TglBtnZInStartDelta,
                TglBtnStartScanZDelta,
                TglBtnYStartScanDelta,
                TglBtnStartScanYDelta,
                TglBtnBpoStartInY,
                TglBtnBpoStartInZ
            };

        }
        // Определяем версию ПО
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
                    _port.ReadTimeout = RecieveTimeOut;
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
        /// <summary>
        /// Посылает указанную команду по протоколу ПК232
        /// </summary>
        /// <param name="command">Команда управления</param>
        /// <param name="pcount">Счетчик пакетов</param>
        /// <param name="shift">Величина смещения в отсчетах</param>
        private void SendCommand(short command, ushort pcount, int shift)
        {
            // Формируем команду  управления
            var ccom = BitConverter.GetBytes(command);
            _package[2] = ccom[1];
            _package[3] = ccom[0];

            // Заполняем счетчик пакетов
            var cpcount = BitConverter.GetBytes(pcount);
            _package[13] = cpcount[1];
            _package[14] = cpcount[0];

            // Заполняем велечину смещения
            var cshift = BitConverter.GetBytes(shift);
            _package[4] = cshift[2];
            _package[5] = cshift[1];
            _package[6] = cshift[0];

            // Расчитываем контрольную сумму
            byte checksum = 0;
            for (int i = 0; i < _package.Length - 1; i++)
            {
                checksum ^= _package[i];
            }
            _package[15] = checksum;

            // Отправляем команду
            _port.Write(_package, 0, _package.Length);
        }
        private void RecieveCommand()
        {
            byte[] responce = new byte[16];

            try
            {
                _port.Read(responce, 0, responce.Length);
            }
            catch (TimeoutException)
            {
                Dispatcher.Invoke(() => { LbOperationsStatus.Items.Add("[" + DateTime.Now + "] " + "Устроство не ответило на запрос."); });
                return;
            }

            byte checksum = 0;
            // Считаем контрольную сумму
            for (int i = 0; i < responce.Length - 1; i++)
            {
                checksum ^= responce[i];
            }
            if (responce[15] != checksum)
                Dispatcher.Invoke(() => { LbOperationsStatus.Items.Add("[" + DateTime.Now + "] " + "Ошибка контрольной суммы пакета"); }); 
            // Проверяем маркер начала пакета
            if (responce[0] != 0xA5 || responce[1] != 0x10)
            {
                Dispatcher.Invoke(() => { LbOperationsStatus.Items.Add("[" + DateTime.Now + "] " + "Ошибка маркера начала пакета"); }); 
                return;
            }

            var status = responce[2] | responce[3] << 8;
            // Установка индикаторов состояния
            Dispatcher.Invoke(() =>
            {
                // Установка статусов системы согалсно полученным битам
                if ((status & 0x01) == 1)
                    IndicReady.Background = Brushes.Green;
                else IndicReady.Background = Brushes.OrangeRed;

                if ((status & 0x02) == 1)
                    IndicStopZ.Background = Brushes.Green;
                else IndicStopZ.Background = Brushes.OrangeRed;

                if ((status & 0x04) == 1)
                    IndicStopY.Background = Brushes.Green;
                else IndicStopY.Background = Brushes.OrangeRed;

                if ((status & 0x08) == 1)
                    IndicCentreX.Background = Brushes.Green;
                else IndicCentreX.Background = Brushes.OrangeRed;

                if ((status & 0x10) == 1)
                    IndicCentreY.Background = Brushes.Green;
                else IndicCentreY.Background = Brushes.OrangeRed;

                if ((status & 0x20) == 1)
                    IndicStartX.Background = Brushes.Green;
                else IndicStartX.Background = Brushes.OrangeRed;

                if ((status & 0x40) == 1)
                    IndicStartY.Background = Brushes.Green;
                else IndicStartY.Background = Brushes.OrangeRed;

                if ((status & 0x80) == 1)
                    IndicErr.Background = Brushes.Green;
                else IndicErr.Background = Brushes.OrangeRed;

                // Величина отсчета по координате Z- старший байт вперед
                TbZCd.Text = (responce[6] | responce[5] << 8 | responce[4] << 16).ToString();
                // Величина отсчета по координате Y- старший байт вперед
                TbYCd.Text = (responce[9] | responce[8] << 8 | responce[7] << 16).ToString();
                // Код ошибки
                if ((status & 0x80) == 1)
                    TbYCd.Text = responce[12].ToString();
                // Счетчик пакетов
                TbCPg.Text = (responce[14] | responce[13] << 8).ToString();
            });
        }
        //По нажатию на кнопку, отключаем остальные кнопки
        private void TglBtn_Click(object sender, RoutedEventArgs e)
        {
            var prdBtn = (ToggleButton)sender;
            foreach (var btn in _tlgBtnList)
            {
                if (btn.Name != prdBtn.Name)
                    btn.IsEnabled = !btn.IsEnabled;
            }
            // Запуск\Остановка таймера
            if(_timer.IsRunning)
                _timer.Stop();
            else _timer.Start();
        }
        /// <summary>
        /// Обмен данными с устройством
        /// </summary>
        /// <param name="command">Команда управления</param>
        /// <param name="count">Номер пакета</param>
        /// <param name="shift">Смещение(для некоторых команд)</param>
        private void DataExchange(short command, ushort count, short shift)
        {
            SendCommand(command, count, shift);
            RecieveCommand();
        }
    }
}
