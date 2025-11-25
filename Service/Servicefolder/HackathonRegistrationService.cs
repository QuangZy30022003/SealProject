    using AutoMapper;
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

            public HackathonRegistrationService(IUOW uow, IMapper mapper)
            {
                _uow = uow;
                _mapper = mapper;
            }

            public async Task<string> RegisterTeamAsync(int userId, int hackathonId, string link)
            {
                // 1️ Lấy team của user (leader hoặc member)
                var userTeams = await _uow.Teams.GetAllIncludingAsync(
                    t => (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)),
                    t => t.TeamMembers
                );

                var team = userTeams.FirstOrDefault();
                if (team == null)
                    return "User does not have a team.";

                // 2️ Chỉ Team Leader được phép đăng ký
                if (team.TeamLeaderId != userId)
                    return "Only the Team Leader can register the team for the hackathon.";

                // 3️ Kiểm tra hackathon tồn tại
                var hackathonExists = await _uow.Hackathons.ExistsAsync(h => h.HackathonId == hackathonId);
                if (!hackathonExists)
                    return "Hackathon not found.";

                // 4️ Kiểm tra số lượng thành viên (bao gồm leader)
                int memberCount = (team.TeamMembers?.Count ?? 0);
                if (memberCount < 3)
                    return "Team must have at least 3 members (including leader) before registering.";
                if (memberCount > 5)
                    return "Team cannot have more than 5 members when registering.";

                // 5️ Kiểm tra đã đăng ký chưa
                bool alreadyRegistered = await _uow.HackathonRegistrations.ExistsAsync(
                    r => r.HackathonId == hackathonId && r.TeamId == team.TeamId
                );
                if (alreadyRegistered)
                    return "Team has already registered for this hackathon.";

                // 6️ Tạo bản ghi đăng ký
                var registration = new HackathonRegistration
                {
                    HackathonId = hackathonId,
                    TeamId = team.TeamId,
                    Link = link ?? "",
                    RegisteredAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                await _uow.HackathonRegistrations.AddAsync(registration);

                // 7 Cập nhật lại HackathonId của team (vì nullable)
                team.HackathonId = hackathonId;
                _uow.Teams.Update(team);

                await _uow.SaveAsync();

                return "Team successfully registered for the hackathon.";
            }

            public async Task<string> CancelRegistrationAsync(int userId, int hackathonId, string reason)
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

                // ✅ Xóa hackathonId của team (nếu muốn hủy hoàn toàn mối liên kết)
                team.HackathonId = null;
                _uow.Teams.Update(team);

                await _uow.SaveAsync();

                return "Team registration has been cancelled.";
            }

            public async Task<string> RestoreRegistrationAsync(int userId, int hackathonId)
            {
                // 1️ Tìm đăng ký liên quan đến user
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

                // Đảm bảo đồng bộ lại hackathonId (nếu đã bị null)
                if (team.HackathonId != hackathonId)
                {
                    team.HackathonId = hackathonId;
                    _uow.Teams.Update(team);
                }

                await _uow.SaveAsync();

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
                registration.Status = "Cancelled";
                registration.CancelledAt = DateTime.UtcNow;
                registration.CancelReason = cancelReason ?? "Rejected by chapter leader";

                // (Tuỳ bạn: có thể null HackathonId để giải phóng team)
                team.HackathonId = null;
                _uow.Teams.Update(team);

                await _uow.SaveAsync();

                return "Team registration has been rejected.";
            }

            public async Task<List<HackathonRegistrationDto>> GetRegistrationsByHackathonAsync(int hackathonId)
            {
                var registrations = await _uow.HackathonRegistrations
                    .GetAllIncludingAsync(r => r.HackathonId == hackathonId, r => r.Team);

                // Map sang DTO
                var dtoList = _mapper.Map<List<HackathonRegistrationDto>>(registrations);

                return dtoList;
            }

        }
    }