using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShellSquare.Client.ETP
{
    public class ChannelItem : INotifyPropertyChanged
    {
        private bool m_HasExpanded;
        public ChannelItem()
        {
            ChannelItems = new List<ChannelItem>();
        }
        public List<ChannelItem> ChannelItems { get; private set; }
        public string Name { get; set; }
        public string Eml { get; set; }
        public int Level { get; set; }
        public string Uid { get; set; }
        public int ChildrensCount { get; set; }
        public bool HasChildren
        {
            get
            {
                if (ChildrensCount > 0 || ChildrensCount == -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public Visibility CanExpand
        {
            get
            {
                if (ChildrensCount > 0 || ChildrensCount == -1)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public bool HasChildrenLoaded { get; set; }        
        public Visibility CanSelect
        {
            get
            {
                if (Eml.ToLower().Contains("logcurveinfo"))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }
        public bool ChildrensLoaded { get; set; }
        public bool HasExpanded
        {
            get
            {
                return m_HasExpanded;
            }
            set
            {
                m_HasExpanded = value;
                RaisePropertyChanged("HasExpanded");
            }
        }
        public string DisplayName
        {
            get
            {
                if (ChildrensCount > 0)
                {
                    return $"{Name} ({ChildrensCount})";
                }
                else
                {
                    return $"{Name}";
                }
            }
        }
        public string Space
        {
            get
            {
                string space = "";
                for (int i = 0; i < Level; i++)
                {
                    space = space + "   ";
                }
                return space;
            }
        }
        private bool m_Selected;
        public bool Selected
        {
            get
            {
                return m_Selected;
            }
            set
            {
                m_Selected = value;
                RaisePropertyChanged("Selected");
            }
        }
        private bool m_Visible = true;
        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                m_Visible = value;
                RaisePropertyChanged("Visible");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
