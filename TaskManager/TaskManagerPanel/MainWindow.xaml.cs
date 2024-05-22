using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using TaskManager.Helpers;
using TaskManager.Models;
using TaskManager.Services;
using static TaskManager.Helpers.CaptureWindow;


namespace TaskManager.TaskManagerPanel
{

    public partial class MainWindow : Page, IDockablePaneProvider
    {
        
        public MainWindow(ViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var screenshotService = new ScreenshotService();
            var viewModel = new ViewModel(screenshotService, Process.GetCurrentProcess(), FlowDocReader, DiscriptionTextBox);

            viewModel.PrintScreen();
        }

        

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.VisibleByDefault = false;
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState()
            {
                DockPosition = DockPosition.Right,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }


        
    }
}
