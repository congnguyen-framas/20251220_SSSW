
using ShotweightEnterprise1000Users.Presentation.ViewModels;
using System.Windows;

namespace ShotweightEnterprise1000Users.Presentation.Views
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
