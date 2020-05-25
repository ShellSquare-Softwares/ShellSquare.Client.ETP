using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace ShellSquare.Client.ETP
{
    /// <summary>
    /// Interaction logic for SingleClientControl.xaml
    /// </summary>
    public partial class SingleClientControl : UserControl
    {
        private FileHandler m_FileHandler;
        

        private string m_LogCount;
        private string m_MessageCount;
        private TabHeaderControl m_TabHeaderControl;
        private string m_FilePath;
        private readonly DateTime m_Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToUniversalTime();
        private int m_Scale = 1000;
        private SolidColorBrush m_Brush;
        private NotifyObservableCollection<ChannelItem> m_ChannelItems;
        private Dictionary<long, ChannelIndexTypes> m_ChannelTypes;
        private ETPHandler m_ETPHandler;
        private CancellationTokenSource m_Source;
        private CancellationToken m_Token;
        private DataTable m_TimeDataTable;
        private DataTable m_DepthDataTable;
        TimeDepthBasedData m_Timebasedwindowform;
        System.Windows.Forms.CheckedListBox CheckedlstBox;
        private List<UserDetails> userDataList = new List<UserDetails>();

        internal void SetHeader(TabHeaderControl tabHeader)
        {
            m_TabHeaderControl = tabHeader;
        }

        public SingleClientControl()
        {
            InitializeComponent();
            LoadPreference();
            m_FileHandler = new FileHandler();

            m_Brush = new SolidColorBrush();
            m_Brush.Color = Color.FromRgb(133, 173, 173);
            m_Source = new CancellationTokenSource();
            m_Token = m_Source.Token;
            m_ChannelItems = new NotifyObservableCollection<ChannelItem>();
            m_ChannelTypes = new Dictionary<long, ChannelIndexTypes>();
            m_ETPHandler = new ETPHandler();
            m_ETPHandler.Message = Message;
            m_ETPHandler.ChannelItemsReceived = ChannelItemsReceived;
            m_ETPHandler.ChannelChildrensReceived = ChannelChildrensReceived;

            MessageDisplay.Document.PageWidth = 10000;
            MessageDisplay.SelectAll();
            MessageDisplay.Selection.Text = "";
            Channels.ItemsSource = m_ChannelItems;
            m_TimeDataTable = new DataTable();
            m_DepthDataTable = new DataTable();

            DataColumn index = new DataColumn();
            index.ColumnName = "Time";
            index.Caption = "Time";
            index.DataType = typeof(DateTime);
            m_TimeDataTable.Columns.Add(index);
            m_TimeDataTable.PrimaryKey = new DataColumn[] { index };

            index = new DataColumn();
            index.ColumnName = "Depth";
            index.Caption = "Depth";
            index.DataType = typeof(double);
            m_DepthDataTable.Columns.Add(index);
            m_DepthDataTable.PrimaryKey = new DataColumn[] { index };

            TimeDataDisplay.DataContext = m_TimeDataTable.DefaultView;
            DepthDataDisplay.DataContext = m_DepthDataTable.DefaultView;

            m_ETPHandler.ChannelDataReceived = ChannelDataReceived;
            m_ETPHandler.ChannelInfoReceived = ChannelInfoReceived;

            DateTime dt = DateTime.Today;
            EndDateTime.Value = dt.ToUniversalTime();
            StartDateTime.Value = dt.AddDays(-1).ToUniversalTime();

            m_LogCount = LogCountText.Text;
            m_MessageCount = MessageCountText.Text;
        }


        private void SettinClose_Click(object sender, RoutedEventArgs e)
        {
            SettingExpander.IsExpanded = false;
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (m_ETPHandler.HasConnected == false)
                {
                    await Connect();
                }

                await Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        if (m_ETPHandler.HasConnected)
                        {
                            break;
                        }

                        Thread.Sleep(1000);
                    }

                    await Channels.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            m_ChannelItems.Clear();
                            string eml = EmlUrl.Text;
                            string message = $"Discovering the {eml}";
                            await Display(message);

                            await m_ETPHandler.Discover(eml, null);

                        }
                        catch (Exception ex)
                        {
                            await DisplayError(ex.Message);
                        }
                    });


                });

            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private void Channels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }

        private async void Channels_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            await PerformGridClick(dep);
        }

        private void TimeMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem menuItem = (MenuItem)sender;
                DataColumn selectedColumn = menuItem.Tag as DataColumn;
                foreach (DataGridColumn column in TimeDataDisplay.Columns)
                {
                    if (column.Header.ToString() == selectedColumn.Caption)
                    {
                        if (menuItem.IsChecked)
                        {
                            column.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            column.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                HideRowsNotHavingData(TimeDataDisplay);
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).ConfigureAwait(true);
            }
        }

        private async void Channels_Selected(object sender, RoutedEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            await PerformGridClick(dep);
        }

        private void DepthMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem menuItem = (MenuItem)sender;
                DataColumn selectedColumn = menuItem.Tag as DataColumn;
                foreach (DataGridColumn column in DepthDataDisplay.Columns)
                {
                    if (column.Header.ToString() == selectedColumn.Caption)
                    {
                        if (menuItem.IsChecked)
                        {
                            column.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            column.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                HideRowsNotHavingData(DepthDataDisplay);
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).ConfigureAwait(true);
            }
        }

        private void DataDisplay_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var col = e.Column as DataGridTextColumn;
            if (col != null)
            {
                col.MinWidth = 50;
            }
            if (e.PropertyType == typeof(System.DateTime))
            {
                col.Binding.StringFormat = "o";
            }

            string headername = e.Column.Header.ToString();

            if ((sender as DataGrid).Name == "DepthDataDisplay")
            {
                string updatedHeader = m_DepthDataTable.Columns[headername].Caption;
                e.Column.Header = updatedHeader;
            }
            else if ((sender as DataGrid).Name == "TimeDataDisplay")
            {
                string updatedHeader = m_TimeDataTable.Columns[headername].Caption;
                e.Column.Header = updatedHeader;
            }

        }

        private async void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (StreamButton.Content.ToString() == "Stream")
                {
                    m_FileHandler.SaveToCSVFile = SaveCSVCheckbox.IsChecked.Value;
                    m_FileHandler.CombinedFile = CombinedCSVCheckbox.IsChecked.Value;
                    m_FileHandler.SetFolder(FolderLocation.Text);
                    m_FileHandler.Clear();

                    Settings settings = GetSettings(true);
                    StreamButton.Content = "Stop";
                    RangeButton.IsEnabled = false;
                    
                    bool isValidStreamRequest = await StartStream(settings);

                    if (!isValidStreamRequest)
                    {
                        StreamButton.Content = "Stream";
                        RangeButton.IsEnabled = true;
                    }
                }
                else
                {
                    StreamButton.Content = "Stream";
                    RangeButton.IsEnabled = true;
                    await StopStream();
                }

            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private void ChannelsGridSelect_Click(object sender, RoutedEventArgs e)
        {
            var items = Channels.SelectedCells;
            foreach (DataGridCellInfo cell in items)
            {
                var row = (ChannelItem)cell.Item;
                if (row.CanSelect == Visibility.Visible)
                {
                    row.Selected = true;
                }
            }
        }

        private void ChannelsGridDeselect_Click(object sender, RoutedEventArgs e)
        {
            var items = Channels.SelectedCells;
            foreach (DataGridCellInfo cell in items)
            {
                var row = (ChannelItem)cell.Item;
                if (row.CanSelect == Visibility.Visible)
                {
                    row.Selected = false;
                }
            }
        }

        private void ChannelsGridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var items = Channels.ItemsSource;
            foreach (ChannelItem item in items)
            {
                if (item.CanSelect == Visibility.Visible && item.Visible == true)
                {
                    item.Selected = true;
                }
            }
        }

        private void ChannelsGridSort_Click(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Channels.ItemsSource);
            view.SortDescriptions.Clear();


            int columnIndex = 1;
            if(this.Channels.ColumnFromDisplayIndex(columnIndex).SortDirection == ListSortDirection.Descending)
            {
                this.Channels.ColumnFromDisplayIndex(columnIndex).SortDirection = ListSortDirection.Ascending;
                view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else
            {
                this.Channels.ColumnFromDisplayIndex(columnIndex).SortDirection = ListSortDirection.Descending;
                view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
            }

            view.Refresh();

        }

        private void ChannelsGridDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            var items = Channels.ItemsSource;
            foreach (ChannelItem item in items)
            {
                if (item.CanSelect == Visibility.Visible && item.Visible == true)
                {
                    item.Selected = false;
                }
            }
        }

        private void ClearChannels_Click(object sender, RoutedEventArgs e)
        {
            // Channels.ItemsSource = null;
            m_ChannelItems.Clear();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectButton.Content.ToString() == "Connect")
            {
                if (m_ETPHandler.HasConnected == false)
                {
                    await Connect();
                }
            }
            else
            {                
                if (m_ETPHandler.HasConnected == true)
                {
                    Disconnect();
                    await Display("Disconnected ...");
                }
                ConnectButton.Content = "Connect";
            }
        }


        private void Disconnect()
        {
            m_Source.Cancel();
            m_Source = new CancellationTokenSource();
            m_Token = m_Source.Token;
            m_ETPHandler.Disconnect();
        }

        private async void RangeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                if (RangeButton.Content.ToString() == "Range")
                {
                    m_FileHandler.SaveToCSVFile = SaveCSVCheckbox.IsChecked.Value;
                    m_FileHandler.CombinedFile = CombinedCSVCheckbox.IsChecked.Value;
                    m_FileHandler.SetFolder(FolderLocation.Text);
                    m_FileHandler.Clear();

                    Settings settings = GetSettings(false);
                    RangeButton.Content = "Stop";
                    StreamButton.IsEnabled = false;
                    bool isValidRangeRequest = await GetRange(settings);

                    if (!isValidRangeRequest)
                    {
                        RangeButton.Content = "Range";
                        StreamButton.IsEnabled = true;
                    }
                }
                else
                {
                    RangeButton.Content = "Range";
                    StreamButton.IsEnabled = true;
                    await EndGetRange();
                }
            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private async void ChannelInfoReceived(ChannelMetadata metadata)
        {
            bool isTimebased = false;
            try
            {

                m_ChannelTypes.Clear();
                m_TimeDataTable = new DataTable();
                m_DepthDataTable = new DataTable();

                DataColumn index = new DataColumn();
                index.ColumnName = "Time";
                index.Caption = "Time";
                index.DataType = typeof(DateTime);
                m_TimeDataTable.Columns.Add(index);
                m_TimeDataTable.PrimaryKey = new DataColumn[] { index };

                index = new DataColumn();
                index.ColumnName = "Depth";
                index.Caption = "Depth";
                index.DataType = typeof(double);
                m_DepthDataTable.Columns.Add(index);
                m_DepthDataTable.PrimaryKey = new DataColumn[] { index };



                foreach (var item in metadata.Channels)
                {
                    DataColumn column = new DataColumn();
                    column.ColumnName = item.ChannelId.ToString();

                    if (string.IsNullOrEmpty(item.Uom))
                    {
                        column.Caption = $"{item.ChannelName} : {item.ChannelId}";
                    }
                    else
                    {
                        column.Caption = $"{item.ChannelName} ({item.Uom}) : {item.ChannelId}";
                    }


                    column.DataType = typeof(string);

                    if (item.Indexes[0].IndexType == ChannelIndexTypes.Time)
                    {
                        isTimebased = true;
                        m_TimeDataTable.Columns.Add(column);
                        m_ChannelTypes.Add(item.ChannelId, ChannelIndexTypes.Time);

                        m_FileHandler.SetHeder("Time", item.ChannelName, item.Uom, item.ChannelId);
                    }
                    else
                    {
                        m_DepthDataTable.Columns.Add(column);
                        m_ChannelTypes.Add(item.ChannelId, ChannelIndexTypes.Depth);
                        int tempScale = item.Indexes[0].Scale;
                        if (tempScale == 0)
                        {
                            m_Scale = 1000;
                        }
                        else
                        {
                            m_Scale = 1;
                            for (int i = 0; i < tempScale; i++)
                            {
                                m_Scale = m_Scale * 10;
                            }
                        }

                        m_FileHandler.SetHeder("Depth", item.ChannelName, item.Uom, item.ChannelId);
                    }
                }

                m_FileHandler.CloneTables(m_DepthDataTable, m_TimeDataTable);
            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }


            await TimeDataDisplay.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (isTimebased)
                    {
                        TimeBasedTabItem.IsSelected = true;
                    }

                    TimeDataDisplay.DataContext = m_TimeDataTable.DefaultView;
                }
                catch (Exception ex)
                {
                    DisplayError(ex.Message).ConfigureAwait(true);
                }
            });


            await DepthDataDisplay.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (!isTimebased)
                    {
                        DepthBasedTabItem.IsSelected = true;
                    }

                    DepthDataDisplay.DataContext = m_DepthDataTable.DefaultView;
                }
                catch (Exception ex)
                {
                    DisplayError(ex.Message).ConfigureAwait(true);
                }
            });
        }

        private void HideRowsNotHavingData(DataGrid grid)
        {
            int rowIndex;
            foreach (DataRowView row in grid.Items)
            {
                bool hasData = false;
                rowIndex = grid.Items.IndexOf(row);
                for (int i = 2; i < grid.Columns.Count; i++)
                {
                    DataGridColumn column = grid.Columns[i];
                    if (column.Visibility == Visibility.Visible)
                    {
                        if (!string.IsNullOrWhiteSpace(row[i].ToString()))
                        {
                            hasData = true;
                            break;
                        }
                    }
                }
                if (hasData == false)
                {
                    DataGridRow emptyRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
                    emptyRow.Visibility = Visibility.Collapsed;
                }
                if (hasData == true)
                {
                    DataGridRow emptyRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
                    emptyRow.Visibility = Visibility.Visible;
                }
            }
        }
       
        private void ChooseSelect_Click(object sender, RoutedEventArgs e)
        {
            m_Timebasedwindowform = new TimeDepthBasedData();
            m_Timebasedwindowform.ShowInTaskbar = false;
            CheckedlstBox = new CustomCheckedListBox();
            CheckedlstBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            CheckedlstBox.Location = new System.Drawing.Point(13, 32);

            try
            {

                m_Timebasedwindowform.Name = "Time";
                int i = 0;
                foreach (DataGridColumn column in TimeDataDisplay.Columns)
                {
                    if (column.Header == "Time")
                    {
                        continue;
                    }
                    CheckedlstBox.Font = new System.Drawing.Font("Times New Roman", 11.0f);
                    CheckedlstBox.Items.Add(column.Header.ToString().Split('-')[0]);
                    
                    if (column.Visibility == Visibility.Visible)
                    {
                        CheckedlstBox.SetItemCheckState(i, System.Windows.Forms.CheckState.Checked);
                        
                    }
                    i++;
                }
                CheckedlstBox.CheckOnClick = true;
                CheckedlstBox.ItemCheck += CheckedlstBox_SelectedIndexChanged;
                CheckedlstBox.HorizontalScrollbar = true;
                CheckedlstBox.ClientSize = new System.Drawing.Size(m_Timebasedwindowform.Width-10, m_Timebasedwindowform.Height-80);
                m_Timebasedwindowform.Controls.Add(CheckedlstBox);
                m_Timebasedwindowform.Show();
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).ConfigureAwait(true);
            }
        }
        private void ChooseSelectDepth_Click(object sender, RoutedEventArgs e)
        {
            m_Timebasedwindowform = new TimeDepthBasedData();
            m_Timebasedwindowform.ShowInTaskbar = false;
            CheckedlstBox = new CustomCheckedListBox();
            CheckedlstBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            CheckedlstBox.Location = new System.Drawing.Point(13, 32);
            
            try
            {

                int i = 0;
                foreach (DataGridColumn column in DepthDataDisplay.Columns)
                {
                    if (column.Header == "Depth")
                    {
                        continue;
                    }
                    CheckedlstBox.Font = new System.Drawing.Font("Times New Roman", 11.0f);
                    CheckedlstBox.Items.Add(column.Header.ToString().Split('-')[0]);
                    if (column.Visibility == Visibility.Visible)
                    {
                        CheckedlstBox.SetItemCheckState(i, System.Windows.Forms.CheckState.Checked);
                    }
                    i++;
                }
                CheckedlstBox.CheckOnClick = true;
                CheckedlstBox.ItemCheck += CheckedlstBox_SelectedIndexChanged;
                CheckedlstBox.HorizontalScrollbar = true;
                CheckedlstBox.ClientSize = new System.Drawing.Size(m_Timebasedwindowform.Width-10, m_Timebasedwindowform.Height - 80);
                m_Timebasedwindowform.Controls.Add(CheckedlstBox);
                m_Timebasedwindowform.Show();


            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).ConfigureAwait(true);
            }
        }
        private void CheckedlstBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            System.Windows.Forms.CheckedListBox menuItem = (System.Windows.Forms.CheckedListBox)sender;
            if (menuItem.CheckedItems.Count >= 0)
            {
                string title = m_Timebasedwindowform.Name;
                string selecteditem = menuItem.SelectedItem.ToString();
                int i = 0;

                if (title.Equals("Time"))
                {
                    foreach (DataGridColumn column in TimeDataDisplay.Columns)
                    {
                        string[] columnlist = column.Header.ToString().Split('-');
                        if (columnlist[0] == selecteditem.ToString())
                        {

                            if (menuItem.GetSelected(i) == true)
                            {
                                if (column.Visibility == Visibility.Visible)
                                {
                                    column.Visibility = Visibility.Collapsed;
                                }
                                else { column.Visibility = Visibility.Visible; }

                            }
                            else
                            {
                                if (column.Visibility == Visibility.Visible)
                                {
                                    column.Visibility = Visibility.Collapsed;
                                }
                                else { column.Visibility = Visibility.Visible; }

                            }
                            i++;
                        }
                    }

                    HideRowsNotHavingData(TimeDataDisplay);
                }
                else
                {
                    foreach (DataGridColumn column in DepthDataDisplay.Columns)
                    {
                        string[] columnlist = column.Header.ToString().Split('-');
                        if (columnlist[0] == selecteditem.ToString())
                        {

                            if (menuItem.GetSelected(i) == true)
                            {
                                if (column.Visibility == Visibility.Visible)
                                {
                                    column.Visibility = Visibility.Collapsed;
                                }
                                else { column.Visibility = Visibility.Visible; }

                            }
                            else
                            {
                                if (column.Visibility == Visibility.Visible)
                                {
                                    column.Visibility = Visibility.Collapsed;
                                }
                                else { column.Visibility = Visibility.Visible; }

                            }
                            i++;
                        }
                    }
                    HideRowsNotHavingData(DepthDataDisplay);
                }
            }
        }



        private async void ChannelDataReceived(IList<DataItem> dataItems)
        {
            StringBuilder records = new StringBuilder();
            await TimeDataDisplay.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    foreach (var item in dataItems)
                    {
                        int logTimeDepthCount;
                        var indexType = m_ChannelTypes[item.ChannelId];
                        if (indexType == ChannelIndexTypes.Time)
                        {
                            for (int i = 0; i < item.Indexes.Count; i++)
                            {
                                var index = item.Indexes[i];
                                var ticks = (m_Epoch.Ticks / 10 + index) * 10;
                                var indexDate = new DateTimeOffset(ticks, TimeSpan.Zero).UtcDateTime;
                                var result = m_TimeDataTable.AsEnumerable().Where(dr => dr.Field<DateTime>("Time") == indexDate);
                                if (result.Count() == 0)
                                {
                                    DataRow row = m_TimeDataTable.NewRow();
                                    row["Time"] = indexDate;

                                    row[item.ChannelId.ToString()] = item.Value.Item.ToString();
                                    m_TimeDataTable.Rows.Add(row);
                                    var header = m_TimeDataTable.Columns[item.ChannelId.ToString()].Caption;
                                    records.AppendLine(indexDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + " : " + header + " : " + item.Value.Item.ToString());

                                    m_FileHandler.WriteToFile(item.ChannelId, indexDate.ToUniversalTime().ToString("o"), item.Value.Item.ToString(), ChannelIndexTypes.Time);
                                    string date = indexDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
                                    StreamingData(date, header, item.Value.Item.ToString());

                                }
                                else
                                {
                                    foreach (var row in result)
                                    {
                                        row[item.ChannelId.ToString()] = item.Value.Item.ToString();
                                        var header = m_TimeDataTable.Columns[item.ChannelId.ToString()].Caption;
                                        records.AppendLine(indexDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + " : " + header + " : " + item.Value.Item.ToString());

                                        m_FileHandler.WriteToFile(item.ChannelId, indexDate.ToUniversalTime().ToString("o"), item.Value.Item.ToString(), ChannelIndexTypes.Time);
                                        string date = indexDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
                                        StreamingData(date, header, item.Value.Item.ToString());

                                    }
                                }
                                int logTimeCount;
                                logTimeDepthCount = int.TryParse(m_LogCount, out logTimeCount) ? logTimeCount : 100;
                                if (m_TimeDataTable.Rows.Count > logTimeDepthCount)
                                {
                                    m_TimeDataTable.Rows.RemoveAt(0);
                                }
                            }


                        }
                        else
                        {
                            for (int i = 0; i < item.Indexes.Count; i++)
                            {
                                double indexDepth = item.Indexes[i] / (double)m_Scale;
                                var result = m_DepthDataTable.AsEnumerable().Where(dr => dr.Field<double>("Depth") == indexDepth);
                                if (result.Count() == 0)
                                {
                                    DataRow row = m_DepthDataTable.NewRow();
                                    row[0] = true;
                                    row["Depth"] = indexDepth;
                                    row[item.ChannelId.ToString()] = item.Value.Item.ToString();
                                    m_DepthDataTable.Rows.Add(row);

                                    var header = m_DepthDataTable.Columns[item.ChannelId.ToString()].Caption;
                                    records.AppendLine(indexDepth.ToString($"F{4}") + " : " + header + " : " + item.Value.Item.ToString());

                                    m_FileHandler.WriteToFile(item.ChannelId, indexDepth.ToString(), item.Value.Item.ToString(), ChannelIndexTypes.Depth);
                                    string depth = indexDepth.ToString();
                                    StreamingData(depth, header, item.Value.Item.ToString());

                                }
                                else
                                {
                                    foreach (var row in result)
                                    {
                                        row[item.ChannelId.ToString()] = item.Value.Item.ToString();
                                        var header = m_DepthDataTable.Columns[item.ChannelId.ToString()].Caption;
                                        records.AppendLine(indexDepth.ToString($"F{4}") + " : " + header + " : " + item.Value.Item.ToString());

                                        m_FileHandler.WriteToFile(item.ChannelId, indexDepth.ToString(), item.Value.Item.ToString(), ChannelIndexTypes.Depth);
                                        string depth = indexDepth.ToString();
                                        StreamingData(depth, header, item.Value.Item.ToString());

                                    }
                                }
                                int logDepCount;
                                logTimeDepthCount = int.TryParse(m_LogCount, out logDepCount) ? logDepCount : 100;
                                if (m_DepthDataTable.Rows.Count > logTimeDepthCount)
                                {
                                    m_DepthDataTable.Rows.RemoveAt(0);
                                }
                            }
                        }
                    }

                    if (TimeDataDisplay.Items.Count > 0)
                    {
                        var border = VisualTreeHelper.GetChild(TimeDataDisplay, 0) as Decorator;
                        if (border != null)
                        {
                            var scroll = border.Child as ScrollViewer;
                            if (scroll != null) scroll.ScrollToEnd();
                        }
                    }
                    if (DepthDataDisplay.Columns.Count > 1)
                    {
                        var border = VisualTreeHelper.GetChild(DepthDataDisplay, 0) as Decorator;
                        if (border != null)
                        {
                            var scroll = border.Child as ScrollViewer;
                            if (scroll != null) scroll.ScrollToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    DisplayError(ex.Message).ConfigureAwait(true);
                }

            });
            if (!string.IsNullOrEmpty(m_FilePath))
            {
                await WriteToFile(records);
            }
            else { return; }
        }

        public async void StreamingData(string indexDate, string header, string result)
        {
            string message = $"{indexDate} : {header} : {result} ";
            await DisplayData(message);
        }



        public async Task WriteToFile(StringBuilder records)
        {
            try
            {
                using (FileStream fs = new FileStream(m_FilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    await streamWriter.WriteLineAsync(records.ToString());
                    await streamWriter.FlushAsync();
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void Message(string message, double timeTaken, TraceLevel level = TraceLevel.Info)
        {
            await Display(message, timeTaken, level);
        }

        private async Task Connect()
        {
            
            try
            {
                string url = EtpUrl.Text;
                m_TabHeaderControl.Tittle = url;
                string userName = UserName.Text;
                string password = UserPassword.Password;
                int maxDataItems = int.Parse(MaxItems.Text);
                int maxMessageRate = int.Parse(PollingRate.Text);

                string message = $"Connecting to {url}.\nuser name {userName}.";


                var protocols = new List<SupportedProtocol>();

                SupportedProtocol p;
                for (int i = 0; i < ProtocolList.Items.Count; i++)
                {
                    var c = (CheckBox)((ListViewItem)ProtocolList.Items[i]).Content;
                    if (c.IsChecked.Value)
                    {
                        switch (i)
                        {
                            case 0:
                                message = message + "\n" + "Protocol=ChannelStreaming, Role=producer";
                                p = ETPHandler.ToSupportedProtocol(Protocols.ChannelStreaming, "producer");
                                protocols.Add(p);
                                break;
                            case 1:
                                message = message + "\n" + "Protocol=Discovery, Role=store";
                                p = ETPHandler.ToSupportedProtocol(Protocols.Discovery, "store");
                                protocols.Add(p);
                                break;
                        }
                    }
                }

                if (protocols.Count == 0)
                {
                    MessageBox.Show("Please Select atleast one protocol", "ETP Client", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                await Display(message);

                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await m_ETPHandler.Connect(url, userName, password, maxDataItems, maxMessageRate, protocols, m_Token);
                        SaveToPreference(url, userName, password);
                        SetButtonStatus("Disconnect");
                    }
                    catch (Exception ex)
                    {
                        await DisplayError(ex.Message);
                        SetButtonStatus("Connect");
                    }

                    
                });

            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
                SetButtonStatus("Connect");
            }
        }



        private void SetButtonStatus(string caption)
        {
            ConnectButton.Dispatcher.Invoke(() =>
            {
                ConnectButton.Content = caption;
            });
        }


        //private async Task StreamBtnParameters()
        private async Task<bool> IsValidStreamParameters(Settings settings)
        {
            StringBuilder message = new StringBuilder();
            int count = 0;

            if (settings.ByIndexCount)
            {
                message.AppendLine($"Streaming initiated by index count.\n Index Count: {settings.IndexCount}");
                await Display(message.ToString());
            }

            else if (settings.BetweenTimeIndex)
            {
                if (!m_ChannelTypes.ContainsValue(ChannelIndexTypes.Time))
                {
                    message.AppendLine($"No time based curves selected for time based streaming request!");
                    await Display(message.ToString());
                    return false;
                }
                message.AppendLine($"Streaming initiated with below time parameters.\n Start Time Index : {settings.StartTimeInput}");
                message.AppendLine("Channels requested : [");
                foreach (var channel in m_ChannelTypes)
                {
                    count++;
                    if (channel.Value == ChannelIndexTypes.Time)
                    {
                        if (count % 7 == 0)
                            message.AppendLine(channel.Key.ToString());
                        else
                            message.Append(channel.Key.ToString() + ",  ");
                    }
                }
                message.AppendLine("]");
                await Display(message.ToString());
            }
            else if (settings.BetweenDepthIndex)
            {
                if (!m_ChannelTypes.ContainsValue(ChannelIndexTypes.Depth))
                {
                    message.AppendLine($"No depth based curves selected for depth based streaming request!");
                    await Display(message.ToString());
                    return false;
                }

                message.AppendLine($"Streaming initiated with below depth parameters.\n Start Depth Index : {settings.StartDepthInput}");
                message.AppendLine("Channels requested : [");
                foreach (var channel in m_ChannelTypes)
                {
                    count++;
                    if (channel.Value == ChannelIndexTypes.Depth)
                    {
                        if (count % 7 == 0)
                            message.AppendLine(channel.Key.ToString());
                        else
                            message.Append(channel.Key.ToString() + ",  ");
                    }
                }
                message.AppendLine("]");
                await Display(message.ToString());
            }
            return true;
        }
        private async Task<bool> IsValidRangeParameters(Settings settings)
        {
            StringBuilder message = new StringBuilder();
            int count = 0;

            if (settings.BetweenTimeIndex)
            {
                if (!m_ChannelTypes.ContainsValue(ChannelIndexTypes.Time))
                {
                    message.AppendLine("No time based curves selected for time based range request!");
                    await Display(message.ToString());
                    return false;
                }

                message.AppendLine($"Range request initiated with below time parameters.\n Start Time Index : {settings.StartTimeInput}\n End Time Index : {settings.EndTimeInput}");
                message.AppendLine("Channels requested : [");
                
                foreach (var channel in m_ChannelTypes )
                {
                    count++;
                    if(channel.Value == ChannelIndexTypes.Time)
                    {
                        if(count % 7 == 0)
                            message.AppendLine(channel.Key.ToString());
                        else
                            message.Append(channel.Key.ToString() + ",  ");
                    }
                }
                message.AppendLine("]");
                await Display(message.ToString());
            }
            if (settings.BetweenDepthIndex)
            {
                if (!m_ChannelTypes.ContainsValue(ChannelIndexTypes.Depth))
                {
                    message.AppendLine("No depth based curves selected for depth based range request!");
                    await Display(message.ToString());
                    return false;
                }

                message.AppendLine($"Range request initiated with below depth parameters.\n Start Depth Index : {settings.StartDepthInput}\n End Depth Index :{settings.EndDepthInput}");
                message.AppendLine("Channels requested : [");
                foreach (var channel in m_ChannelTypes)
                {
                    count++;
                    if (channel.Value == ChannelIndexTypes.Depth)
                    {
                        if (count % 7 == 0)
                            message.AppendLine(channel.Key.ToString());
                        else
                            message.Append(channel.Key.ToString() + ",  ");
                    }
                }
                message.AppendLine("]");
                await Display(message.ToString());
            }

            return true;
        }

        private void SaveToPreference(string url, string userName, string password)
        {
            if (userDataList.Where(x => x.Url.Equals(url)).Count() == 0)
            {
                UserDetails user = new UserDetails();
                user.Url = url;
                user.UserName = userName;
                user.Password = password;
                userDataList.Add(user);
            }
            else if (userDataList.Where(x => x.Url.Equals(url) && (x.UserName != userName || x.Password != password)).Count() > 0)
            {
                UserDetails user = userDataList.Where(x => x.Url.Equals(url)).FirstOrDefault();
                user.Url = url;
                user.UserName = userName;
                user.Password = password;
            }

            string json = JsonConvert.SerializeObject(userDataList);
            Properties.Settings.Default.UserCredentials = json;
            Properties.Settings.Default.Save();
        }

        private void LoadPreference()
        {

            string jsonString = Properties.Settings.Default.UserCredentials;
            userDataList = JsonConvert.DeserializeObject<List<UserDetails>>(jsonString);
            var userDetail = userDataList?.FirstOrDefault();
            EtpUrl.Text = userDetail?.Url;
            UserName.Text = userDetail?.UserName;
            UserPassword.Password = userDetail?.Password;
            List<string> detail = new List<string>();
            if (userDataList == null)
            {
                userDataList = new List<UserDetails>();
            }
            foreach (var item in userDataList)
            {
                detail?.Add(item.Url);
            }
            EtpUrl.ItemsSource = detail;

            if (string.IsNullOrWhiteSpace(EtpUrl.Text))
            {
                EtpUrl.Text = "wss://localhost/etp";
            }
        }


        private async Task DisplayError(string message)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                p = new Paragraph(new Run(message));
                p.Foreground = Brushes.Red;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                MessageDisplay.ScrollToEnd();
            });
        }
        public async Task DisplayData(string message)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                Paragraph p = new Paragraph(new Run(message));
                p.Foreground = Brushes.Black;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                MessageDisplay.ScrollToEnd();
            });
        }

        private async Task Display(string message)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                p = new Paragraph(new Run(message));
                p.Foreground = Brushes.Black;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                MessageDisplay.ScrollToEnd();
            });
        }

        private async Task Display(string message, double timeTaken, TraceLevel level)
        {
            await MessageDisplay.Dispatcher.InvokeAsync(() =>
            {
                string timeMessage = $"{DateTime.UtcNow.ToString("o")}";

                Paragraph p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0, 10, 0, 0);

                timeMessage = $"Time taken : {timeTaken} (ms)";

                p = new Paragraph(new Run(timeMessage));
                p.Foreground = m_Brush;
                MessageDisplay.Document.Blocks.Add(p);
                p.Margin = new Thickness(0);

                if (level == TraceLevel.Error)
                {
                    p = new Paragraph(new Run(message));
                    p.Foreground = Brushes.Red;
                    MessageDisplay.Document.Blocks.Add(p);
                }
                else
                {
                    p = new Paragraph(new Run(message));
                    p.Foreground = Brushes.Black;
                    MessageDisplay.Document.Blocks.Add(p);
                }
                p.Margin = new Thickness(0);
                int totalCount = MessageDisplay.Document.Blocks.Count;
                int maxMessageCount, logmsgCount;
                maxMessageCount = int.TryParse(m_MessageCount, out logmsgCount) ? logmsgCount : 200;
                if (totalCount > maxMessageCount)
                {
                    for (int i = totalCount - maxMessageCount; i > 0; i--)
                    {
                        MessageDisplay.Document.Blocks.Remove(MessageDisplay.Document.Blocks.ElementAt(i));
                    }
                }
                MessageDisplay.ScrollToEnd();
            });
        }

        private async void ChannelItemsReceived(ChannelItem item)
        {
            try
            {
                item.HasExpanded = false;
                item.HasChildrenLoaded = false;
                await Channels.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        lock (m_ChannelItems)
                        {
                            m_ChannelItems.Add(item);
                        }

                    }
                    catch (Exception ex)
                    {
                        DisplayError(ex.Message).ConfigureAwait(true);
                    }
                });

            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private async void ChannelChildrensReceived(ChannelItem item, ChannelItem parent)
        {
            try
            {
                parent.ChildrensLoaded = true;

                if (item.DisplayName == "Time" || item.DisplayName == "DEP")
                {
                    item.Visible = false;
                }
                else
                {
                    parent.ChannelItems.Add(item);

                }

                item.HasExpanded = false;
                item.HasChildrenLoaded = false;
                await Channels.Dispatcher.InvokeAsync(() =>
                {

                    try
                    {
                        lock (m_ChannelItems)
                        {
                            int rowindex = m_ChannelItems.IndexOf(parent);

                            if (rowindex == m_ChannelItems.Count - 1)
                            {
                                m_ChannelItems.Add(item);
                            }
                            else
                            {
                                m_ChannelItems.Insert(rowindex + 1, item);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        DisplayError(ex.Message).ConfigureAwait(true);
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayError(ex.Message);
            }
        }

        private async Task PerformGridClick(DependencyObject dep)
        {
            while ((dep != null) &&
           !(dep is DataGridCell) &&
           !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
            }

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                if (cell.Column.DisplayIndex == 0)
                {
                    DataGridRow row = DataGridRow.GetRowContainingElement(cell);
                    ChannelItem item = (ChannelItem)row.Item;

                    if (item.HasChildren)
                    {
                        if (item.HasExpanded)
                        {
                            var currentRowIndex = Channels.Items.IndexOf(Channels.CurrentItem);

                            item.HasExpanded = false;
                            HideChildrens(item, currentRowIndex);
                        }
                        else
                        {
                            item.HasExpanded = true;

                            if (item.ChildrensLoaded)
                            {
                                var currentRowIndex = Channels.Items.IndexOf(Channels.CurrentItem);
                                DisplayChildrens(item, currentRowIndex);
                            }
                            else
                            {
                                string eml = item.Eml;
                                string message = $"Discovering the {eml}";
                                await Display(message);
                                await Task.Factory.StartNew(async () =>
                                {
                                    await m_ETPHandler.Discover(eml, item);
                                });
                            }
                        }
                    }
                }
            }
        }

        private void DisplayChildrens(ChannelItem item, int rowIndex)
        {
            for (int i = rowIndex + 1; i < m_ChannelItems.Count; i++)
            {
                if (item.Level >= m_ChannelItems[i].Level)
                {
                    break;
                }

                if (item.Level + 1 == m_ChannelItems[i].Level)
                {
                    m_ChannelItems[i].Visible = true;
                }

                if (m_ChannelItems[rowIndex].DisplayName.ToLower().Contains("time") && m_ChannelItems[i].DisplayName == "Time")
                {
                    m_ChannelItems[i].Visible = false;
                }
                if (m_ChannelItems[rowIndex].DisplayName.ToLower().Contains("depth") && m_ChannelItems[i].DisplayName == "DEP")
                {
                    m_ChannelItems[i].Visible = false;
                }

                m_ChannelItems[i].HasExpanded = false;
            }
        }

        private void HideChildrens(ChannelItem item, int rowIndex)
        {
            for (int i = rowIndex + 1; i < m_ChannelItems.Count; i++)
            {
                if (item.Level >= m_ChannelItems[i].Level)
                {
                    break;
                }

                m_ChannelItems[i].Visible = false;
                m_ChannelItems[i].HasExpanded = false;
            }
        }

        private async Task StopStream()
        {
            string messageClose = $" ]";
            await DisplayData(messageClose);
            string message = $"Stopping the data Stream";
            await Display(message);

            await Task.Factory.StartNew(async () =>
            {
                await m_ETPHandler.StopStreaming();
            });
        }

        private async Task<bool> StartStream(Settings settings)
        {
            bool validStreamParameters = false;
            string eml = "";
            List<string> channels = new List<string>();
            foreach (var item in m_ChannelItems)
            {
                if (item.Visible && item.Selected)
                {
                    channels.Add(item.Eml);
                    eml = eml + $"\n{item.Eml}";
                }
            }

            if (channels.Count == 0)
            {
                MessageBox.Show("Please Select atleast one channel", "ETP Client", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                StreamButton.Content = "Stream";
                RangeButton.IsEnabled = true;
                return false;
            }

            await Task.Run(async () =>
            {
                try
                {
                    if (m_ETPHandler.HasConnected == false)
                    {
                        await Connect();

                        while (true)
                        {
                            Thread.Sleep(1000);
                            if (m_ETPHandler.HasConnected)
                            {
                                break;
                            }
                        }
                    }

                    string message = $"Describing{eml}";
                    await Display(message);
                    await Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            m_ETPHandler.HasDescribing = true;
                            await m_ETPHandler.Describe(channels);

                        }
                        catch (Exception ex)
                        {
                            await DisplayError(ex.Message);
                        }
                    });



                    while (true)
                    {
                        Thread.Sleep(1000);
                        if (!m_ETPHandler.HasDescribing)
                        {
                            break;
                        }
                    }

                    validStreamParameters = await IsValidStreamParameters(settings);

                    if (validStreamParameters)
                    {
                        message = $"Streaming Data \nData: [";
                        await Display(message);
                        await Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                if (settings.BetweenDepthIndex)
                                {
                                    settings.StartDepth = (long)(settings.StartDepthInput * m_Scale);
                                    settings.EndDepth = (long)(settings.EndDepthInput * m_Scale);
                                }
                                await m_ETPHandler.StartStreaming(settings, m_ChannelTypes);

                            }
                            catch (Exception ex)
                            {
                                await DisplayError(ex.Message);
                            }
                        });
                    }

                }
                catch (Exception ex)
                {
                    await DisplayError(ex.Message);
                }
            });

            return validStreamParameters;
        }

        private Settings GetSettings(bool isStreamRequest)
        {
            Settings settings = new Settings();

            settings.ByIndexCount = true;
            int indexCountHolder = 0;
            if (!int.TryParse(IndexCount.Text, out indexCountHolder))
            {
                indexCountHolder = 1;
                IndexCount.Text = "1";
            }
            if (indexCountHolder == 0)
            {
                indexCountHolder = 1;
                IndexCount.Text = "1";
            }
            settings.IndexCount = indexCountHolder;



            if (BetweenTimeIndexOption.IsChecked.Value)
            {
                settings.BetweenTimeIndex = true;

                if (StartDateTime.Value.HasValue)
                {
                    var ticks = StartDateTime.Value.Value.ToUniversalTime().Ticks;
                    settings.StartTime = (ticks / 10) - (m_Epoch.Ticks / 10);
                    settings.StartTimeInput = StartDateTime.Value.Value.ToString();
                }
                else 
                { 
                    throw new Exception("Please Provide Valid StartTime Index value..."); 
                }

                if (!isStreamRequest)
                {
                    if (EndDateTime.Value.HasValue)
                    {
                        var ticks = EndDateTime.Value.Value.ToUniversalTime().Ticks;
                        settings.EndTime = (ticks / 10) - (m_Epoch.Ticks / 10);
                        settings.EndTimeInput = EndDateTime.Value.Value.ToString();
                    }
                    else 
                    { 
                        throw new Exception("Please provide Valid EndTime Index Value..."); 
                    }

                    if (settings.StartTime > settings.EndTime)
                    {
                        throw new Exception("End time should be greater than start time for range request!");
                    }
                }                
            }

            if (BetweenDepthIndex.IsChecked.Value)
            {
                settings.BetweenDepthIndex = true;

                if (!string.IsNullOrEmpty(StartDepthText.Text))
                {
                    settings.StartDepthInput = double.Parse(StartDepthText.Text);
                }
                else 
                { 
                    throw new Exception("Please provide valid StartDepth Index value..."); 
                }

                if (!isStreamRequest)
                {
                    if (!string.IsNullOrEmpty(EndDepthText.Text))
                    {
                        settings.EndDepthInput = double.Parse(EndDepthText.Text);
                    }
                    else 
                    { 
                        throw new Exception("Please provide Valid EndDepth Index value..."); 
                    }

                    if(settings.StartDepthInput > settings.EndDepthInput)
                    {
                        throw new Exception("End depth should be greater than start depth for range request!");
                    }
                }
            }

            return settings;
        }

        private void MessageCheckbox_Click(object sender, RoutedEventArgs e)
        {
            SetVisibility();
        }

        private void QueryCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SetVisibility();
        }

        private void SetVisibility()
        {
            if (MessageCheckbox.IsChecked.Value && QueryCheckBox.IsChecked.Value)
            {
                MessageColumn.Width = new GridLength(1, GridUnitType.Star);
                QueryColumn.Width = new GridLength(1, GridUnitType.Star);
                ContentColumn.Width = new GridLength(2, GridUnitType.Star);
            }
            else if (MessageCheckbox.IsChecked.Value)
            {
                MessageColumn.Width = new GridLength(1, GridUnitType.Star);
                QueryColumn.Width = new GridLength(0, GridUnitType.Star);
                ContentColumn.Width = new GridLength(2, GridUnitType.Star);
            }
            else if (QueryCheckBox.IsChecked.Value)
            {
                MessageColumn.Width = new GridLength(0, GridUnitType.Star);
                QueryColumn.Width = new GridLength(1, GridUnitType.Star);
                ContentColumn.Width = new GridLength(2, GridUnitType.Star);
            }
            else
            {
                MessageColumn.Width = new GridLength(0, GridUnitType.Star);
                QueryColumn.Width = new GridLength(0, GridUnitType.Star);
                ContentColumn.Width = new GridLength(2, GridUnitType.Star);
            }
        }

        private async Task<bool> GetRange(Settings settings)
        {
            bool validRangeParameters = false;
            string eml = "";
            List<string> channels = new List<string>();
            foreach (var item in m_ChannelItems)
            {
                if (item.Visible && item.Selected)
                {
                    channels.Add(item.Eml);
                    eml = eml + $"\n{item.Eml}";
                }
            }

            if (channels.Count == 0)
            {
                MessageBox.Show("Please Select atleast one channel", "ETP Client", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                RangeButton.Content = "Range";
                StreamButton.IsEnabled = true;
                return false;
            }

            await Task.Run(async () =>
            {
                try
                {
                    if (m_ETPHandler.HasConnected == false)
                    {
                        await Connect();

                        while (true)
                        {
                            Thread.Sleep(1000);
                            if (m_ETPHandler.HasConnected)
                            {
                                break;
                            }
                        }
                    }

                    string message = $"Describing {eml}";
                    await Display(message);
                    await Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            m_ETPHandler.HasDescribing = true;
                            await m_ETPHandler.Describe(channels);

                        }
                        catch (Exception ex)
                        {
                            await DisplayError(ex.Message);
                        }
                    });



                    while (true)
                    {
                        Thread.Sleep(1000);
                        if (!m_ETPHandler.HasDescribing)
                        {
                            break;
                        }
                    }

                    validRangeParameters = await IsValidRangeParameters(settings);

                    if (validRangeParameters)
                    {
                        message = $"Getting Range of data";
                        await Display(message);
                        await Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                if (settings.BetweenDepthIndex)
                                {
                                    settings.StartDepth = (long)(settings.StartDepthInput * m_Scale);
                                    settings.EndDepth = (long)(settings.EndDepthInput * m_Scale);
                                }
                                await m_ETPHandler.GetRange(settings, m_ChannelTypes);
                            }
                            catch (Exception ex)
                            {
                                await DisplayError(ex.Message);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    await DisplayError(ex.Message);
                }
            });

            return validRangeParameters;
        }

        private async Task EndGetRange()
        {

        }
        
        private void EtpUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {


                ComboBox menuItem = (ComboBox)sender; ;

                foreach (var d in userDataList)
                {
                    if (d?.Url == menuItem.SelectedItem?.ToString())
                    {
                        UserName.Text = d.UserName;
                        UserPassword.Password = d.Password;
                        EtpUrl.Text = d.Url;
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message).Wait();
            }
        }


        private void StartDepthText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void EndDepthText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void IndexCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            expControl.IsExpanded = !(expControl.IsExpanded);
        }

       
    }
}
