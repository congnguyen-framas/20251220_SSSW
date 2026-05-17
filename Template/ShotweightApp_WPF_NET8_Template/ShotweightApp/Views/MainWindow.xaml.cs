
using ShotweightApp.ViewModels;
using System.Windows;

namespace ShotweightApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
