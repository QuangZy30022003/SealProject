using Common.DTOs.ChapterDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IChapterService
    {
        Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto, int userId);
        Task<ChapterDto?> GetByIdAsync(int id);
        Task<IEnumerable<ChapterDto>> GetAllAsync();
        Task<ChapterDto?> UpdateAsync(int id, UpdateChapterDto dto);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
