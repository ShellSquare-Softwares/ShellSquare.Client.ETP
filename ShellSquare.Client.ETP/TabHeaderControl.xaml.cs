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

namespace ShellSquare.Client.ETP
{
    /// <summary>
    /// Interaction logic for TabHeaderControl.xaml
    /// </summary>
    public partial class TabHeaderControl : UserControl
    {
        public Action<TabHeaderControl> CloseAction;
        public TabHeaderControl()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAction?.Invoke(this);
        }


        public string Tittle
        {
            get { return (string)GetValue(TittleProperty); }
            set { SetValue(TittleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Tittle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TittleProperty =
            DependencyProperty.Register("Tittle", typeof(string), typeof(TabHeaderControl), new UIPropertyMetadata("localhost"));
    }
}
