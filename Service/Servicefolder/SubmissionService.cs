using AutoMapper;
using Common.DTOs.Submission;
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
    public class SubmissionService : ISubmissionService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public SubmissionService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // Tạo draft submission (mọi thành viên)
        public async Task<SubmissionResponseDto> CreateDraftAsync(SubmissionCreateDto dto, int currentUserId)
        {
            // Check team tồn tại
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found");

            // Check phase tồn tại
            var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId);
            if (phase == null)
                throw new Exception("Phase not found");

            var submission = new Submission
            {
                TeamId = dto.TeamId,
                PhaseId = dto.PhaseId,
                Title = dto.Title,
                FilePath = dto.FilePath,
                SubmittedAt = DateTime.UtcNow,
                SubmittedBy = currentUserId,
                IsFinal = false
            };

            await _uow.Submissions.AddAsync(submission);
            await _uow.SaveAsync();

            return _mapper.Map<SubmissionResponseDto>(submission);
        }

        // Update draft (chỉ người submit mới edit)
        public async Task<SubmissionResponseDto?> UpdateDraftAsync(int submissionId, SubmissionUpdateDto dto, int currentUserId)
        {
            var submission = await _uow.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                throw new Exception("Submission not found");

            if (submission.IsFinal)
                throw new Exception("Cannot edit final submission");

            if (submission.SubmittedBy != currentUserId)
                throw new Exception("Not authorized to edit this draft");

            submission.Title = dto.Title;
            submission.FilePath = dto.FilePath;
            await _uow.SaveAsync();

            return _mapper.Map<SubmissionResponseDto>(submission);
        }

        // Set submission final (chỉ team leader)
        public async Task<SubmissionResponseDto?> SetFinalAsync(SubmissionFinalDto dto, int currentUserId)
        {
            var submission = await _uow.Submissions.GetByIdAsync(dto.SubmissionId);
            if (submission == null)
                throw new Exception("Submission not found");

            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found");

            if (team.TeamLeaderId != currentUserId)
                throw new Exception("Not authorized to set final submission");

            submission.IsFinal = true;
            await _uow.SaveAsync();

            return _mapper.Map<SubmissionResponseDto>(submission);
        }

        public async Task<List<SubmissionResponseDto>> GetSubmissionsByTeamAsync(int teamId)
        {
            // Kiểm tra team có tồn tại
            var teamExists = await _uow.Teams.ExistsAsync(t => t.TeamId == teamId);
            if (!teamExists)
                throw new Exception("Team not found");

            // Lấy submission của team, include Team và TeamTrackSelections
            var teamSubmissions = await _uow.Submissions.GetAllIncludingAsync(
                s => s.TeamId == teamId,
                s => s.Team,
                s => s.Team.TeamTrackSelections
            );

            // AutoMapper sẽ map TrackId từ Team.TeamTrackSelections.First()
            return _mapper.Map<List<SubmissionResponseDto>>(teamSubmissions);
        }



    }
}