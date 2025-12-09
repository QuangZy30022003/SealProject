using AutoMapper;
using Common.DTOs.JudgeAssignmentDto;
using Common.DTOs.NotificationDto;
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
    public class JudgeAssignmentService : IJudgeAssignmentService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public JudgeAssignmentService(IUOW uow, IMapper mapper, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        // ✅ Admin tạo JudgeAssignment
        public async Task<JudgeAssignmentResponseDto> AssignJudgeAsync(JudgeAssignmentCreateDto dto, int adminId)
        {
            // 1️⃣ Validate tồn tại Hackathon
            var hackathon = await _uow.Hackathons.GetByIdAsync(dto.HackathonId);
            if (hackathon == null)
                throw new Exception("Hackathon not found");

            // 2️⃣ Validate Judge có tồn tại và có role là Judge
            var judge = await _uow.Users.GetByIdAsync(dto.JudgeId);
            if (judge == null)
                throw new Exception("Judge not found");

            if (judge.RoleId != 6) // ví dụ role 4 là Judge
                throw new Exception("User is not a judge");

            // 3️⃣ Validate Track, Phase (nếu có)
            if (dto.TrackId.HasValue)
            {
                var track = await _uow.Tracks.GetByIdAsync(dto.TrackId.Value);
                if (track == null)
                    throw new Exception("Track not found");
            }

            if (dto.PhaseId.HasValue)
            {
                var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId.Value);
                if (phase == null)
                    throw new Exception("Phase not found");
            }

            bool exists = await _uow.JudgeAssignments.ExistsAsync(
        x => x.JudgeId == dto.JudgeId &&
             x.HackathonId == dto.HackathonId &&
             x.TrackId == dto.TrackId &&
             x.PhaseId == dto.PhaseId
    );

            if (exists)
                throw new Exception("This judge is already assigned to this track in the same phase.");

            // 5️⃣ Tạo mới
            var assignment = new JudgeAssignment
            {
                JudgeId = dto.JudgeId,
                HackathonId = dto.HackathonId,
                TrackId = dto.TrackId,
                PhaseId = dto.PhaseId,
                AssignedAt = DateTime.UtcNow,
                Status = "Active"
            };

            await _uow.JudgeAssignments.AddAsync(assignment);
            await _uow.SaveAsync();

            // ✅ GỬI NOTIFICATION CHO JUDGE
            var trackName = await _uow.Tracks.GetByIdAsync(dto.TrackId.Value);
            var phaseName = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId.Value);
            var trackInfo = dto.TrackId.HasValue ? $" - Track: {trackName.Name}" : "";
            var phaseInfo = dto.PhaseId.HasValue ? $" - Phase: {phaseName.PhaseName}" : "";

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = dto.JudgeId,
                Message = $"You have been assigned as a judge for {hackathon.Name}{trackInfo}{phaseInfo}"
            });

            return _mapper.Map<JudgeAssignmentResponseDto>(assignment);
        }

        // ✅ Lấy danh sách judges được gán cho hackathon
        public async Task<List<JudgeAssignmentResponseDto>> GetByHackathonAsync(int hackathonId)
        {
            var assignments = await _uow.JudgeAssignments
                .GetAllIncludingAsync(x => x.HackathonId == hackathonId,
                                      x => x.Judge, x => x.Track, x => x.Phase, x => x.Hackathon);
            return _mapper.Map<List<JudgeAssignmentResponseDto>>(assignments);
        }

        // ✅ Xóa / hủy gán judge
        public async Task<bool> RemoveAssignmentAsync(int assignmentId)
        {
            var assignment = await _uow.JudgeAssignments.GetByIdAsync(assignmentId);
            if (assignment == null)
                throw new Exception("Assignment not found");

            if (assignment.Status?.ToLower() == "blocked")
                throw new Exception("This assignment is already blocked");

            assignment.Status = "Blocked"; // hoặc "Inactive" tùy theo convention của bạn
            _uow.JudgeAssignments.Update(assignment);
            await _uow.SaveAsync();

            var hackathon = await _uow.Hackathons.GetByIdAsync(assignment.HackathonId);

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = assignment.JudgeId,
                Message = $"Your judge assignment for {hackathon.Name} has been removed."
            });

            return true;
        }

        public async Task<bool> ReactivateAssignmentAsync(int assignmentId)
        {
            var assignment = await _uow.JudgeAssignments.GetByIdAsync(assignmentId);
            if (assignment == null)
                throw new Exception("Assignment not found");

            // Nếu đã active rồi thì không cần đổi
            if (assignment.Status == "Active")
                throw new Exception("Assignment is already active");

            assignment.Status = "Active";
            _uow.JudgeAssignments.Update(assignment);
            await _uow.SaveAsync();

            return true;
        }
        public async Task<List<HackathonAssignedDto>> GetAssignedHackathonsAsync(int judgeId)
        {
            var assignments = await _uow.JudgeAssignments.GetAllIncludingAsync(
                x => x.JudgeId == judgeId && x.Status == "Active",
                x => x.Hackathon,
                x => x.Track,
                x => x.Phase
            );

            return _mapper.Map<List<HackathonAssignedDto>>(assignments);
        }

    }
}