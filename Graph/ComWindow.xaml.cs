using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;

namespace Graph
{
    /// <summary>
    /// Логика взаимодействия для ComWindow.xaml
    /// </summary>
    public partial class ComWindow : Window
    {
        SerialPort sp = new SerialPort();
        string pathToFile = ("");
        Thread t;

        public ComWindow()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            for (int i = 0; i < ports.Length; i++)
            {
                Cb.Items.Insert(i, "[" + i.ToString() + "] " + ports[i].ToString());
            }

            int n = Cb.SelectedIndex;

            // настройки порта
            sp.PortName = ports[n];
            sp.BaudRate = 9600;
            sp.DataBits = 8;
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;
            sp.ReadTimeout = 1000;
            sp.WriteTimeout = 1000;

            sp.Handshake = Handshake.None;

        }

        private void OpenCom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sp.Open();
            }
            catch (Exception ee)
            {
                TxbCom.Text = ("ERROR: невозможно открыть порт:" + ee.ToString());
                return;
            }
            TxbCom.Text = TxbCom.Text + ("Порт открыт") + '\r' + '\n';
        }

        private void SendCom_Click(object sender, RoutedEventArgs e)
        {
            byte[] send = Encoding.ASCII.GetBytes(TxbSend.Text);
            sp.Write(send, 0, send.Length);
            send[0] = Convert.ToByte('\r');
            sp.Write(send, 0, 1);
        }
       

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "Файл импульса (*.csv)|*.csv" + "|Все файлы (*.*)|*.* ";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                pathToFile = myDialog.FileName;
            }
            txbFile.Text = myDialog.SafeFileName;
        }

        private void SendFilebtn_Click(object sender, RoutedEventArgs e)
        {
            t = new Thread(SendFile);
            t.Start();
        }

        public void SendFile()
        {
            string data;
            string readrow;
            int data1;
            byte it;
            byte[] send = new byte[1];
            string[] csv = File.ReadAllLines(pathToFile);
            var row = 0;

            foreach (var l in csv)
            {
                readrow = "";
                char[] ar = l.ToCharArray();
                it = 0;
                for (int i = 0; i < ar.Length; i++)
                {
                    sp.Write(Convert.ToString(ar[i]));
                    do
                    {

                        data1 = sp.ReadByte();
                        it++;
                        var datach = Convert.ToChar(data1);
                        Thread.Sleep(1);
                    }
                    while (Convert.ToChar(data1) != Convert.ToChar(ar[i]) && it!=100);
                    readrow += Convert.ToChar(data1);
                }
                Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + readrow; }));
                send[0] = Convert.ToByte('\r');
                sp.Write(send, 0, 1);
                data = sp.ReadLine();
                Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + data; }));
                data = sp.ReadLine();
                Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + data + '\r' + '\n'; }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text +'\r' + '\n'; }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.ScrollToEnd(); }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { Pgbcsv.Value = row * 100 / csv.Length; }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { PgbText.Text = Convert.ToString(row * 100 / csv.Length)+"%"; }));
                row++;
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            t.Abort();
        }
    }
}
