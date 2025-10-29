using AutoMapper;
using Common.DTOs.HackathonDto;
using Microsoft.EntityFrameworkCore;
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
    public class HackathonFullService : IHackathonFullService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IPhaseChallengeService _phaseChallengeService;
        public HackathonFullService(IUOW uow, IMapper mapper, IPhaseChallengeService phaseChallengeService)
        {
            _uow = uow;
            _mapper = mapper;
            _phaseChallengeService = phaseChallengeService;
        }
        public async Task<int> CreateFullHackathonAsync(HackathonFullCreateDto dto, int userid)
        {
            using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                // ✅ Kiểm tra Season tồn tại
                var season = await _uow.SeasonRepository.GetByIdAsync(dto.Hackathon.SeasonId);
                if (season == null)
                    throw new Exception($"Season with ID {dto.Hackathon.SeasonId} does not exist.");

                // 🔄 Chuyển DateOnly → DateTime để so sánh
                var seasonStart = season.StartDate;
                var seasonEnd = season.EndDate;

                // ⚙️ Kiểm tra null và convert
                if (dto.Hackathon.StartDate == null || dto.Hackathon.EndDate == null)
                    throw new Exception("Hackathon start and end date are required.");

                var hackathonStart = ((DateOnly)dto.Hackathon.StartDate).ToDateTime(TimeOnly.MinValue);
                var hackathonEnd = ((DateOnly)dto.Hackathon.EndDate).ToDateTime(TimeOnly.MinValue);

                // ✅ Validate ngày tháng hackathon
                if (hackathonStart >= hackathonEnd)
                    throw new Exception("Hackathon start date must be before end date.");

                if (hackathonStart < seasonStart || hackathonEnd > seasonEnd)
                    throw new Exception(
                        $"Hackathon dates must fall within the season period ({seasonStart:dd/MM/yyyy} - {seasonEnd:dd/MM/yyyy})."
                    );

                // ✅ Tạo Hackathon
                var hackathon = _mapper.Map<Hackathon>(dto.Hackathon);
                hackathon.CreatedBy = userid;
                hackathon.SeasonId = season.SeasonId;
                await _uow.Hackathons.AddAsync(hackathon);
                await _uow.SaveAsync();

                // ✅ Kiểm tra và thêm Phase
                if (dto.Phases != null && dto.Phases.Any())
                {
                    var phases = _mapper.Map<List<HackathonPhase>>(dto.Phases);
                    phases = phases.OrderBy(p => p.StartDate).ToList();

                    for (int i = 0; i < phases.Count; i++)
                    {
                        var phase = phases[i];
                        var phaseStart = phase.StartDate;
                        var phaseEnd = phase.EndDate;

                        // ✅ Ngày phase hợp lệ
                        if (phaseStart >= phaseEnd)
                            throw new Exception($"Phase {i + 1} has invalid dates (start >= end).");

                        // ✅ Phase nằm trong Hackathon
                        if (phaseStart < hackathonStart || phaseEnd > hackathonEnd)
                            throw new Exception($"Phase {i + 1} must be within the Hackathon period.");

                        // ✅ Kiểm tra chồng lấn
                        if (i > 0)
                        {
                            var prev = phases[i - 1];
                            if (phaseStart < prev.EndDate)
                                throw new Exception($"Phase {i + 1} overlaps with previous phase {i}.");
                        }

                        // ✅ Gán HackathonId
                        phase.HackathonId = hackathon.HackathonId;
                        await _uow.HackathonPhases.AddAsync(phase);
                    }
                }

                // ✅ Thêm Prize
                if (dto.Prizes != null && dto.Prizes.Any())
                {
                    var prizes = _mapper.Map<List<Prize>>(dto.Prizes);
                    foreach (var prize in prizes)
                    {
                        prize.HackathonId = hackathon.HackathonId;
                        await _uow.Prizes.AddAsync(prize);
                    }
                }

                await _uow.SaveAsync();
                await transaction.CommitAsync();

                // ✅ Auto-assign challenges nếu bật
                if (dto.AutoAssignChallenges)
                {
                    await _phaseChallengeService.AssignRandomChallengesToHackathonPhasesAsync(
                        hackathon.HackathonId, dto.ChallengesPerPhase
                    );
                }

                return hackathon.HackathonId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
