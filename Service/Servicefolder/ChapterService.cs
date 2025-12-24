using AutoMapper;
using Common.DTOs.ChapterDto;
using Common.DTOs.TeamDto;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class ChapterService : IChapterService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public ChapterService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto, int userId)
        {
            // check exist name
            var exists = await _uow.Chapters.ExistsAsync(c => c.ChapterName == dto.ChapterName);
            if (exists)
            {
                throw new InvalidOperationException("Chapter name already exists");
            }

            // 2. Check user đã tạo chapter chưa
            var existsLeader = await _uow.Chapters
                .ExistsAsync(c => c.ChapterLeaderId == userId);

            if (existsLeader)
                throw new InvalidOperationException("Each user can only create one chapter");

            var entity = _mapper.Map<Chapter>(dto);

            entity.ChapterLeaderId = userId;

            await _uow.Chapters.AddAsync(entity);
            await _uow.SaveAsync();

            var created = await _uow.Chapters.GetByIdIncludingAsync(
                c => c.ChapterId == entity.ChapterId,
                c => c.ChapterLeader
            );

            return _mapper.Map<ChapterDto>(created);
        }
        public async Task<ChapterDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Chapters.GetByIdIncludingAsync(c => c.ChapterId == id, c => c.ChapterLeader);
            return entity == null ? null : _mapper.Map<ChapterDto>(entity);
        }

        public async Task<IEnumerable<ChapterDto>> GetAllAsync()
        {
            var entities = await _uow.Chapters.GetAllIncludingAsync(null, c => c.ChapterLeader);
            return _mapper.Map<IEnumerable<ChapterDto>>(entities);
        }

        public async Task<ChapterDto?> UpdateAsync(int id, UpdateChapterDto dto)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id);
            if (chapter == null) return null;

            // just update if have value
            if (!string.IsNullOrWhiteSpace(dto.ChapterName))
                chapter.ChapterName = dto.ChapterName;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                chapter.Description = dto.Description;

            _uow.Chapters.Update(chapter);
            await _uow.SaveAsync();

            var updated = await _uow.Chapters.GetByIdIncludingAsync(c => c.ChapterId == id, c => c.ChapterLeader);
            return _mapper.Map<ChapterDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id);
            if (chapter == null) return false;

            if (chapter.ChapterLeaderId != userId)
                throw new UnauthorizedAccessException("You are not the leader of this chapter.");

            _uow.Chapters.Remove(chapter);
            await _uow.SaveAsync();
            return true;
        }
    }
}
