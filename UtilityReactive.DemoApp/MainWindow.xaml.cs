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
            var dtnPlus = DateTime.Now.AddDays(1);
            var xx = Range(0, 10).Select((a) => dtnPlus.AddMinutes(a)).ToArray(); ;
            ListView1.ItemsSource = xx;
            DateTime dtn = DateTime.Now;
            var arr = UtilityReactive.TimeHelper.ChangeRate(xx, 6).AdjustAllByFirstTo(DateTime.Now.AddSeconds(3)).ToArray();

            var items = arr.Select(a => new CountDown(a, TimeSpan.FromMilliseconds(100))).ToArray(); ;

            ListView1.SelectedIndex = 0;

            foreach (var xlx in items)
            {
                xlx.OnAnyPropertyChange().Subscribe(a =>
                {
                    if (a.TimeRemaining == default(TimeSpan))
                        this.Dispatcher.Invoke(() =>
                        {
                            ListView1.SelectedIndex = items.Select((a, i) => (i, a.TimeRemaining)).Where(a=>a.TimeRemaining>default(TimeSpan)).OrderBy(a => a.TimeRemaining).DefaultIfEmpty().FirstOrDefault().i;
                        });
                });
            }
            ListView1.ItemsSource = items;

    
        }
    }
}
