﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for ExpandCollapseControl.xaml
    /// </summary>
    public partial class ExpandCollapseControl : UserControl
    {
        public ExpandCollapseControl()
        {
            InitializeComponent();
        }



        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ExpandCollapseControl), new UIPropertyMetadata(new CornerRadius(0)));

        public bool Expanded
        {
            get { return (bool)GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Expanded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExpandedProperty =
            DependencyProperty.Register("Expanded", typeof(bool), typeof(ExpandCollapseControl), new UIPropertyMetadata(false));


    }
}
