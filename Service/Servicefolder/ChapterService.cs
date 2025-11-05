using AutoMapper;
using Common.DTOs.ChapterDto;
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

        public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto)
        {
            // check trùng tên
            var exists = await _uow.Chapters.ExistsAsync(c => c.ChapterName == dto.ChapterName);
            if (exists)
            {
                throw new Exception("Chapter name already exists");
            }

            var entity = _mapper.Map<Chapter>(dto);

            await _uow.Chapters.AddAsync(entity);
            await _uow.SaveAsync();

            return _mapper.Map<ChapterDto>(entity);
        }
        public async Task<ChapterDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Chapters.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ChapterDto>(entity);
        }

        public async Task<IEnumerable<ChapterDto>> GetAllAsync()
        {
            var entities = await _uow.Chapters.GetAllAsync();
            return _mapper.Map<IEnumerable<ChapterDto>>(entities);
        }

        public async Task<ChapterDto?> UpdateAsync(int id, UpdateChapterDto dto)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id);
            if (chapter == null) return null;

            chapter.ChapterName = dto.ChapterName;
            _uow.Chapters.Update(chapter);
            await _uow.SaveAsync();

            return _mapper.Map<ChapterDto>(chapter);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id);
            if (chapter == null) return false;

            _uow.Chapters.Remove(chapter);
            await _uow.SaveAsync();
            return true;
        }
    }
}
