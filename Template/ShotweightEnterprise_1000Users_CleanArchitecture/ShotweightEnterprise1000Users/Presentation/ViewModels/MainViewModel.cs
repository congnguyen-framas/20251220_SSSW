
using ShotweightEnterprise1000Users.Application.Interfaces;
using ShotweightEnterprise1000Users.Domain.Entities;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ShotweightEnterprise1000Users.Presentation.ViewModels
{
    public class MainViewModel
    {
        private readonly IStepRepository _repo;

        public ObservableCollection<Step> Steps { get; set; } = new();

        public MainViewModel(IStepRepository repo)
        {
            _repo = repo;
            Load();
        }

        private async Task Load()
        {
            var list = await _repo.GetAllAsync();
            foreach (var item in list)
                Steps.Add(item);
        }
    }
}
