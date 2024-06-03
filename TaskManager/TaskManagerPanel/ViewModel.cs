using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TaskManager.TaskManagerPanel
{
    public class ViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Task> tasks = new ObservableCollection<Task>();
        public ObservableCollection<Task> Tasks
        {
            get { return tasks; }
            set { tasks = value; OnPropertyChanged("Tasks"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Task
    {
        public string TaskName { get; set; }
        public string CreatorName { get; set; }
        public string CreationDate { get; set; }
        public string Description { get; set; }
        public BitmapImage Screenshot { get; set; }
        public BitmapImage InkImage { get; set; }

        // Добавляем свойства для сохранения информации о виде
        public System.Windows.Point StartPointImg { get; set; }
        public System.Windows.Point EndtPointImg { get; set; }
        public XYZ RevitStartPointCoordinates { get; set; }
        public XYZ RevitEndPointCoordinates { get; set; }
        public ElementId ViewId { get; set; }
        public double Scale { get; set; }
    }
}
