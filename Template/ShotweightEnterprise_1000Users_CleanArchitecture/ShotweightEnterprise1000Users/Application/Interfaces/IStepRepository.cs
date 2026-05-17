
using ShotweightEnterprise1000Users.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShotweightEnterprise1000Users.Application.Interfaces
{
    public interface IStepRepository
    {
        Task<List<Step>> GetAllAsync();
        Task AddAsync(Step step);
    }
}
