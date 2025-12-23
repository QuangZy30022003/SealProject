using AutoMapper;
using Common.DTOs.TeamTrackDto;
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
    public partial class TeamTrackService : ITeamTrackService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public TeamTrackService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<TeamSelectTrackResponse?> SelectTrackAsync(int userIdFromToken, TeamSelectTrackRequest request)
        {
            // Lấy team
            var team = await _uow.Teams.GetByIdAsync(request.TeamId);
            if (team == null)
                throw new Exception("Team not found.");
            var track = await _uow.Tracks.GetByIdAsync(request.TrackId);
            if (track == null)
                throw new Exception("Track not found.");
            // Kiểm tra user có phải leader
            if (team.TeamLeaderId != userIdFromToken)
                throw new Exception("Only team leader can select track.");

            // Kiểm tra team đã chọn track chưa
            var existingSelection = (await _uow.TeamTrackSelections.GetAllAsync(
                t => t.TeamId == request.TeamId)).FirstOrDefault();

            if (existingSelection != null)
                throw new Exception("Team has already selected a track.");

            // Tạo selection mới
            var selection = new TeamTrackSelection
            {
                TeamId = request.TeamId,
                TrackId = request.TrackId,
                SelectedAt = DateTime.UtcNow
            };

            await _uow.TeamTrackSelections.AddAsync(selection);
            await _uow.SaveAsync();

            return _mapper.Map<TeamSelectTrackResponse>(selection);
        }

        public async Task<TeamTrackByPhaseResponseDto?> GetTeamTrackByPhaseAsync(
         int teamId,
         int phaseId)
        {
            // 1️⃣ Kiểm tra team tồn tại
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new Exception("Team not found");

            //// 2️⃣ Kiểm tra team có submission FINAL trong phase này không
            //var finalSubmission = await _uow.Submissions.FirstOrDefaultAsync(
            //    s => s.TeamId == teamId
            //         && s.PhaseId == phaseId
            //         && s.IsFinal);

            //if (finalSubmission == null)
            //    return null; 

            // 3️⃣ Lấy track mà team đã chọn
            var selection = (await _uow.TeamTrackSelections.GetAllIncludingAsync(
                t => t.TeamId == teamId,
                t => t.Team,
                t => t.Track
            )).FirstOrDefault();

            if (selection == null)
                return null;

            // ✅ AutoMapper
            return _mapper.Map<TeamTrackByPhaseResponseDto>(selection);
        }


    }
}