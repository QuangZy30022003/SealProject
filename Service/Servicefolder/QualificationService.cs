using AutoMapper;
using Common.DTOs.QualifiedFinealTeamDto;
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
    public class QualificationService : IQualificationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public QualificationService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<QualifiedTeamDto>> GenerateQualifiedTeamsAsync(int phaseId)
        {
            const int quantity = 8;

            // 0. Lấy phase hiện tại
            var currentPhase = await _uow.HackathonPhases.GetByIdAsync(phaseId);
            if (currentPhase == null)
                return new List<QualifiedTeamDto>();

            // 1. Tìm phase chấm điểm (phase trước theo thời gian)
            var scoringPhase = (await _uow.HackathonPhases.GetAllAsync(
                filter: p =>
                    p.HackathonId == currentPhase.HackathonId &&
                    p.EndDate < currentPhase.StartDate
            ))
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefault();

            if (scoringPhase == null)
                return new List<QualifiedTeamDto>();

            int scoringPhaseId = scoringPhase.PhaseId;

            // 2. Lấy tất cả group thuộc PHASE CHẤM ĐIỂM
            var groups = await _uow.Groups.GetAllIncludingAsync(
                g => g.Track.PhaseId == scoringPhaseId,
                g => g.GroupTeams,
                g => g.Track
            );

            if (!groups.Any())
                return new List<QualifiedTeamDto>();
            var penalties = await _uow.PenaltiesBonuses.GetAllAsync(
      p => p.PhaseId == scoringPhaseId && !p.IsDeleted
  );

            decimal GetAdjustedScore(GroupTeam gt)
            {
                var penalty = penalties
                    .Where(p => p.TeamId == gt.TeamId)
                    .Sum(p => p.Points);

                return (gt.AverageScore ?? 0) + penalty;
            }
            var topTeams = new List<GroupTeam>();

            // 3. Lấy team cao điểm nhất mỗi group
            foreach (var group in groups)
            {
                var top = group.GroupTeams
                    .Where(gt => gt.AverageScore.HasValue)
                    .OrderByDescending(gt => gt.AverageScore)
                    .FirstOrDefault();

                if (top != null)
                    topTeams.Add(top);
            }

            // 4. Nếu chưa đủ 8 → lấy thêm từ toàn phase chấm điểm
            if (topTeams.Count < quantity)
            {
                int need = quantity - topTeams.Count;

                var additional = (await _uow.GroupsTeams.GetAllAsync(
                    filter: gt =>
                        gt.AverageScore != null &&
                        gt.Group.Track.PhaseId == scoringPhaseId,
                    includeProperties: "Team,Group,Group.Track"
                ))
                .Where(x => !topTeams.Any(t => t.TeamId == x.TeamId))
                .OrderByDescending(x => x.AverageScore)
                .Take(need)
                .ToList();

                topTeams.AddRange(additional);
            }

            // 5. Chốt đúng 8 team
            topTeams = topTeams
                .OrderByDescending(t => t.AverageScore)
                .Take(quantity)
                .ToList();

            // 6. Lưu FinalQualification cho PHASE HIỆN TẠI
            foreach (var item in topTeams)
            {
                bool exists = await _uow.FinalQualifications.ExistsAsync(
                    f => f.TeamId == item.TeamId && f.PhaseId == phaseId
                );

                if (exists) continue;

                var final = new FinalQualification
                {
                    TeamId = item.TeamId,
                    GroupId = item.GroupId,
                    PhaseId = phaseId,              // phase sau
                    TrackId = item.Group.TrackId,
                    QualifiedAt = DateTime.UtcNow
                };

                await _uow.FinalQualifications.AddAsync(final);
            }

            await _uow.SaveAsync();

            // 7. Map ra DTO
            var result = _mapper.Map<List<QualifiedTeamDto>>(topTeams);
            foreach (var dto in result)
            {
                var gt = topTeams.First(x => x.TeamId == dto.TeamId);
                dto.AverageScore = GetAdjustedScore(gt);
            }

            return result;
        }

        public async Task<List<QualifiedTeamDtos>> GetFinalQualifiedTeamsAsync(int phaseId)
        {
            // 1) Phase người dùng truyền vào
            var inputPhase = await _uow.HackathonPhases.GetByIdAsync(phaseId);
            if (inputPhase == null)
                throw new ArgumentException("Phase not found");

            var hackathonId = inputPhase.HackathonId;

            // 2) Lấy tất cả phase của hackathon
            var allPhases = await _uow.HackathonPhases
                .GetAllAsync(p => p.HackathonId == hackathonId);

            if (!allPhases.Any())
                throw new Exception("No phases found for this hackathon");

            // Convert helper (DateOnly? + DateTime? → DateTime?)
            DateTime? ToDate(object d)
            {
                if (d == null) return null;
                if (d is DateOnly dd) return new DateTime(dd.Year, dd.Month, dd.Day);
                if (d is DateTime dt) return dt;
                return null;
            }

            // 3) Tìm final phase = phase có EndDate lớn nhất
            var finalPhase = allPhases
                .Where(p => ToDate(p.EndDate).HasValue)
                .OrderByDescending(p => ToDate(p.EndDate).Value)
                .FirstOrDefault();

            if (finalPhase == null)
                throw new Exception("No valid final phase found");

            // 4) Kiểm tra user có nhập final phase hay không
            if (finalPhase.PhaseId != phaseId)
                throw new Exception("This is not the final phase");

            // 5) Lấy danh sách final qualified
            var finals = await _uow.FinalQualifications.GetAllIncludingAsync(
  f => f.Team.HackathonId == hackathonId,
  f => f.Team,
                f => f.Group,
                f => f.Track
            );

            // 6) Map thủ công để chắc chắn không lỗi
            return finals.Select(f => new QualifiedTeamDtos
            {
                TeamId = f.TeamId,
                TeamName = f.Team?.TeamName,

                GroupId = f.GroupId,
                GroupName = f.Group?.GroupName,

                TrackName = f.Track?.Name
            }).ToList();
        }


    }
}
