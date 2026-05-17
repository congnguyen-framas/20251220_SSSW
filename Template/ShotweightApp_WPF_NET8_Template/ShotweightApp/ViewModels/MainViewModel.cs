
using ShotweightApp.Models;
using ShotweightApp.Core.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace ShotweightApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly WeightService _service;

        public ObservableCollection<StepModel> Steps { get; set; }

        private StepModel? _selectedStep;
        public StepModel? SelectedStep
        {
            get => _selectedStep;
            set
            {
                _selectedStep = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveCommand { get; }

        public MainViewModel(WeightService service)
        {
            _service = service;

            Steps = new ObservableCollection<StepModel>
            {
                new StepModel { Id=1, Status="Done", Machine="M01", Item="Outsole A", Std=35, Actual=36 },
                new StepModel { Id=2, Status="Pending", Machine="M02", Item="Midsole B", Std=40, Actual=39 }
            };

            SaveCommand = new RelayCommand(Save);
        }

        private void Save()
        {
            if (SelectedStep == null)
            {
                MessageBox.Show("Please select a row.");
                return;
            }

            if (_service.IsOverWeight(SelectedStep.Actual, SelectedStep.Std))
                MessageBox.Show("Over weight detected!");
            else
                MessageBox.Show("Within range.");
        }
    }
}
