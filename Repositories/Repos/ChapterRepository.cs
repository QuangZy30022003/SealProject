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
    public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
    {
        private readonly SealDbContext _context;

        public ChapterRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Chapters.AnyAsync(c => c.ChapterName == name);
        }
    }
}
