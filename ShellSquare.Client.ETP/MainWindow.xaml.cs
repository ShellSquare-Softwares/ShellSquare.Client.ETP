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

using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ShellSquare.Client.ETP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CreateTab();
        }

        private void CreateTab()
        {
            SingleClientControl singleClient = new SingleClientControl();
            TabHeaderControl tabHeader = new TabHeaderControl();
            tabHeader.CloseAction = CloseTab;
            TabItem tabItem = new TabItem();
            tabItem.Content = singleClient;
            tabItem.Header = tabHeader;
            singleClient.SetHeader(tabHeader);
            int tabItemIndex = MainTabControl.Items.Count - 1;
            MainTabControl.Items.Insert(tabItemIndex, tabItem);
            MainTabControl.SelectedIndex = tabItemIndex;
        }

        private void CloseTab(TabHeaderControl tabHeader)
        {
            for (int i = 0; i < MainTabControl.Items.Count; i++)
            {
                TabItem item = MainTabControl.Items.GetItemAt(i) as TabItem;
                TabHeaderControl tabHeaderControl = item.Header as TabHeaderControl;
                if (tabHeaderControl != null && tabHeaderControl == tabHeader)
                {
                    if (i != 0)
                    {
                        if (MainTabControl.SelectedIndex == i)
                        {
                            MainTabControl.SelectedIndex = i - 1;
                        }
                    }

                    MainTabControl.Items.RemoveAt(i);
                    break;
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if(MainTabControl.SelectedIndex == MainTabControl.Items.Count - 1)
            {
                CreateTab();
            }

        }



    }
}
