using Microsoft.EntityFrameworkCore;
using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos
{
    public class SeasonRepository : GenericRepository<Season>, ISeasonRepository
    {
        private readonly SealDbContext _context;

        public SeasonRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Season?> GetByCodeAsync(string code)
        {
            return await _context.Seasons
                .FirstOrDefaultAsync(s => s.Code == code);
        }
        public async Task<Season?> GetByNameAsync(string name)
        {
            return await _context.Seasons
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
        }

        // chỉ thao tác update db, ko chứa logic
        public async Task UpdateSeasonAsync(Season season)
        {
            _context.Seasons.Update(season);
            await _context.SaveChangesAsync();
        }

    }

}
