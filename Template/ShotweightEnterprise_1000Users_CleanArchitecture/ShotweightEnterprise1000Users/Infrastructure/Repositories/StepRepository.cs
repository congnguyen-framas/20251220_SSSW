
using Microsoft.EntityFrameworkCore;
using ShotweightEnterprise1000Users.Application.Interfaces;
using ShotweightEnterprise1000Users.Domain.Entities;
using ShotweightEnterprise1000Users.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShotweightEnterprise1000Users.Infrastructure.Repositories
{
    public class StepRepository : IStepRepository
    {
        private readonly AppDbContext _context;

        public StepRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Step>> GetAllAsync()
        {
            return await _context.Steps.ToListAsync();
        }

        public async Task AddAsync(Step step)
        {
            _context.Steps.Add(step);
            await _context.SaveChangesAsync();
        }
    }
}
