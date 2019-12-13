using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
using static System.Linq.Enumerable;

namespace UtilityReactive.DemoApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var dtn = DateTime.Now.AddDays(1);
            var xx = Range(0, 10).Select((a) => dtn.AddMinutes(a)).ToArray(); ;
            ListView1.ItemsSource = xx;

            var new1 = UtilityReactive.TimeHelper.ChangeRate(xx, 60).AdjustAllByFirstTo(DateTime.Now).ToArray();
            ListView2.ItemsSource = new1.Select(t=>t.ToLongTimeString());

            UtilityReactive.TimeHelper.ChangeRate(xx, 60).AdjustAllByFirstTo(DateTime.Now).ByTimeStamp()

                .Subscribe(a =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    ListView1.SelectedIndex = ListView1.SelectedIndex + 1;
                    ListView2.SelectedIndex = ListView2.SelectedIndex + 1;
                });
            });
        }
    }
}
