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

        // 🟢 1️⃣ Thành viên tạo submission (mặc định là draft)
        public async Task<SubmissionResponseDto> CreateDraftSubmissionAsync(SubmissionCreateDto dto, int userId)
        {
            // ✅ 1. Kiểm tra PhaseChallenge tồn tại
            var phaseChallenge = await _uow.PhaseChallenges.GetByIdAsync(dto.PhaseChallengeId);
            if (phaseChallenge == null)
                throw new Exception("Invalid PhaseChallengeId.");

            // ✅ 2. Kiểm tra team đã đăng ký phase chưa
            bool isRegistered = await _uow.TeamChallenges.ExistsAsync(tc =>
                tc.TeamId == dto.TeamId &&
                tc.PhaseId == phaseChallenge.PhaseId &&
                tc.Status == true);

            if (!isRegistered)
                throw new Exception("Team has not registered for this phase.");

            // ✅ 3. Kiểm tra user có thuộc team không
            var member = await _uow.TeamMembers.FirstOrDefaultAsync(m =>
                m.TeamId == dto.TeamId && m.UserId == userId);
            if (member == null)
                throw new UnauthorizedAccessException("You are not a member of this team.");

            // ✅ 4. Kiểm tra đã có draft chưa
            var existingDraft = await _uow.Submissions.FirstOrDefaultAsync(s =>
                s.TeamId == dto.TeamId &&
                s.PhaseChallengeId == dto.PhaseChallengeId &&
                s.IsFinal == false);
            if (existingDraft != null)
                throw new Exception("A draft submission already exists for this team in this phase.");

            // ✅ 5. Tạo submission mới
            var submission = _mapper.Map<Submission>(dto);
            submission.SubmittedBy = userId;
            submission.SubmittedAt = DateTime.UtcNow;
            submission.IsFinal = false;

            await _uow.Submissions.AddAsync(submission);
            await _uow.SaveAsync();

            // ✅ 6. Chuẩn bị dữ liệu trả về
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            var user = await _uow.Users.GetByIdAsync(userId);

            // PhaseName có thể null nếu không Include, nên check null-safe
            var phaseName = phaseChallenge.Phase?.PhaseName ?? "Unknown Phase";

            return new SubmissionResponseDto
            {
                SubmissionId = submission.SubmissionId,
                TeamName = team?.TeamName ?? "Unknown Team",
                PhaseName = phaseName,
                Title = submission.Title,
                GitHubLink = submission.GitHubLink,
                DemoLink = submission.DemoLink,
                ReportLink = submission.ReportLink,
                IsFinal = submission.IsFinal,
                SubmittedAt = submission.SubmittedAt,
                SubmittedByName = user?.FullName ?? "Unknown User"
            };
        }

        // 🟢 2️⃣ Team leader chọn bài final
        public async Task<SubmissionResponseDto> SetFinalSubmissionAsync(SubmissionSelectFinalDto dto, int userId)
        {
            // ✅ Kiểm tra team & leader
            var finalSubmission = await _uow.Submissions.FirstOrDefaultAsync(s => s.SubmissionId == dto.SubmissionId);
            if (finalSubmission == null)
                throw new Exception("Submission not found.");

            // ✅ Lấy team từ submission
            var team = await _uow.Teams.GetByIdAsync(finalSubmission.TeamId);
            if (team == null)
                throw new Exception("Team not found.");

            // ✅ Kiểm tra quyền (chỉ leader được phép)
            if (team.LeaderId != userId)
                throw new UnauthorizedAccessException("Only the team leader can set the final submission.");

            // ✅ Lấy tất cả submissions cùng team và cùng phase challenge
            var submissions = await _uow.Submissions.GetAllAsync(
                s => s.TeamId == finalSubmission.TeamId && s.PhaseChallengeId == finalSubmission.PhaseChallengeId
            );

            if (!submissions.Any())
                throw new Exception("No submissions found for this PhaseChallenge.");

            // ✅ Đặt final
            foreach (var sub in submissions)
            {
                sub.IsFinal = sub.SubmissionId == dto.SubmissionId;
                _uow.Submissions.Update(sub);
            }

            await _uow.SaveAsync();

            // ✅ Load phase & user để trả về thông tin chi tiết
            var phaseChallenge = (await _uow.PhaseChallenges.GetAllAsync(
                x => x.PhaseChallengeId == finalSubmission.PhaseChallengeId,
                includeProperties: "Phase"
            )).FirstOrDefault();

            var user = await _uow.Users.GetByIdAsync(finalSubmission.SubmittedBy);

            return new SubmissionResponseDto
            {
                SubmissionId = finalSubmission.SubmissionId,
                TeamName = team.TeamName,
                PhaseName = phaseChallenge?.Phase?.PhaseName ?? "Unknown Phase",
                Title = finalSubmission.Title,
                GitHubLink = finalSubmission.GitHubLink,
                DemoLink = finalSubmission.DemoLink,
                ReportLink = finalSubmission.ReportLink,
                IsFinal = finalSubmission.IsFinal,
                SubmittedAt = finalSubmission.SubmittedAt,
                SubmittedByName = user?.FullName ?? "Unknown"
            };
        }

        // 🟢 3️⃣ Lấy danh sách submission của team trong PhaseChallenge
        public async Task<IEnumerable<SubmissionResponseDto>> GetSubmissionsByTeamAndPhaseAsync(int? teamId, int? phaseChallengeId)
        {
            var submissions = await _uow.Submissions.GetAllIncludingAsync(
                s =>
                    (!teamId.HasValue || s.TeamId == teamId.Value) &&
                    (!phaseChallengeId.HasValue || s.PhaseChallengeId == phaseChallengeId.Value),
                s => s.Team,
                s => s.SubmittedByNavigation
            );

            return submissions.Select(s => new SubmissionResponseDto
            {
                SubmissionId = s.SubmissionId,
                TeamName = s.Team?.TeamName ?? "Unknown",
                Title = s.Title,
                GitHubLink = s.GitHubLink,
                DemoLink = s.DemoLink,
                ReportLink = s.ReportLink,
                IsFinal = s.IsFinal,
                SubmittedAt = s.SubmittedAt,
                SubmittedByName = s.SubmittedByNavigation?.FullName ?? "Unknown"
            });
        }

    }
}