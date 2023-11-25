using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion.ViewModels
{
    public class ChartDataPoint : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime xValue;

        private double yValue;

        public ChartDataPoint(DateTime xValue, double yValue)
        {
            XValue = xValue;
            YValue = yValue;
        }

        public DateTime XValue
        {
            get
            {
                return xValue;
            }
            set
            {
                xValue = value;
                RaisePropertyChanged("XValue");
            }
        }

        
        public double YValue
        {
            get
            {
                return yValue;
            }
            set
            {
                yValue = value;
                RaisePropertyChanged("YValue");
            }
        }

        private void RaisePropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
