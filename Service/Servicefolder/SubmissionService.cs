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
        private readonly INotificationService _notificationService;

        public SubmissionService(IUOW uow, IMapper mapper, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
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
            var fullSubmission = (await _uow.Submissions.GetAllIncludingAsync(
        s => s.SubmissionId == submission.SubmissionId,
        s => s.Team,
        s => s.Team.TeamTrackSelections,
        s => s.Phase
    )).First();

            await LoadTracksAsync(fullSubmission);
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
            var fullSubmission = (await _uow.Submissions.GetAllIncludingAsync(
        s => s.SubmissionId == submission.SubmissionId,
        s => s.Team,
        s => s.Team.TeamTrackSelections,
        s => s.Phase
    )).First();

            await LoadTracksAsync(fullSubmission);
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

            var existingFinal = (await _uow.Submissions.GetAllAsync(s =>
       s.TeamId == dto.TeamId &&
       s.PhaseId == submission.PhaseId &&
       s.IsFinal == true))
       .FirstOrDefault();

            // 🔴 2. Nếu có final cũ → hạ xuống false
            if (existingFinal != null)
            {
                existingFinal.IsFinal = false;
                _uow.Submissions.Update(existingFinal);
            }

            // 🔴 3. Set submission mới là final
            submission.IsFinal = true;
            _uow.Submissions.Update(submission);

            await _uow.SaveAsync();
            var fullSubmission = (await _uow.Submissions.GetAllIncludingAsync(
        s => s.SubmissionId == submission.SubmissionId,
        s => s.Team,
        s => s.Team.TeamTrackSelections,
        s => s.Phase
    )).First();

            await LoadTracksAsync(fullSubmission);
            // ✅ GỬI NOTIFICATION CHO JUDGES
            var judges = await _uow.JudgeAssignments.GetAllAsync(
                j => j.PhaseId == submission.PhaseId);

            var judgeIds = judges.Select(j => j.JudgeId).Distinct().ToList();

            await _notificationService.CreateNotificationsAsync(
                judgeIds,
                $"New submission from {team.TeamName} is ready for scoring"
            );

            return _mapper.Map<SubmissionResponseDto>(submission);
        }

        public async Task<List<SubmissionResponseDto>> GetSubmissionsByTeamAsync(int teamId)
        {
            var teamExists = await _uow.Teams.ExistsAsync(t => t.TeamId == teamId);
            if (!teamExists)
                throw new Exception("Team not found");

            // Load Submission + Team + TeamTrackSelections + Phase
            var teamSubmissions = await _uow.Submissions.GetAllIncludingAsync(
                s => s.TeamId == teamId,
                s => s.Team,
                s => s.Team.TeamTrackSelections,
                s => s.Phase
            );

            // Lấy toàn bộ TrackId
            var trackIds = teamSubmissions
                .SelectMany(s => s.Team.TeamTrackSelections.Select(ts => ts.TrackId))
                .Distinct()
                .ToList();

            // Load track một lần
            var tracks = await _uow.Tracks.GetAllAsync(
                t => trackIds.Contains(t.TrackId)
            );

            // Gán Track vào đúng selection
            foreach (var submission in teamSubmissions)
            {
                foreach (var sel in submission.Team.TeamTrackSelections)
                {
                    sel.Track = tracks.FirstOrDefault(t => t.TrackId == sel.TrackId);
                }
            }

            return _mapper.Map<List<SubmissionResponseDto>>(teamSubmissions);
        }


        public async Task<List<SubmissionResponseDto>> GetFinalSubmissionsByPhaseAsync(int phaseId, int userId, string role)
        {
            IEnumerable<Submission> submissions;

            // ADMIN
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("SystemAdministrator", StringComparison.OrdinalIgnoreCase))
            {
                submissions = await _uow.Submissions.GetAllIncludingAsync(
                    s => s.IsFinal && s.PhaseId == phaseId,
                    s => s.Team,
                    s => s.Team.TeamTrackSelections,
                    s => s.Phase
                );
            }
            // JUDGE
            else if (role.Equals("Judge", StringComparison.OrdinalIgnoreCase))
            {
                var assigned = await _uow.JudgeAssignments.ExistsAsync(
                    j => j.JudgeId == userId && j.PhaseId == phaseId
                );

                if (!assigned)
                    throw new Exception("You are not assigned to this phase.");

                submissions = await _uow.Submissions.GetAllIncludingAsync(
                    s => s.IsFinal && s.PhaseId == phaseId,
                    s => s.Team,
                    s => s.Team.TeamTrackSelections,
                    s => s.Phase
                );
            }
            else
            {
                throw new Exception("You are not authorized to view submissions.");
            }

            // ---- BỔ SUNG: LOAD TRACK ĐỂ LẤY TRACK NAME ----
            var trackIds = submissions
                .SelectMany(s => s.Team.TeamTrackSelections.Select(ts => ts.TrackId))
                .Distinct()
                .ToList();

            var tracks = await _uow.Tracks.GetAllAsync(t => trackIds.Contains(t.TrackId));

            foreach (var submission in submissions)
            {
                foreach (var sel in submission.Team.TeamTrackSelections)
                {
                    sel.Track = tracks.FirstOrDefault(t => t.TrackId == sel.TrackId);
                }
            }

            // MAP DTO
            return _mapper.Map<List<SubmissionResponseDto>>(submissions);
        }

        public async Task<SubmissionResponseDto> GetSubmissionByIdAsync(int submissionId)
        {
            var submission = await _uow.Submissions.GetAllIncludingAsync(
                s => s.SubmissionId == submissionId,
                s => s.Team,
                s => s.Team.TeamTrackSelections,
                s => s.Phase
            );

            var sub = submission.FirstOrDefault();
            if (sub == null)
                throw new Exception("Submission not found");

            // Lấy TrackId
            var trackIds = sub.Team.TeamTrackSelections
                .Select(x => x.TrackId)
                .Distinct()
                .ToList();

            // Load track
            var tracks = await _uow.Tracks.GetAllAsync(t => trackIds.Contains(t.TrackId));

            foreach (var sel in sub.Team.TeamTrackSelections)
            {
                sel.Track = tracks.FirstOrDefault(t => t.TrackId == sel.TrackId);
            }

            return _mapper.Map<SubmissionResponseDto>(sub);
        }

        public async Task<List<SubmissionResponseDto>> GetAllSubmissionsAsync()
        {
            var submissions = await _uow.Submissions.GetAllIncludingAsync(
                s => true,
                s => s.Team,
                s => s.Team.TeamTrackSelections,
                s => s.Phase
            );

            if (submissions == null || !submissions.Any())
                return new List<SubmissionResponseDto>();

            // Lấy toàn bộ TrackId
            var trackIds = submissions
                .SelectMany(s => s.Team.TeamTrackSelections.Select(ts => ts.TrackId))
                .Distinct()
                .ToList();

            // Load track duy nhất 1 lần
            var tracks = await _uow.Tracks.GetAllAsync(t => trackIds.Contains(t.TrackId));

            foreach (var submission in submissions)
            {
                foreach (var sel in submission.Team.TeamTrackSelections)
                {
                    sel.Track = tracks.FirstOrDefault(t => t.TrackId == sel.TrackId);
                }
            }

            return _mapper.Map<List<SubmissionResponseDto>>(submissions);
        }


        private async Task LoadTracksAsync(Submission submission)
        {
            if (submission.Team?.TeamTrackSelections == null ||
                !submission.Team.TeamTrackSelections.Any())
                return;

            var trackIds = submission.Team.TeamTrackSelections
                .Select(t => t.TrackId)
                .Distinct()
                .ToList();

            var tracks = await _uow.Tracks.GetAllAsync(t => trackIds.Contains(t.TrackId));

            foreach (var sel in submission.Team.TeamTrackSelections)
            {
                sel.Track = tracks.FirstOrDefault(t => t.TrackId == sel.TrackId);
            }
        }

    }
}