using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Graph
{
    public class COMdevice
    {
        private byte[] rxdata;
        int rxidx;
        private SerialPort _serialPort;
        private const int WAIT_ANSWER_TIMEOUT = 500;
        private log logfile;
        public string COM = "";
        public string errtxt = "";

        private void RxReset()
        {
            rxidx = 0;
            rxdata[0] = 0;
        }

        //Конструктор
        public void pmini()
        {
            _serialPort = new SerialPort();
            logfile = new log(this.GetType().ToString());
            rxdata = new byte[256];
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            try
            {
                while (0 != sp.BytesToRead)
                {
                    if (rxidx < rxdata.Length - 2)
                    {
                        rxdata[rxidx++] = (byte)sp.ReadByte();
                        rxdata[rxidx] = 0;
                    }
                    else
                        sp.ReadByte();
                }
            }
            catch (Exception ex)
            {
                logfile.write(ex.Message);
                errtxt = ex.Message;
            }
        }

        private bool Open(string COM)
        {
            bool opened = false;

            _serialPort.PortName = COM;
            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Open();
            // Создание обработчика события для приема данных:
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            _serialPort.WriteTimeout = 50;
            opened = true;
            return opened;
        }

        private void Close()
        {
            _serialPort.Close();
        }

        private String Received()
        {
            return Encoding.GetEncoding(1251).GetString(rxdata);
        }

        private bool Write(string senddata)
        {
            bool result;
            int cnt = senddata.Length;
            int i = 0;
            int tumeoutcnt = 0;

            errtxt = "";
            try
            {
                RxReset();
                for (i = 0; i < cnt; i++)
                {
                    char sym = senddata[i];
                    _serialPort.Write(sym.ToString());
                    tumeoutcnt = 50;
                    while (!Received().Contains(senddata.Substring(0, i + 1))
                           && (0 != tumeoutcnt))
                    {
                        Thread.Sleep(10);
                        tumeoutcnt--;
                    }
                }
            }
            catch (Exception ex)
            {
                logfile.write(ex.Message);
                errtxt = ex.Message;
            }
            result = (i == cnt) && (0 != tumeoutcnt);
            return result;
        }
    }
}
}
