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
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Drawing;
using System.Windows.Controls.Primitives;
using System.Data;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;

namespace Graph
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public delegate Point GetPosition(IInputElement element);
        private Point startPoint = new Point();
        private Point endPoint = new Point();
        private ChartStyleGridlines cs;
        private DataCollection dc;
        private DataSeries ds;
        private double xmin0 = -1;
        private double xmax0 = 30;
        private double ymin0 = -210;
        private double ymax0 = 210;
        private double xIncrement = 15;
        private double yIncrement = 15;
        private int tblength = 1;
        string pathToFile = ("");
        string TbTitle = "Импульс сварочного тока";
        public string[] csv;
        string[] lines;
        bool flcb;
                SerialPort sp = new SerialPort();
        Thread t;
        ///////
        private List<Ellipse> circles = new List<Ellipse>();
        private List<Ellipse> labelCircles = new List<Ellipse>();
        private List<TextBlock> labelResults = new List<TextBlock>();
        private TextBlock xCoordinate = new TextBlock();
        ///////
        ObservableCollection<Table> dataFirst = new ObservableCollection<Table>();
        ObservableCollection<Table> data = new ObservableCollection<Table>();
        ObservableCollection<Table> dopdata = new ObservableCollection<Table>();
        Table min;
        Table max;
        bool threadstop = false;
        private object dummyNode = null;
        byte[] w = new byte[1];
        byte[] hb = new byte[1];
        byte[] lb = new byte[1];
        byte[] cr = new byte[1];
        byte[] cd = new byte[1];
        string LastItem = "";//"C:\\Users\\ASGord\\Documents\\PulseWeling WaveForm"
        string prelastitem;
        string file;
        int linemax=0;
        public MainWindow()
        {
           // Control.CheckForIllegalCrossThreadCalls = false;
            string[] ports = SerialPort.GetPortNames();
            InitializeComponent();
            
            LastItem = FileRead();
            prelastitem = LastItem;
           
            cs = new ChartStyleGridlines
            {
                Xmin = xmin0,
                Xmax = xmax0,
                Ymin = ymin0,
                Ymax = ymax0
            };
            for (int i = 0; i < ports.Length; i++)
            {
                Cb.Items.Insert(i, "[" + i.ToString() + "] " + ports[i].ToString());
            }
            int n = Cb.SelectedIndex;

            tbYmax.Text = Convert.ToString(ymax0);
            tbYmin.Text = Convert.ToString(ymin0);
            tbXmax.Text = Convert.ToString(xmax0);
            tbXmin.Text = Convert.ToString(xmin0);

            // настройки порта
            sp.PortName = ports[n];
            sp.BaudRate = 9600;
            sp.DataBits = 8;
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;
            sp.ReadTimeout = 1000;
            sp.WriteTimeout = 1000;
            sp.Handshake = Handshake.None;

            
            TabCon.SelectedItem = TabFile;
            data.Add(new Table { Time = tblength, Current = 0, Cod = false });

             DataGridView.ItemsSource = data;

            //Ellipse circle = new Ellipse();
            //circle.Width = 8;
            //circle.Height = 8;
            //circle.Margin = new Thickness(2);
            //circle.Fill = Brushes.Blue;
            //labelCircles.Add(circle);
            //TextBlock tb = new TextBlock();
            //tb.Text = "Y0 Value";
            //tb.FontSize = 10;
            //tb.Margin = new Thickness(2);
            //labelResults.Add(tb);
            //circle = new Ellipse();
            //circle.Width = 8;
            //circle.Height = 8;
            //circle.Fill = Brushes.Blue;
            //circle.Visibility = Visibility.Hidden;
            //circles.Add(circle);
            //labelResults.Add(tb);
            //circle = new Ellipse();
            //circle.Width = 8;
            //circle.Height = 8;
            //circle.Fill = Brushes.Blue;
            //circle.Visibility = Visibility.Hidden;
            //circles.Add(circle);

            //xCoordinate.Text = "X Value";
            //xCoordinate.FontSize = 10;
            //xCoordinate.Margin = new Thickness(2);
            //OpenPort();
            //resultPanel.Children.Add(xCoordinate);
        }

        void OpenPort()
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
            //TxbCom.Text = TxbCom.Text + "Порт " + Cb.Text.Substring(4) + " открыт" + '\r' + '\n';
        }

        void ClosePort()
        {
            try
            {
                sp.Close();
            }
            catch (Exception ee)
            {
                TxbCom.Text = ("ERROR: невозможно закрыть порт:" + ee.ToString());
                return;
            }
            //TxbCom.Text = TxbCom.Text + "Порт " + Cb.Text.Substring(4) + " закрыт" + '\r' + '\n';
        }



        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
        {

           // data.Clear();
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "Файл импульса (*.csv)|*.csv" + "|Все файлы (*.*)|*.* ";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                pathToFile = myDialog.FileName;
            }
            if (pathToFile != "")
            {
                TbTitle = myDialog.SafeFileName;
                data = Addtable();
                tblength = lines.Length;
                cs.Xmax = tblength * 0.1 + 1;
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas, textCanvas);
            }
        }

        /// <summary>
        /// Посмтроение коллекции
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Table> Addtable()
        {
            ObservableCollection<Table> datas = new ObservableCollection<Table>();
            lines = File.ReadAllLines(pathToFile);
            foreach (var l in lines)
            {
                string[] cell = l.Split(';');
                cell[0] = cell[0].Substring(1);
                if (cell[2] == "")
                {
                    datas.Add(new Table { Time = Convert.ToInt32(cell[0]), Current = 0, Cod = false });
                }
                else
                {
                    if ((Convert.ToByte(Convert.ToInt32(cell[2]) & 0x02) == 0x02))
                    {
                        if ((Convert.ToByte(Convert.ToInt32(cell[2]) & 0x80) == 0x80))
                        {
                            datas.Add(new Table { Time = Convert.ToInt32(cell[0]), Current = Convert.ToInt32(cell[1]) * -1 * 2, Cod = true });
                        }
                        else
                        {
                            datas.Add(new Table { Time = Convert.ToInt32(cell[0]), Current = Convert.ToInt32(cell[1]) * -1 * 2, Cod = false });
                        }
                    }
                    else
                    {
                        if ((Convert.ToByte(Convert.ToInt32(cell[2]) & 0x80) == 0x80))
                        {
                            datas.Add(new Table { Time = Convert.ToInt32(cell[0]), Current = Convert.ToInt32(cell[1]) * 2, Cod = true });
                        }
                        else
                        {
                            datas.Add(new Table { Time = Convert.ToInt32(cell[0]), Current = Convert.ToInt32(cell[1]) * 2, Cod = false });
                        }
                    }
                }
            }
            return datas;
        }

        public void AddChart(double xmin, double xmax, double ymin, double ymax, ObservableCollection<Table> data, Canvas chartCanvas, Canvas textCanvas)
        {

            resultPanel.Children.Clear();
            chartCanvas.Children.Clear();
            textCanvas.Children.RemoveRange(1, textCanvas.Children.Count - 1);

            tblength = data.Count;
            cs = new ChartStyleGridlines
            {
                ChartCanvas = chartCanvas,
                TextCanvas = textCanvas,
                Title = TbTitle,
                XLabel = "Время, мс",
                YLabel = "Ток, А",
                Xmin = xmin,
                Xmax = xmax,
                Ymin = ymin,
                Ymax = ymax,
                GridlinePattern = ChartStyleGridlines.GridlinePatternEnum.Dot,
                GridlineColor = Brushes.Black,
            };
            cs.AddChartStyle(tbTitle, tbXLabel, tbYLabel);
            dc = new DataCollection();
            ds = new DataSeries();

            tbYmax.Text = Convert.ToString(Math.Round(ymax));
            tbYmin.Text = Convert.ToString(Math.Round(ymin));
            tbXmax.Text = Convert.ToString(Math.Round(xmax, 1, MidpointRounding.AwayFromZero));
            tbXmin.Text = Convert.ToString(Math.Round(xmin, 1, MidpointRounding.AwayFromZero));

            double dx = (cs.Xmax - cs.Xmin) / 100;
            if (data != null && tblength!=0) //pathToFile != "" &&
            {

                ds.LineColor = Brushes.Blue;
                ds.LineThickness = 4;
                ds.LineSeries.Points.Add(new Point(0, 0));
                for (int i = 0; i < tblength; i++)
                {
                    ds.LineSeries.Points.Add(new Point(Convert.ToInt32(data[i].Time) * 0.1, Convert.ToInt32(data[i].Current)));
                }
                ds.LineSeries.Points.Add(new Point(data[tblength - 1].Time * 0.1 + 0.1, 0));
                dc.DataList.Add(ds);

                ///////////////////////////
                //Ellipse circle = new Ellipse();
                //circle.Width = 8;
                //circle.Height = 8;
                //circle.Margin = new Thickness(2);
                //circle.Fill = ds.LineColor;
                //labelCircles.Add(circle);
                //TextBlock tb = new TextBlock();
                //tb.Text = "Y0 Value";
                //tb.FontSize = 10;
                //tb.Margin = new Thickness(2);
                //labelResults.Add(tb);
                //circle = new Ellipse();
                //circle.Width = 8;
                //circle.Height = 8;
                //circle.Fill = ds.LineColor;
                //circle.Visibility = Visibility.Hidden;
                //circles.Add(circle);
                //labelResults.Add(tb);
                //circle = new Ellipse();
                //circle.Width = 8;
                //circle.Height = 8;
                //circle.Fill = ds.LineColor;
                //circle.Visibility = Visibility.Hidden;
                //circles.Add(circle);

                //xCoordinate.Text = "X Value";
                //xCoordinate.FontSize = 10;
                //xCoordinate.Margin = new Thickness(2);

                //resultPanel.Children.Add(xCoordinate);

                //chartCanvas.Children.Add(circles[0]);
                //Canvas.SetTop(circles[0], 0);
                //Canvas.SetLeft(circles[0], 0);
                //resultPanel.Children.Add(labelCircles[0]);
                //resultPanel.Children.Add(labelResults[0]);
                //for (int i = 0; i < dc.DataList.Count; i++)
                //{
                //    chartCanvas.Children.Add(circles[i]);
                //    Canvas.SetTop(circles[i], 0);
                //    Canvas.SetLeft(circles[i], 0);
                //    resultPanel.Children.Add(labelCircles[i]);
                //    resultPanel.Children.Add(labelResults[i]);
                //}
                ////////////////////
            }

            ds = new DataSeries
            {
                LineColor = Brushes.Black,
                LinePattern = DataSeries.LinePatternEnum.Solid,
                LineThickness = 1
            };

            ds.LineSeries.Points.Add(new Point(200, 0));
            ds.LineSeries.Points.Add(new Point(-1, 0));
            dc.DataList.Add(ds);
            if (chartCanvas == chartCanvas1)
            {
                if (TimeMinTB.Text != "" && TimeMaxTB.Text != "")
                {
                    if (data != null) //pathToFile != "" &&
                    {
                        ds = new DataSeries();
                        ds.LineColor = Brushes.Red;
                        ds.LineThickness = 4;
                        try
                        {
                            for (int i = Convert.ToInt32(TimeMinTB.Text) - 1; i < Convert.ToInt32(TimeMaxTB.Text); i++)
                                ds.LineSeries.Points.Add(new Point(Convert.ToInt32(data[i].Time) * 0.1, Convert.ToInt32(data[i].Current)));
                        }
                        catch(Exception) { }


                    }
                    dc.DataList.Add(ds);
                }
            }
            
            dc.AddLines(cs);
        }
        
        private void ChartGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Mouse.RightButton != MouseButtonState.Pressed)
            {
               
                resultPanel.Children.Clear();
                textCanvas.Width = chartGrid.ActualWidth;
                textCanvas.Height = chartGrid.ActualHeight;

                
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas, textCanvas);
            }
        }

        private void ChartGrid1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Mouse.RightButton != MouseButtonState.Pressed)
            {
                resultPanel1.Children.Clear();
                textCanvas1.Width = chartGrid1.ActualWidth;
                textCanvas1.Height = chartGrid1.ActualHeight;
               

                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
                
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!chartCanvas1.IsMouseCaptured)
            {
                startPoint = e.GetPosition(chartCanvas1);
                chartCanvas1.CaptureMouse();

                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    chartCanvas1.Cursor = Cursors.Cross;
                    for (int i = 0; i < dc.DataList.Count; i++)
                    {
                        double x = startPoint.X;
                        double y = GetInterpolatedYValue(dc.DataList[i], x);
                        Canvas.SetLeft(circles[i], x - circles[i].Width / 2);
                        Canvas.SetTop(circles[i], y - circles[i].Height / 2);
                    }
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {

            if (chartCanvas1.IsMouseCaptured)
            {
                if (e.RightButton != MouseButtonState.Pressed)
                {
                    endPoint = e.GetPosition(chartCanvas1);
                    TranslateTransform tt = new TranslateTransform();
                    tt.X = endPoint.X - startPoint.X;
                    tt.Y = endPoint.Y - startPoint.Y;
                    for (int i = 0; i < dc.DataList.Count; i++)
                    {
                        dc.DataList[i].LineSeries.RenderTransform = tt;
                    }
                }
                else
                {
                    endPoint = e.GetPosition(chartCanvas1);

                    //if (Math.Abs(endPoint.X - startPoint.X) >
                    //SystemParameters.MinimumHorizontalDragDistance &&
                    //Math.Abs(endPoint.Y - startPoint.Y) >
                    //SystemParameters.MinimumVerticalDragDistance)
                    //{
                    double x, y;
                    //for (int i = 0; i < dc.DataList.Count; i++)
                    //{
                    //TranslateTransform tt = new TranslateTransform();

                    //tt.X = endPoint.X - startPoint.X;
                    //tt.Y = GetInterpolatedYValue(dc.DataList[0], endPoint.X) - GetInterpolatedYValue(dc.DataList[0], startPoint.X);

                    //circles[0].RenderTransform = tt;
                    //circles[0].Visibility = Visibility.Visible;

                    x = endPoint.X;
                    x = cs.Xmin + x * (cs.Xmax - cs.Xmin) / chartCanvas1.Width;

                    y = GetInterpolatedYValue(dc.DataList[0], endPoint.X);
                    y = cs.Ymin + (chartCanvas1.Height - y) * (cs.Ymax - cs.Ymin) / chartCanvas1.Height;

                    //xCoordinate.Text = Math.Round(x, 1).ToString();
                    int x1 = Convert.ToInt32(Math.Round(x * 10));
                    // labelResults[0].Text = Math.Round(y, 1).ToString();

                    if (x > 0 && x1 < data.Count - 1)
                    {
                        dopdata.Add(new Table { Time = data[x1].Time, Current = data[x1].Current, Cod = data[x1].Cod });
                        min = dopdata[0];
                        max = dopdata[dopdata.Count - 1];
                        if (min.Time < max.Time)
                        {
                            TimeMinTB.Text = Convert.ToString(min.Time);
                            TimeMaxTB.Text = Convert.ToString(max.Time);
                            CurMinTB.Text = Convert.ToString(min.Current);
                            CurMaxTB.Text = Convert.ToString(max.Current);
                        }
                        else
                        {
                            TimeMinTB.Text = Convert.ToString(max.Time);
                            TimeMaxTB.Text = Convert.ToString(min.Time);
                            CurMinTB.Text = Convert.ToString(max.Current);
                            CurMaxTB.Text = Convert.ToString(min.Current);
                        }
                        DataGridView.SelectedIndex = Convert.ToInt32(Math.Round(x * 10));
                        DataGridView.ScrollIntoView(DataGridView.Items[DataGridView.SelectedIndex]);
                        AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
                    }
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (Keyboard.IsKeyDown(Key.LeftShift))
            //{
            //chartCanvas.ReleaseMouseCapture();
            //chartCanvas.Cursor = Cursors.Arrow;

            //xCoordinate.Text = "X Value";
            //for (int i = 0; i < dc.DataList.Count; i++)
            //{
            //    circles[i].Visibility = Visibility.Hidden;
            //    labelResults[i].Text = "Y" + i.ToString() + " Value";
            //}
            //}
            //else
            //{
            if (data.Count != 0)
            {
                double dx = 0;
                double dy = 0;
                double x0 = 0;
                double x1 = 1;
                double y0 = 0;
                double y1 = 1;
                endPoint = e.GetPosition(chartCanvas1);
                dx = (cs.Xmax - cs.Xmin) * (endPoint.X - startPoint.X) /
                chartCanvas1.Width;
                dy = (cs.Ymax - cs.Ymin) * (endPoint.Y - startPoint.Y) /
                chartCanvas1.Height;
                x0 = cs.Xmin - dx;
                x1 = cs.Xmax - dx;
                y0 = cs.Ymin + dy;
                y1 = cs.Ymax + dy;
                AddChart(x0, x1, y0, y1, data, chartCanvas1, textCanvas1);
                chartCanvas1.ReleaseMouseCapture();
                chartCanvas1.Cursor = Cursors.Arrow;
            }

        }

        private double GetInterpolatedYValue(DataSeries data, double x)
        {
            double result = double.NaN;
            for (int i = 1; i < data.LineSeries.Points.Count; i++)
            {
                double x1 = data.LineSeries.Points[i - 1].X;
                double x2 = data.LineSeries.Points[i].X;
                if (x >= x1 && x < x2)
                {
                    double y1 = data.LineSeries.Points[i - 1].Y;
                    double y2 = data.LineSeries.Points[i].Y;
                    result = y1 + (y2 - y1) * (x - x1) / (x2 - x1);
                }
            }
            return result;
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (!chartCanvas1.IsMouseCaptured)
            {
                dopdata.Clear();
                startPoint = e.GetPosition(chartCanvas);
                chartCanvas1.Cursor = Cursors.Cross;
                chartCanvas1.CaptureMouse();
                //for (int i = 0; i < dc.DataList.Count; i++)
                //{
                //double x = startPoint.X;
                //double y = GetInterpolatedYValue(dc.DataList[0], x);
                //Canvas.SetLeft(circles[0], x - circles[0].Width / 2);
                //Canvas.SetTop(circles[0], y - circles[0].Height / 2);
                //}

            }
        }

        private void chartCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            chartCanvas1.ReleaseMouseCapture();
            chartCanvas1.Cursor = Cursors.Arrow;
            if (dopdata.Count != 0)
            {
                min = dopdata[0];
                max = DataGridView.SelectedItems[0] as Table;
                if (min.Time < max.Time)
                {
                    TimeMinTB.Text = Convert.ToString(min.Time);
                    TimeMaxTB.Text = Convert.ToString(max.Time);
                    CurMinTB.Text = Convert.ToString(min.Current);
                    CurMaxTB.Text = Convert.ToString(max.Current);
                }
                else
                {
                    TimeMinTB.Text = Convert.ToString(max.Time);
                    TimeMaxTB.Text = Convert.ToString(min.Time);
                    CurMinTB.Text = Convert.ToString(max.Current);
                    CurMaxTB.Text = Convert.ToString(min.Current);
                }
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
            }
            //xCoordinate.Text = "X Value";
            //for (int i = 0; i < dc.DataList.Count; i++)
            //{
            //    circles[i].Visibility = Visibility.Hidden;
            //    labelResults[i].Text = "Y" + i.ToString() + " Value";
            //}

        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (pathToFile != "")
            {
                double dx = (e.Delta > 0) ? xIncrement : -xIncrement;
                double dy = (e.Delta > 0) ? yIncrement : -yIncrement;
                double x0 = cs.Xmin + (cs.Xmax - cs.Xmin) * dx / chartCanvas1.Width;
                double x1 = cs.Xmax - (cs.Xmax - cs.Xmin) * dx / chartCanvas1.Width;
                double y0 = cs.Ymin + (cs.Ymax - cs.Ymin) * dy / chartCanvas1.Height;
                double y1 = cs.Ymax - (cs.Ymax - cs.Ymin) * dy / chartCanvas1.Height;
                AddChart(x0, x1, y0, y1, data, chartCanvas1, textCanvas1);
            }

        }

        private void AddRowBtn_Click(object sender, RoutedEventArgs e)
        {
            int it = 0;
            if (TxbRow.Text != "")
            {

                it = Convert.ToInt32(TxbRow.Text);
            }
            else
            {
                it = 1;
            }
            for (int i = 0; i < it; i++)
            {
                tblength++;
                data.Add(new Table { Time = tblength, Current = 0, Cod = false });
            }
        }

        private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
        {
            int it = 0;
            if (TxbRow.Text != "")
            {
                it = Convert.ToInt32(TxbRow.Text);
            }
            else
            {
                it = 1;

            }
            if (it > tblength)
                it = tblength;
            for (int i = 0; i < it; i++)
            {
                tblength--;
                data.RemoveAt(tblength);
            }
        }

        private void DataGridView_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (Mouse.RightButton != MouseButtonState.Pressed)
            {
                cs.Xmax = tblength * 0.1 + 1;
                cs.Xmin = -0.5;
                if (data.Count != 0)
                    AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
            }
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
            TxbCom.Text = TxbCom.Text + "Порт " + Cb.Text.Substring(4) + " открыт" + '\r' + '\n';
        }

        public void SafeFile()
        {
            csv = new string[tblength];

            for (int i = 0; i < tblength; i++)

            {
                if (data[i].Current < 0)
                {
                    if (data[i].Cod == true)
                    {

                        csv[i] = "W" + data[i].Time + ";" + Math.Round(Math.Abs(Convert.ToDouble(data[i].Current)) / 2) + ";" + 131;
                    }
                    else
                    {
                        csv[i] = "W" + data[i].Time + ";" + Math.Round(Math.Abs(Convert.ToDouble(data[i].Current)) / 2) + ";" + 3;
                    }
                }
                else if (data[i].Current > 0)
                {
                    if (data[i].Cod == true)
                    {

                        csv[i] = "W" + data[i].Time + ";" + Math.Round(Math.Abs(Convert.ToDouble(data[i].Current)) / 2) + ";" + 69;
                    }
                    else
                    {
                        csv[i] = "W" + data[i].Time + ";" + Math.Round(Math.Abs(Convert.ToDouble(data[i].Current)) / 2) + ";" + 5;
                    }
                    
                }
                else csv[i] = "W" + data[i].Time + ";;";
            }
        }

        public void SendFile()
        {
             
            SafeFile();
            string data2;
            string readrow;
            int data1;
            byte it;
            byte[] send = new byte[1];
            var row = 0;
            byte highbyte;
            byte lowbyte;
            char datachar;
            int wc, hbc, lbc, crc, cdc;
            
            if (flcb == false)
            {
                
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
                        while (Convert.ToChar(data1) != Convert.ToChar(ar[i]) && it != 100);
                        readrow += Convert.ToChar(data1);
                    }

                    if (linemax == 200)
                    {
                        linemax = 0;
                        TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            
                            TxbCom.Text = "";
                        }));
                    }
                   // Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + readrow; }));
                    TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TxbCom.Text = TxbCom.Text + readrow;
                    }));
                    send[0] = Convert.ToByte('\r');
                    linemax++;
                    sp.Write(send, 0, 1);
                    data2 = sp.ReadLine();
                    // Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + data2; }));
                    TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TxbCom.Text = TxbCom.Text + data2;
                    }));
                    data2 = sp.ReadLine();
                    TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TxbCom.Text = TxbCom.Text + data2 + '\r' + '\n';
                        TxbCom.ScrollToEnd();
                    }));
                    linemax++;
                    Pgbcsv.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Pgbcsv.Value = row * 100 / csv.Length;
                        PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";
                    }));
                    //  Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + data2 + '\r' + '\n'; TxbCom.ScrollToEnd(); Pgbcsv.Value = row * 100 / csv.Length; PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%"; }));

                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.Text = TxbCom.Text + data2 + '\r' + '\n'; }));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.ScrollToEnd(); }));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { Pgbcsv.Value = row * 100 / csv.Length; }));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%"; }));
                    row++;
                }
            }
            else
            {
                w[0] = 0;
                hb[0] = 0;
                lb[0] = 0;
                cr[0] = 0;
                cd[0] = 0;
                
                for (int i = 0; i < data.Count; i++)
                {
                    if (threadstop) break;
                    highbyte = Convert.ToByte((data[i].Time & 0xFF00) >> 8);
                    lowbyte = Convert.ToByte(data[i].Time & 0x00FF);
                    w[0] = 119;
                    hb[0] = highbyte;
                    lb[0] = lowbyte;
                    cr[0] = Convert.ToByte(Math.Abs(data[i].Current));
                    if (data[i].Current < 0)
                    {
                        if (data[i].Cod == true)
                        {

                            cd[0] = Convert.ToByte(131);
                        }
                        else
                        {

                            cd[0] = Convert.ToByte(3);
                        }
                    }
                    else
                    {
                        if (data[i].Cod == true)
                        {
                            cd[0] = Convert.ToByte(69);
                        }
                        else
                        {
                            cd[0] = Convert.ToByte(5);
                        }
                    }
                    wc = Sendfastcom(w);
                    hbc = Sendfastcom(hb);
                    lbc = Sendfastcom(lb);
                    crc = Sendfastcom(cr);
                    cdc = Sendfastcom(cd);
                    data1 = sp.ReadByte();
                    datachar = Convert.ToChar(data1);

                    readrow = Convert.ToChar(wc) + " " + Convert.ToString(data[i].Time) + " " + crc + " " + cdc + " " + datachar;

                    if (linemax == 150)
                    {
                        TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            linemax = 0;
                            TxbCom.Text = "";
                        }));
                    }
                    TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        
                        TxbCom.Text = TxbCom.Text + readrow + '\r' + '\n';
                        TxbCom.ScrollToEnd();
                    }));
                    linemax++;
                    Pgbcsv.Dispatcher.BeginInvoke(new Action(() =>
                     {
                         Pgbcsv.Value = row * 100 / csv.Length;
                         PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";
                     }));

                    //Application.Current.Dispatcher.Invoke(new System.Action(() =>
                    //{
                    //    TxbCom.Text = TxbCom.Text + Convert.ToChar(wc) + " " + Convert.ToString(data[i].Time) + " " + crc + " " + cdc + " " + datachar + '\r' + '\n';
                    //    TxbCom.ScrollToEnd();
                    //    Pgbcsv.Value = row * 100 / csv.Length;
                    //    PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";
                    //})); 
                    //Action a = () =>
                    //{
                    //    TxbCom.Text = TxbCom.Text + Convert.ToChar(wc) + " " + Convert.ToString(data[i-1].Time) + " " + crc + " " + cdc + " " + datachar + '\r' + '\n';
                    //    TxbCom.ScrollToEnd();
                    //    Pgbcsv.Value = row * 100 / csv.Length;
                    //    PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";
                    //};
                    //Dispatcher.BeginInvoke(a);

                    //this.Dispatcher.Invoke(new Action(
                    // delegate ()
                    //    {
                    //        TxbCom.Text = TxbCom.Text + Convert.ToChar(wc) + " " + Convert.ToString(data[i].Time) + " " + crc + " " + cdc + " " + datachar + '\r' + '\n';
                    //        TxbCom.ScrollToEnd();
                    //        Pgbcsv.Value = row * 100 / csv.Length;
                    //        PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";
                    //    }
                    //));

                    //Dispatcher.BeginInvoke(new ThreadStart(delegate
                    //{
                    //    TxbCom.Text = TxbCom.Text + Convert.ToChar(wc) + " " + Convert.ToString(data[i - 1].Time) + " " + crc + " " + cdc + " " + datachar + '\r' + '\n';
                    //    TxbCom.ScrollToEnd();
                    //    Pgbcsv.Value = row * 100 / csv.Length;
                    //    PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%";

                    //}));

                    //Dispatcher.BeginInvoke(new ThreadStart(delegate
                    //{
                    //    TxbCom.Text = TxbCom.Text + Convert.ToChar(w[0]) + " " + Convert.ToString(data[i - 1].Time) + " "
                    //    + Convert.ToString(cr[0]) + " " + Convert.ToString(cd[0]) + " " + datachar + '\r' + '\n';
                    //}));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { TxbCom.ScrollToEnd(); }));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { Pgbcsv.Value = row * 100 / csv.Length; }));
                    //Dispatcher.BeginInvoke(new ThreadStart(delegate { PgbText.Text = Convert.ToString(row * 100 / csv.Length) + "%"; }));
                    row++;

                    // }
                }
            }
            TxbCom.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TxbCom.Text = TxbCom.Text + "Готово" + '\r' + '\n';
                        TxbCom.ScrollToEnd();

                      
                    }));
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                ClosePort();
            }));
            
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            threadstop = true;
            Thread.Sleep(100);
           // t.Abort();
            ClosePort();
        }

        public int Sendfastcom(byte[] w)
        {
            
            int it, data1;
            sp.Write(w, 0, 1);
            it = 0;
            do
            {
                data1 = sp.ReadByte();
                it++;
                Thread.Sleep(1);
            }
            while (Convert.ToChar(data1) != w[0] && it != 100);
            return data1;
        }

        private void WeldBtn_Click(object sender, RoutedEventArgs e)
        {
            string data = "";
            string readrow;
            string readrow2;
            int data1;
            byte it;
            byte[] send = new byte[1];
            // string[] csv = File.ReadAllLines(pathToFile);
            readrow = "";
            readrow2 = "";
            char[] ar = new char[] { 'A' };
            it = 0;
            OpenPort();
            sp.Write(Convert.ToString(ar[0]));
            do
            {
                data1 = sp.ReadByte();
                it++;
                var datach = Convert.ToChar(data1);
                Thread.Sleep(1);
            }
            while (Convert.ToChar(data1) != Convert.ToChar(ar[0]) && it != 100);
            readrow += Convert.ToChar(data1);
            send[0] = Convert.ToByte('\r');
            sp.Write(send, 0, 1);
            Thread.Sleep(5000);
            do
            {
                data = sp.ReadLine();
                it++;
                var datach = Convert.ToChar(data1);
                Thread.Sleep(1);
            }
            while (data == "" && it != 100); //"Ok           "
                                             //data = sp.ReadLine();
                                             //TxbCom.Text = TxbCom.Text + '\r' + '\n';
            data = sp.ReadLine();
            readrow2 += data;
            TxbCom.Text = TxbCom.Text + readrow2 + '\r' + '\n';
            ClosePort();
        }

        //private void SaveMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    SafeFile();
        //    SaveFileDialog saveFileDialog = new SaveFileDialog();
        //    saveFileDialog.Filter = "Файл импульса (*.csv)|*.csv" + "|Все файлы (*.*)|*.* "; ;
        //    if (saveFileDialog.ShowDialog() == true)
        //        File.WriteAllLines(saveFileDialog.FileName, csv);
        //}

        //private void OpenMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    //data.Clear();
        //    OpenFileDialog myDialog = new OpenFileDialog();
        //    myDialog.Filter = "Файл импульса (*.csv)|*.csv" + "|Все файлы (*.*)|*.* ";
        //    myDialog.CheckFileExists = true;
        //    myDialog.Multiselect = false;
        //    if (myDialog.ShowDialog() == true)
        //    {
        //        pathToFile = myDialog.FileName;
        //    }
        //    if (pathToFile != "")
        //    {
        //        TbTitle = myDialog.SafeFileName;
        //        data = Addtable();
        //        chartCanvas.Children.Clear();
        //        textCanvas.Children.RemoveRange(1, textCanvas.Children.Count - 1);
        //        tblength = lines.Length;
        //        cs.Xmax = tblength * 0.1 + 1;
        //        AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        //    }
        //}

        //private void SendMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    flcb = Convert.ToBoolean(FastLoadCB.IsChecked);
        //    threadstop = false;
        //    t = new Thread(SendFile);
        //    t.Start();

        //}

        //private void FastloadMenu_Click(object sender, RoutedEventArgs e)
        //{

        //}

        private void TxbSend_KeyUp(object sender, KeyEventArgs e)
        {
            

            string data = "";
            string readrow;
            string readrow2;
            int data1;
            byte it;
            byte[] send = new byte[1];
            // string[] csv = File.ReadAllLines(pathToFile);
            readrow = "";
            readrow2 = "";

            char[] ar = TxbSend.Text.ToCharArray();
            string str = new string(ar);

            if (e.Key == Key.Enter)
            {
                OpenPort();
                if (StringIsValid(Convert.ToString(str)))
                {

                    if (ar[0] == 'A')
                    {
                        it = 0;
                        sp.Write(Convert.ToString(ar[0]));
                        do
                        {
                            data1 = sp.ReadByte();
                            it++;
                            var datach = Convert.ToChar(data1);
                            Thread.Sleep(1);
                        }
                        while (Convert.ToChar(data1) != Convert.ToChar(ar[0]) && it != 100);
                        readrow += Convert.ToChar(data1);
                        send[0] = Convert.ToByte('\r');
                        sp.Write(send, 0, 1);
                        Thread.Sleep(5000);
                        do
                        {
                            data = sp.ReadLine();
                            it++;
                            var datach = Convert.ToChar(data1);
                            Thread.Sleep(1);
                        }
                        while (data == "" && it != 100); //"Ok           "
                                                         //data = sp.ReadLine();
                                                         //TxbCom.Text = TxbCom.Text + '\r' + '\n';
                        data = sp.ReadLine();
                        readrow2 += data;
                        TxbCom.Text = TxbCom.Text + readrow2 + '\r' + '\n';
                    }
                    else
                    {
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
                            while (Convert.ToChar(data1) != Convert.ToChar(ar[i]) && it != 100);
                            readrow += Convert.ToChar(data1);
                        }
                        TxbCom.Text = TxbCom.Text + readrow;
                        send[0] = Convert.ToByte('\r');
                        sp.Write(send, 0, 1);
                        data = sp.ReadLine();
                        TxbCom.Text = TxbCom.Text + '\r' + '\n';
                       
                        data = sp.ReadLine();
                        readrow2 += data;
                        TxbCom.Text = TxbCom.Text + readrow2 + '\r' + '\n';
                        TxbCom.ScrollToEnd();
                        
                        TxbSend.Text = "";
                    }
                }
                else
                {
                    TxbSend.Text = "";
                    TxbCom.Text = TxbCom.Text + "Введен неверный символ" + '\r' + '\n';
                    TxbCom.ScrollToEnd();
                }
                ClosePort();
            }
        }

        private void InterBTN_Click(object sender, RoutedEventArgs e)
        {
            int[] x0 = new int[2];
            int[] y0 = new int[2];
            x0[0] = Convert.ToInt32(TimeMinTB.Text);
            x0[1] = Convert.ToInt32(TimeMaxTB.Text);
            y0[0] = Convert.ToInt32(CurMinTB.Text);
            y0[1] = Convert.ToInt32(CurMaxTB.Text);
            int x1 = Convert.ToInt32(x0[1]);
            int x2 = Convert.ToInt32(x0[0]);
            int n = x1 - x2;
            int[] x = new int[n];
            int j = 0;

            if (InterCB.SelectedIndex == 0)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] = x2 + 1 + i;
                }
                int[] y = InterpolationAlgorithms.Linear(x0, y0, x);

                data[x2 - 1].Current = Convert.ToInt32(y0[0]);
                data[x1 - 1].Current = Convert.ToInt32(y0[1]);
                for (int i = x2; i < x1 - 1; i++)
                {

                    data[i].Current = Convert.ToInt32(y[j]);
                    j++;

                }
            }

            if (InterCB.SelectedIndex == 1)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] = i;
                }
                int[] y = InterpolationAlgorithms.Sinus(x0, y0, x);

                data[x2 - 1].Current = Convert.ToInt32(y0[0]);
                //  data[x1 - 1].Current = Convert.ToInt32(y0[1]);
                for (int i = x2; i < x1 - 1; i++)
                {
                    data[i].Current = Convert.ToInt32(y[j + 1]);
                    j++;
                }
            }

            if (InterCB.SelectedIndex == 2)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] = i;
                }
                int[] y = InterpolationAlgorithms.Sinus1(x0, y0, x);

                data[x2 - 1].Current = Convert.ToInt32(y0[0]);
                //  data[x1 - 1].Current = Convert.ToInt32(y0[1]);
                for (int i = x2; i < x1 - 1; i++)
                {
                    data[i].Current = Convert.ToInt32(y[j + 1]);
                    j++;
                }
            }
            if (data.Count != 0)
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        }

        private void tb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                cs.Ymin = Convert.ToDouble(tbYmin.Text);
                cs.Ymax = Convert.ToDouble(tbYmax.Text);
                cs.Xmax = Convert.ToDouble(tbXmax.Text);
                cs.Xmin = Convert.ToDouble(tbXmin.Text);
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
            }
        }

        private void DataGridView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = DataGridView.SelectedItems;
            if (item.Count != 0)
            {
                Table min = item[0] as Table;
                Table max = item[DataGridView.SelectedItems.Count - 1] as Table;
                if (min.Time < max.Time)
                {
                    TimeMinTB.Text = Convert.ToString(min.Time);
                    TimeMaxTB.Text = Convert.ToString(max.Time);
                    CurMinTB.Text = Convert.ToString(min.Current);
                    CurMaxTB.Text = Convert.ToString(max.Current);
                }
                else
                {
                    TimeMinTB.Text = Convert.ToString(max.Time);
                    TimeMaxTB.Text = Convert.ToString(min.Time);
                    CurMinTB.Text = Convert.ToString(max.Current);
                    CurMaxTB.Text = Convert.ToString(min.Current);
                }

                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
            }
        }

        private void DataGridView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void dgcmSin_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int[] x0 = new int[2];
            int[] y0 = new int[2];
            x0[0] = Convert.ToInt32(TimeMinTB.Text);
            x0[1] = Convert.ToInt32(TimeMaxTB.Text);
            y0[0] = Convert.ToInt32(CurMinTB.Text);
            y0[1] = Convert.ToInt32(CurMaxTB.Text);
            int x1 = Convert.ToInt32(x0[1]);
            int x2 = Convert.ToInt32(x0[0]);
            int n = x1 - x2;
            int[] x = new int[n];
            int j = 0;

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = i;
            }
            int[] y = InterpolationAlgorithms.Sinus(x0, y0, x);

            data[x2 - 1].Current = Convert.ToInt32(y0[0]);
            //  data[x1 - 1].Current = Convert.ToInt32(y0[1]);
            for (int i = x2; i < x1 - 1; i++)
            {
                data[i].Current = Convert.ToInt32(y[j + 1]);
                j++;
            }
           
            if (data.Count != 0)
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        }

        private void dgcmSin1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int[] x0 = new int[2];
            int[] y0 = new int[2];
            x0[0] = Convert.ToInt32(TimeMinTB.Text);
            x0[1] = Convert.ToInt32(TimeMaxTB.Text);
            y0[0] = Convert.ToInt32(CurMinTB.Text);
            y0[1] = Convert.ToInt32(CurMaxTB.Text);
            int x1 = Convert.ToInt32(x0[1]);
            int x2 = Convert.ToInt32(x0[0]);
            int n = x1 - x2;
            int[] x = new int[n];
            int j = 0;
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = i;
            }
            int[] y = InterpolationAlgorithms.Sinus1(x0, y0, x);

            data[x2 - 1].Current = Convert.ToInt32(y0[0]);
            //  data[x1 - 1].Current = Convert.ToInt32(y0[1]);
            for (int i = x2; i < x1 - 1; i++)
            {
                data[i].Current = Convert.ToInt32(y[j + 1]);
                j++;
            }
          
            if (data.Count != 0)
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        }

        private void dgcmLine_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int[] x0 = new int[2];
            int[] y0 = new int[2];

            x0[0] = Convert.ToInt32(TimeMinTB.Text);
            x0[1] = Convert.ToInt32(TimeMaxTB.Text);
            y0[0] = Convert.ToInt32(CurMinTB.Text);
            y0[1] = Convert.ToInt32(CurMaxTB.Text);

            int x1 = Convert.ToInt32(x0[1]);
            int x2 = Convert.ToInt32(x0[0]);

            int n = x1 - x2;

            int[] x = new int[n];
            int j = 0;

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = x2 + 1 + i;
            }
            int[] y = InterpolationAlgorithms.Linear(x0, y0, x);

            data[x2 - 1].Current = Convert.ToInt32(y0[0]);
            data[x1 - 1].Current = Convert.ToInt32(y0[1]);
            for (int i = x2; i < x1 - 1; i++)
            {
                data[i].Current = Convert.ToInt32(y[j]);
                j++;
            }
           
            if (data.Count != 0)
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        }

        private void TimeTB_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
            }
        }

        public static bool StringIsValid(string str)
        {
            return !string.IsNullOrEmpty(str) && !Regex.IsMatch(str, @"[^a-zA-z\d_;]");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in Directory.GetLogicalDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = s;
                item.Tag = s;
                item.FontWeight = FontWeights.Normal;
                item.Items.Add(dummyNode);
                item.Expanded += new RoutedEventHandler(folder_Expanded);
                foldersItem.Items.Add(item);
                string sub = item.Tag.ToString();
                if (LastItem != "")
                {
                    if (sub[0] == LastItem[0])
                    {
                        item.IsExpanded = true;
                    }
                }
            }

            
        }

        void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        //prelastitem = LastItem.Substring(0, s.Length);

                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        

                        item.Items.Add(subitem);
                        //if (prelastitem == s)
                        //{
                        //    subitem.IsExpanded = true;
                        //}
                        if (prelastitem.StartsWith(s))
                        {
                            subitem.IsExpanded = true;
                        }

                        LastItem = item.Tag.ToString();
                    }
                    foreach (string s in Directory.GetFiles(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        if (getFileExtension(s) == "csv" || getFileExtension(s) == "txt")
                        {
                            subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                            subitem.Tag = s;
                            subitem.FontWeight = FontWeights.Normal;
                            subitem.Items.Add(dummyNode);
                            subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                            item.Items.Add(subitem);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        private void foldersItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ObservableCollection<Table> dataFirst = new ObservableCollection<Table>();
            
            TreeView tree = (TreeView)sender;
            TreeViewItem temp = ((TreeViewItem)tree.SelectedItem);

            pathToFile = temp.Tag.ToString();
            if (getFileExtension(pathToFile) == "csv")
            {
                if (pathToFile != "")
                {
                    TbTitle = temp.Name;
                    dataFirst = Addtable();

                    int min = dataFirst[0].Current, minIndex = 0;
                    for (int i = 0; i < dataFirst.Count; i++)
                    {
                        if (min > dataFirst[i].Current)
                        {
                            min = dataFirst[i].Current;
                            minIndex = i;
                        }
                    }

                    int max = dataFirst[0].Current, maxIndex = 0;
                    for (int i = 0; i < dataFirst.Count; i++)
                    {
                        if (max < dataFirst[i].Current)
                        {
                            max = dataFirst[i].Current;
                            maxIndex = i;
                        }
                    }

                    if (Math.Abs(min) <= Math.Abs(max))
                    {
                        cs.Ymax = max + max * 0.05;
                        cs.Ymin = -max - max * 0.05;
                    }
                    else
                    {
                        cs.Ymax = Math.Abs(min) + Math.Abs(min) * 0.05;
                        cs.Ymin = min + min * 0.05;
                    }

                    tblength = lines.Length;
                    cs.Xmax = tblength * 0.1 + 1;

                    
                    AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, dataFirst, chartCanvas, textCanvas);
                }
            }
        }

        public string getFileExtension(string fileName)
        {
            return fileName.Substring(fileName.LastIndexOf(".") + 1);
        }

        private void lfBtn_Click(object sender, RoutedEventArgs e)
        {
            data = Addtable();
            DataGridView.ItemsSource = data;
            if (textCanvas1.ActualHeight == 0)
            {
                textCanvas1.Height = chartCanvas.ActualHeight;
                textCanvas1.Width = chartCanvas.ActualWidth;
            }
            TabCon.SelectedItem = TabGraph;
            FileWrite();
            AddChart(cs.Xmin, cs.Xmax, cs.Ymin, cs.Ymax, data, chartCanvas1, textCanvas1);
        }

        private void LoadFileBtn_Click(object sender, RoutedEventArgs e)
        {
                OpenPort();
                flcb = Convert.ToBoolean(FastLoadCB.IsChecked);
                threadstop = false;
                t = new Thread(SendFile);
                t.Start();
            

        }

        void FileWrite()
        {

            file = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "path.txt");

            using (FileStream fstream = new FileStream(file, FileMode.OpenOrCreate))// @"C:\Users\mr. Seront\source\repos\Graph\note.txt"
            {
                // преобразуем строку в байты
                byte[] array = System.Text.Encoding.Default.GetBytes(LastItem);
                // запись массива байтов в файл
                fstream.Write(array, 0, array.Length);
                
            }
        }

        string FileRead()
        {
            try
            {
                file = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "path.txt");
                using (FileStream fstream = File.OpenRead(file))
                {
                    // преобразуем строку в байты
                    byte[] array = new byte[fstream.Length];
                    // считываем данные
                    fstream.Read(array, 0, array.Length);
                    // декодируем байты в строку
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    return textFromFile;
                }
            }
            catch (Exception) { return ""; }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            string data = "";
            string readrow;
            string readrow2;
            int data1;
            byte it;
            byte[] send = new byte[1];
            // string[] csv = File.ReadAllLines(pathToFile);
            readrow = "";
            readrow2 = "";
            char[] ar = new char[] { 'C' };
            it = 0;
            OpenPort();
            sp.Write(Convert.ToString(ar[0]));
            do
            {
                data1 = sp.ReadByte();
                it++;
                var datach = Convert.ToChar(data1);
                Thread.Sleep(1);
            }
            while (Convert.ToChar(data1) != Convert.ToChar(ar[0]) && it != 100);
            readrow += Convert.ToChar(data1);
            send[0] = Convert.ToByte('\r');
            sp.Write(send, 0, 1);
            do
            {
                data = sp.ReadLine();
                it++;
                var datach = Convert.ToChar(data1);
                Thread.Sleep(1);
            }
            while (data == "" && it != 100); //"Ok           "
                                             //data = sp.ReadLine();
                                             //TxbCom.Text = TxbCom.Text + '\r' + '\n';
            data = sp.ReadLine();
            readrow2 += data;
            TxbCom.Text = TxbCom.Text + readrow2 + '\r' + '\n';
            TxbCom.Text = TxbCom.Text + "Память очищена" + '\r' + '\n';
            TxbCom.ScrollToEnd();
            ClosePort();
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            SafeFile();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Файл импульса (*.csv)|*.csv" + "|Все файлы (*.*)|*.* "; ;
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllLines(saveFileDialog.FileName, csv);
        }
    }
}





