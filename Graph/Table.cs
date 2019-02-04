using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    public class Table : INotifyPropertyChanged
    {
        private int time { get; set; }
        private int current { get; set; }
        private bool cod { get; set; }

        public int Time
        {
            get { return time; }
            set
            {
                time = value;
                OnPropertyChanged("Time");
            }
        }

        public int Current
        {
            get { return current; }
            set
            {
                current = value;
                OnPropertyChanged("Current");
            }
        }
        
        public bool Cod
        {
            get { return cod; }
            set
            {
                cod = value;
                OnPropertyChanged("Cod");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}

