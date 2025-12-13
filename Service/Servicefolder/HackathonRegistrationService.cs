using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.RegisterHackathonDto;
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
    public class HackathonRegistrationService : IHackathonRegistrationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        public HackathonRegistrationService(IUOW uow, IMapper mapper, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<string> RegisterTeamAsync(int userId, int hackathonId, int teamId, string link)
        {
            // 1️ Lấy team của user (leader hoặc member)
            var team = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == teamId,
                t => t.TeamMembers
            );

            if (team == null)
                return "Team not found.";

            // 2️ Chỉ Team Leader được phép đăng ký
            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can register the team for the hackathon.";

            // 3 Kiểm tra team đã có hackathonId chưa
            if (team.HackathonId != null)
                return "This team is already bound to a hackathon.";

            // 4 Kiểm tra hackathon tồn tại
            bool hackathonExists = await _uow.Hackathons.ExistsAsync(h => h.HackathonId == hackathonId);
            if (!hackathonExists)
                return "Hackathon not found.";

            // 5 Kiểm tra số lượng thành viên (bao gồm leader)
            int memberCount = (team.TeamMembers?.Count ?? 0);
            if (memberCount < 3)
                return "Team must have at least 3 members (including leader) before registering.";
            if (memberCount > 5)
                return "Team cannot have more than 5 members when registering.";

            // 🔒 1 USER – 1 HACKATHON 
            bool userAlreadyInHackathon = await _uow.Teams.ExistsAsync(t =>
                t.HackathonId == hackathonId &&
                (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId))
            );

            if (userAlreadyInHackathon)
                return "You are already participating in this hackathon.";

            // 6 Kiểm tra đã đăng ký chưa
            bool alreadyRegistered = await _uow.HackathonRegistrations.ExistsAsync(
                r => r.HackathonId == hackathonId && r.TeamId == team.TeamId
            );
            if (alreadyRegistered)
                return "Team has already registered for this hackathon.";

            // 7 Tạo bản ghi đăng ký
            var registration = new HackathonRegistration
            {
                HackathonId = hackathonId,
                TeamId = team.TeamId,
                Link = link ?? "",
                RegisteredAt = DateTime.UtcNow,
                Status = "Pending"
            };

            await _uow.HackathonRegistrations.AddAsync(registration);

            // 8 Cập nhật lại HackathonId của team (vì nullable)
            team.HackathonId = hackathonId;
            _uow.Teams.Update(team);

            await _uow.SaveAsync();

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = $"Your team {team.TeamName} has successfully registered for the hackathon."
            });

            return "Team successfully registered for the hackathon.";
        }

        public async Task<string> CancelRegistrationAsync(int userId, int hackathonId, string reason, int teamId)
        {
            // 1️ Tìm đăng ký hackathon mà user là leader hoặc thành viên
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r =>
                    r.HackathonId == hackathonId &&
                    (r.Team.TeamLeaderId == userId || r.Team.TeamMembers.Any(tm => tm.UserId == userId))
                );

            if (registration == null)
                return "Team has not registered for this hackathon.";

            var team = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == registration.TeamId,
                t => t.TeamMembers
            );

            if (team == null)
                return "Team not found.";

            // 2️ Chỉ Team Leader mới được hủy
            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can cancel the registration.";

            // 3️ Không thể hủy nếu đã approved
            if (registration.Status == "Approved")
                return "Approved registrations cannot be cancelled.";

            // 4️ Cập nhật trạng thái
            registration.Status = "Cancelled";
            registration.CancelledAt = DateTime.UtcNow;
            registration.CancelReason = reason ?? "";

            _uow.Teams.Update(team);

            await _uow.SaveAsync();

            var memberIds = team.TeamMembers
            .Select(tm => tm.UserId)
            .Append(team.TeamLeaderId)
            .Distinct();

            foreach (var memberId in memberIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = memberId,
                    Message = $"The registration of team {team.TeamName} has been cancelled by the team leader."
                });
            }


            return "Team registration has been cancelled.";
        }

        public async Task<string> RestoreRegistrationAsync(int userId, int hackathonId, int teamId)
        {
            // 1️ Tìm đăng ký liên quan đến team và hackathon
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            var team = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == registration.TeamId,
                t => t.TeamMembers
            );

            if (team == null)
                return "Team not found.";

            // 2️ Chỉ Team Leader được restore
            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can restore the registration.";

            // 3️ Chỉ restore nếu bị cancelled
            if (registration.Status != "Cancelled")
                return "Registration is not cancelled, cannot restore.";

            // 4️ Kiểm tra số lượng thành viên (3–5)
            int memberCount = (team.TeamMembers?.Count ?? 0);
            if (memberCount < 3)
                return "Team must have at least 3 members (including leader) to restore registration.";
            if (memberCount > 5)
                return "Team cannot have more than 5 members.";

            // ✅ Restore lại
            registration.Status = "Pending";
            registration.CancelledAt = null;
            registration.CancelReason = null;

            await _uow.SaveAsync();

            var memberIds = team.TeamMembers
            .Select(tm => tm.UserId)
            .Append(team.TeamLeaderId)
            .Distinct();

            foreach (var memberId in memberIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = memberId,
                    Message = $"The registration of team {team.TeamName} has been restored and is pending approval."
                });
            }


            return "Team registration has been restored.";
        }

        public async Task<string> ApproveTeamAsync(int chapterId, int hackathonId, int teamId)
        {
            // 1️ Lấy đăng ký của team
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            var team = await _uow.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return "Team not found.";

            // 2️ Check chapter
            var chapter = await _uow.Chapters.FirstOrDefaultAsync(c => c.ChapterId == team.ChapterId);

            if (chapter == null)
                return "Chapter not found.";

            if (chapter.ChapterLeaderId != chapterId)
                return "You are not authorized to approve teams outside your chapter.";

            // 3️ Chỉ cho phép duyệt nếu đang Pending
            if (registration.Status != "Pending")
                return "Only pending registrations can be approved.";

            // ✅ Cập nhật trạng thái
            registration.Status = "WaitingMentor";

            await _uow.SaveAsync();

            // ✅ GỬI NOTIFICATION CHO TEAM LEADER
            var hackathon = await _uow.Hackathons.GetByIdAsync(hackathonId);

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = $"Your team {team.TeamName} has been approved for {hackathon.Name}. Please request a mentor."
            });

            return "Team registration approved successfully.";
        }

        public async Task<string> RejectTeamAsync(int chapterId, int hackathonId, int teamId, string cancelReason)
        {
            // 1️ Lấy đăng ký của team
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            var team = await _uow.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return "Team not found.";

            // 2️ Check chapter
            var chapter = await _uow.Chapters.FirstOrDefaultAsync(c => c.ChapterId == team.ChapterId);

            if (chapter == null)
                return "Chapter not found.";

            if (chapter.ChapterLeaderId != chapterId)
                return "You are not authorized to approve teams outside your chapter.";

            // 3️ Chỉ reject nếu chưa bị cancel
            if (registration.Status == "Cancelled")
                return "Registration is already cancelled.";

            // ✅ Cập nhật trạng thái
            registration.Status = "Rejected";
            registration.CancelledAt = DateTime.UtcNow;
            registration.CancelReason = cancelReason ?? "Rejected by chapter leader";

            _uow.Teams.Update(team);

            await _uow.SaveAsync();

            // ✅ GỬI NOTIFICATION CHO TEAM LEADER
            var hackathon = await _uow.Hackathons.GetByIdAsync(hackathonId);

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = $"Your team {team.TeamName} registration for {hackathon.Name} has been rejected. Reason: {cancelReason}"
            });


            return "Team registration has been rejected.";
        }

        public async Task<List<HackathonRegistrationDto>> GetRegistrationsByHackathonAsync(int hackathonId)
        {
            var registrations = await _uow.HackathonRegistrations
                .GetAllIncludingAsync(r => r.HackathonId == hackathonId, r => r.Team, r => r.Hackathon);

            // Map sang DTO
            var dtoList = _mapper.Map<List<HackathonRegistrationDto>>(registrations);

            return dtoList;
        }
        public async Task<object> GetPendingRegistrationsAsync(int hackathonId)
        {
            // 1️⃣ Kiểm tra hackathon tồn tại
            var hackathonExists = await _uow.Hackathons.ExistsAsync(h => h.HackathonId == hackathonId);
            if (!hackathonExists)
                return "Hackathon not found.";

            // 2️⃣ Lấy danh sách registration có status = Pending
            var registrations = await _uow.HackathonRegistrations
                .GetAllIncludingAsync(
                    r => r.HackathonId == hackathonId && r.Status == "Pending",
                    r => r.Team,
                    r => r.Hackathon
                );

            if (registrations == null)
                return "No pending registrations found for this hackathon.";

            // 3️⃣ Map sang DTO
            var dtoList = _mapper.Map<List<HackathonRegistrationDto>>(registrations);

            return dtoList;
        }

        public async Task<List<HackathonRegistrationDto>> GetMyRegistrationsAsync(int userId)
        {
            // Lấy tất cả registration mà user là leader hoặc member
            var registrations = await _uow.HackathonRegistrations.GetAllIncludingAsync(
                r => r.Team.TeamLeaderId == userId || r.Team.TeamMembers.Any(tm => tm.UserId == userId),
                r => r.Team,
                r => r.Hackathon
            );

            // Map với AutoMapper
            var dtoList = _mapper.Map<List<HackathonRegistrationDto>>(registrations);

            return dtoList;
        }

    }
}