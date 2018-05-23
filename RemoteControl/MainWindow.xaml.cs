using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace RemoteControl
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SerialPort _port;
        private readonly Multimedia.Timer _timer;
        private short _command;
        private int _shift;
        private ushort _counter;
        private List<ToggleButton> _tlgBtnList;
        private int _errchecksum;
        private int _errtimeout;
        private bool _comwithshift;
        private const int Delay = 1000;
        private const int RecieveTimeOut = 100;
        private readonly byte[] _package = {0x5a, 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
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
            _timer.Tick += _timer_Tick;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Считываем версию ПО
            LbVersion.Content = "Версия: " + Assembly.GetExecutingAssembly().GetName().Version;
            // Считываем доступные COM порты
            foreach (var portName in SerialPort.GetPortNames())
            {
                CbPortName.Items.Add(portName);
            }
            // Загружаем файл конфигурации
            LoadProperties();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Остановливаем обмен
            _timer.Stop();
            // Закрываем порт
            if (_port != null && _port.IsOpen)
                _port.Close();
            // Сохраняем информацию о сдвигах
            SaveShiftFields();
        }
        // Сохранение полей в XML файл
        private void SaveShiftFields()
        {
            XmlTextWriter writer = new XmlTextWriter("setting.xml", Encoding.UTF8);
            writer.WriteStartDocument();
            writer.WriteStartElement("xml");
            writer.WriteEndElement();
            writer.Close();

            XmlDocument doc = new XmlDocument();
            doc.Load("setting.xml");
            XmlNode header = doc.CreateElement("RemoteControl");
            doc.DocumentElement?.AppendChild(header);

            XmlNode element1 = doc.CreateElement("Com-Properties");
            doc.DocumentElement?.AppendChild(element1);

            XmlNode portname = doc.CreateElement("PortName"); // даём имя
            portname.InnerText = CbPortName.SelectionBoxItem.ToString(); // и значение
            element1.AppendChild(portname); // и указываем кому принадлежит

            XmlNode baudrate = doc.CreateElement("BaudRate"); // даём имя
            baudrate.InnerText = CbBaudRate.SelectionBoxItem.ToString(); // и значение
            element1.AppendChild(baudrate); // и указываем кому принадлежит

            XmlNode element2 = doc.CreateElement("Shift-Properties");
            doc.DocumentElement?.AppendChild(element2);

            XmlNode startScanYDelta = doc.CreateElement("StartScanYDelta"); // даём имя
            startScanYDelta.InnerText = TbStartScanYDelta.Text; // и значение
            element2.AppendChild(startScanYDelta); // и указываем кому принадлежит

            XmlNode yInCentreDelta = doc.CreateElement("YInCentreDelta"); // даём имя
            yInCentreDelta.InnerText = TbYInCentreDelta.Text; // и значение
            element2.AppendChild(yInCentreDelta); // и указываем кому принадлежит

            XmlNode yStartScanDelta = doc.CreateElement("YStartScanDelta"); // даём имя
            yStartScanDelta.InnerText = TbYStartScanDelta.Text; // и значение
            element2.AppendChild(yStartScanDelta); // и указываем кому принадлежит

            XmlNode startScanZDelta = doc.CreateElement("StartScanZDelta"); // даём имя
            startScanZDelta.InnerText = TbStartScanZDelta.Text; // и значение
            element2.AppendChild(startScanZDelta); // и указываем кому принадлежит

            XmlNode zInStartDelta = doc.CreateElement("ZInStartDelta"); // даём имя
            zInStartDelta.InnerText = TbZInStartDelta.Text; // и значение
            element2.AppendChild(zInStartDelta); // и указываем кому принадлежит

            XmlNode zCentreDelta = doc.CreateElement("ZCentreDelta"); // даём имя
            zCentreDelta.InnerText = TbZCentreDelta.Text; // и значение
            element2.AppendChild(zCentreDelta); // и указываем кому принадлежит

            doc.Save("setting.xml");
        }
        // Загрузка полей из XML
        private void LoadProperties()
        {
            if (File.Exists("setting.xml"))
            {               
                XDocument xDoc = XDocument.Load("setting.xml");
                // Считывание параметров соединения
                XElement portName = xDoc.Descendants("PortName").First();
                for (int i = 0; i < CbPortName.Items.Count; i++)
                {
                    if (portName.Value == (string)CbPortName.Items[i])
                        CbPortName.SelectedIndex = i;
                }

                XElement baudrate = xDoc.Descendants("BaudRate").First();
                for (int i = 0; i < CbBaudRate.Items.Count; i++)
                {
                    if (baudrate.Value == (string)CbBaudRate.Items[i])
                        CbBaudRate.SelectedIndex = i;
                }

                // Считывание сдвигов
                XElement startScanYDelta = xDoc.Descendants("StartScanYDelta").First();
                TbStartScanYDelta.Text = int.TryParse(startScanYDelta.Value, out int _) ? startScanYDelta.Value : 0.ToString();

                XElement yInCentreDelta = xDoc.Descendants("YInCentreDelta").First();
                TbYInCentreDelta.Text = int.TryParse(yInCentreDelta.Value, out int _) ? yInCentreDelta.Value : 0.ToString();

                XElement yStartScanDelta = xDoc.Descendants("YStartScanDelta").First();
                TbYStartScanDelta.Text = int.TryParse(yStartScanDelta.Value, out int _) ? yStartScanDelta.Value : 0.ToString();

                XElement startScanZDelta = xDoc.Descendants("StartScanZDelta").First();
                TbStartScanZDelta.Text = int.TryParse(startScanZDelta.Value, out int _) ? startScanZDelta.Value : 0.ToString();

                XElement zInStartDelta = xDoc.Descendants("ZInStartDelta").First();
                TbZInStartDelta.Text = int.TryParse(zInStartDelta.Value, out int _) ? zInStartDelta.Value : 0.ToString();

                XElement zCentreDelta = xDoc.Descendants("ZCentreDelta").First();
                TbZCentreDelta.Text = int.TryParse(zCentreDelta.Value, out int _) ? zCentreDelta.Value : 0.ToString();
            }
            else SetDefaultConfig();
        }
        /// <summary>
        /// Установка свойств и кнопок в положение по умолчанию
        /// </summary>
        private void SetDefaultConfig()
        {
            CbPortName.SelectedIndex = -1;
            CbBaudRate.SelectedIndex = -1;
            TbStartScanYDelta.Text = 0.ToString();
            TbYInCentreDelta.Text = 0.ToString();
            TbYStartScanDelta.Text = 0.ToString();
            TbStartScanZDelta.Text = 0.ToString();
            TbZInStartDelta.Text = 0.ToString();
            TbZInStartDelta.Text = 0.ToString();
        }

        // Сброс ошибки таймат, при двойном клике на поле
        private void TbTimeoutErr_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _errtimeout = 0;
            TbTimeoutErr.Text = _errtimeout.ToString();
        }
        // Сброс ошибки контрольной суммы, при двойном клике на поле
        private void TbChecksumErr_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _errchecksum = 0;
            TbChecksumErr.Text = _errchecksum.ToString();
        }
        // Включение кнопки подключить
        private void CbPortName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnConnect.IsEnabled = true;
        }
        // Такт срабатывания таймера
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
        //Подключение по COM
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_port == null || !_port.IsOpen)
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
                    _timer.Start();

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
                // В отключения переподключения сбрасываем команду
                _command = 0x00;
                _timer.Stop();
                _port.Close();
                BtnConnect.Content = "Подключить";
                LbConnectionStatus.Content = "Статус: Отключено";
            }
            // Включаем кнопки управления
            foreach (var toggleButton in _tlgBtnList)
            {
                toggleButton.IsEnabled = !toggleButton.IsEnabled;
                toggleButton.IsChecked = false;
            }
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
                _errtimeout++;
                Dispatcher.Invoke(() => { TbTimeoutErr.Text = _errtimeout.ToString(); });
                return;
            }
            catch (IOException) {return;}

            byte checksum = 0;
            // Считаем контрольную сумму
            for (int i = 0; i < responce.Length - 1; i++)
            {
                checksum ^= responce[i];
            }
            if (responce[15] != checksum)
            {
                Dispatcher.Invoke(() => { TbChecksumErr.Text = _errchecksum.ToString(); });
                _errchecksum++;
            }
            // Проверяем маркер начала пакета
            if (responce[0] != 0xA5 || responce[1] != 0x10)
            {
                Dispatcher.Invoke(() => { TbChecksumErr.Text = _errchecksum.ToString(); });
                _errchecksum++;
            }

            //var status = responce[2] | responce[3] << 8;
            var status = responce[2] << 8 | responce[3];
            // Установка индикаторов состояния
            Dispatcher.Invoke(() =>
            {
                // Установка статусов системы согалсно полученным битам
                if ((status & 0x01) != 0)
                    IndicReady.Background = Brushes.GreenYellow;
                else IndicReady.Background = Brushes.OrangeRed;

                if ((status & 0x02) != 0)
                    IndicStopZ.Background = Brushes.GreenYellow;
                else IndicStopZ.Background = Brushes.OrangeRed;

                if ((status & 0x04) != 0)
                    IndicStopY.Background = Brushes.GreenYellow;
                else IndicStopY.Background = Brushes.OrangeRed;

                if ((status & 0x08) != 0)
                    IndicCentreX.Background = Brushes.GreenYellow;
                else IndicCentreX.Background = Brushes.OrangeRed;

                if ((status & 0x10) != 0)
                    IndicCentreY.Background = Brushes.GreenYellow;
                else IndicCentreY.Background = Brushes.OrangeRed;

                if ((status & 0x20) != 0)
                    IndicStartX.Background = Brushes.GreenYellow;
                else IndicStartX.Background = Brushes.OrangeRed;

                if ((status & 0x40) != 0)
                    IndicStartY.Background = Brushes.GreenYellow;
                else IndicStartY.Background = Brushes.OrangeRed;

                if ((status & 0x80) != 0)
                    IndicErr.Background = Brushes.GreenYellow;
                else IndicErr.Background = Brushes.OrangeRed;

                // Величина отсчета по координате Z- старший байт вперед
                TbZCd.Text = (responce[6] | responce[5] << 8 | responce[4] << 16).ToString();
                // Величина отсчета по координате Y- старший байт вперед
                TbYCd.Text = (responce[9] | responce[8] << 8 | responce[7] << 16).ToString();
                // Время сканирование
                // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                TbScanTime.Text = ((short)(responce[10] << 8 | responce[11])*Math.Pow(10, -3)).ToString();
                // Код ошибки
                if ((status & 0x80) != 0)
                    TbCErr.Text = responce[12].ToString();
                // Счетчик пакетов
                TbCPg.Text = (responce[14] | responce[13] << 8).ToString();
            });
        }
        //По нажатию на кнопку, отключаем остальные кнопки
        private void TglBtn_Click(object sender, RoutedEventArgs e)
        {
            // Снимаем нажатие с кнопки если нажато более 1ой
            var prdBtn = (ToggleButton)sender;
            foreach (var btn in _tlgBtnList)
            {
                if (prdBtn.IsChecked == true && prdBtn.Name != btn.Name)
                    btn.IsChecked = false;
            }

            // В случае если все кнопки отжаты, шлем команду 0x0000
            foreach (var btn in _tlgBtnList)
            {
                if (btn.IsChecked != false) continue;
                _command = 0x0000;
                _comwithshift = false;
            }

            //По статусу кнопки определяем какую команду отправлять
            foreach (var btn in _tlgBtnList)
            {
                if (btn.IsChecked != true) continue;
                switch (btn.Name)
                {
                    case "TglBtnZUp":
                        _command = 0x0101;
                        _comwithshift = false;
                        break;
                    case "TglBtnZDown":
                        _command = 0x0102;
                        _comwithshift = false;
                        break;
                    case "TglBtnYLeft":
                        _command = 0x0201;
                        _comwithshift = false;
                        break;
                    case "TglBtnYRight":
                        _command = 0x0202;
                        _comwithshift = false;
                        break;
                    case "TglBtnZCentre":
                        _command = 0x0103;
                        _comwithshift = false;
                        break;
                    case "TglBtnYCentre":
                        _command = 0x0203;
                        _comwithshift = false;
                        break;
                    case "TglBtnZCentreDelta":
                        _command = 0x0103;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbZCentreDelta.Text);
                        break;
                    case "TglBtnYInCentreDelta":
                        _command = 0x0203;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbYInCentreDelta.Text);
                        break;
                    case "TglBtnZInStartDelta":
                        _command = 0x0104;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbZInStartDelta.Text);
                        break;
                    case "TglBtnStartScanZDelta":
                        _command = 0x0105;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbStartScanZDelta.Text);
                        break;
                    case "TglBtnYStartScanDelta":
                        _command = 0x0204;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbYStartScanDelta.Text);
                        break;
                    case "TglBtnStartScanYDelta":
                        _command = 0x0205;
                        _comwithshift = true;
                        _shift = Convert.ToInt32(TbStartScanYDelta.Text);
                        break;
                    case "TglBtnBpoStartInY":
                        _command = 0x0301;
                        _comwithshift = false;
                        break;
                    case "TglBtnBpoStartInZ":
                        _command = 0x0302;
                        _comwithshift = false;
                        break;
                    default:
                        throw new Exception("Something gone wrong.");
                }
            }
        }
        /// <summary>
        /// Обмен данными с устройством
        /// </summary>
        /// <param name="command">Команда управления</param>
        /// <param name="count">Номер пакета</param>
        /// <param name="shift">Смещение(для некоторых команд)</param>
        private void DataExchange(short command, ushort count, int shift)
        {
            // Зануляем сдвиг, если команда без него
            if (!_comwithshift)
            {
                _shift = 0;
                shift = 0;
            }

            SendCommand(command, count, shift);
            RecieveCommand();
        }
        // Обновления поля сдвига
        private void TbShift_TextChanged(object sender, TextChangedEventArgs e)
        {
            var texbox = (TextBox) sender;

            if (int.TryParse(texbox.Text, out int shift))
            {
                //По статусу кнопки определяем какой сдвиг обновился
                    switch (texbox.Name)
                    {
                    case "TbStartScanYDelta":
                        if (TglBtnStartScanYDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    case "TbYInCentreDelta":
                        if (TglBtnYInCentreDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    case "TbYStartScanDelta":
                        if (TglBtnYStartScanDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    case "TbStartScanZDelta":
                        if (TglBtnStartScanZDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    case "TbZInStartDelta":
                        if (TglBtnZInStartDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    case "TbZCentreDelta":
                        if (TglBtnZCentreDelta.IsChecked == true)
                        _shift = shift;
                        break;
                    default:
                    throw new Exception("Something gone wrong.");
                    }
                }
            else
            {
                _shift = 0;
                MessageBox.Show(this, "Введите число", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                texbox.Text = 0.ToString();
            }
        }
    }
}
