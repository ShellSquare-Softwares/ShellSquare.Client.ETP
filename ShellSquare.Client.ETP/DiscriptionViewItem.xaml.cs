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
    /// Interaction logic for DiscriptionViewItem.xaml
    /// </summary>
    public partial class DiscriptionViewItem : UserControl
    {
        public DiscriptionViewItem()
        {
            InitializeComponent();
            DisplayName = "Test Well";
        }

        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(DiscriptionViewItem), new PropertyMetadata(""));



        public string Eml
        {
            get { return (string)GetValue(EmlProperty); }
            set { SetValue(EmlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Eml.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmlProperty =
            DependencyProperty.Register("Eml", typeof(string), typeof(DiscriptionViewItem), new PropertyMetadata(""));


        public string ItemUid
        {
            get { return (string)GetValue(ItemUidProperty); }
            set { SetValue(ItemUidProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemUid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemUidProperty =
            DependencyProperty.Register("ItemUid", typeof(string), typeof(DiscriptionViewItem), new PropertyMetadata(""));

    }
}
