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
            // Lấy team của user
            var userTeams = await _uow.Teams.GetAllIncludingAsync(
                t => t.HackathonId == hackathonId &&
                     (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)),
                t => t.TeamMembers
            );

            var team = userTeams.FirstOrDefault();
            if (team == null)
                return "User does not have a team for this hackathon.";

            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can register the team for the hackathon.";

            if (_uow.HackathonRegistrations == null)
                throw new Exception("_uow.HackathonRegistrations repository is not initialized");

            // Check đã đăng ký
            bool alreadyRegistered = await _uow.HackathonRegistrations.ExistsAsync(
                r => r.HackathonId == hackathonId && r.TeamId == team.TeamId
            );
            if (alreadyRegistered)
                return "Team has already registered for this hackathon.";

            // Tạo đăng ký
            var registration = new HackathonRegistration
            {
                HackathonId = hackathonId,
                TeamId = team.TeamId,
                Link = link ?? "",
                RegisteredAt = DateTime.UtcNow,
                Status = "Pending"
            };

            await _uow.HackathonRegistrations.AddAsync(registration);
            await _uow.SaveAsync();

            return "Team successfully registered for the hackathon.";
        }

        public async Task<string> CancelRegistrationAsync(int userId, int hackathonId, string reason)
        {
            // Lấy team của user
            var userTeam = await _uow.Teams
                .GetAllIncludingAsync(
                    t => t.HackathonId == hackathonId &&
                         (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)),
                    t => t.TeamMembers
                );

            var team = userTeam.FirstOrDefault();
            if (team == null)
                return "User does not have a team for this hackathon.";

            // Chỉ TeamLeader được hủy
            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can cancel the registration.";

            // Lấy đăng ký
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == team.TeamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            // Cập nhật trạng thái cancel
            registration.Status = "Cancelled";
            registration.CancelledAt = DateTime.UtcNow;
            registration.CancelReason = reason ?? "";

            await _uow.SaveAsync();

            return "Team registration has been cancelled.";
        }
        public async Task<string> RestoreRegistrationAsync(int userId, int hackathonId)
        {
            // Lấy team của user
            var userTeam = await _uow.Teams
                .GetAllIncludingAsync(
                    t => t.HackathonId == hackathonId &&
                         (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)),
                    t => t.TeamMembers
                );

            var team = userTeam.FirstOrDefault();
            if (team == null)
                return "User does not have a team for this hackathon.";

            // Chỉ TeamLeader được restore
            if (team.TeamLeaderId != userId)
                return "Only the Team Leader can restore the registration.";

            // Lấy đăng ký
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == team.TeamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            if (registration.Status != "Cancelled")
                return "Registration is not cancelled, cannot restore.";

            // Restore registration
            registration.Status = "Pending";
            registration.CancelledAt = null;
            registration.CancelReason = null;

            await _uow.SaveAsync();

            return "Team registration has been restored.";
        }

        public async Task<string> ApproveTeamAsync(int chapterId, int hackathonId, int teamId)
        {
            // Lấy đăng ký của team, bao gồm hackathon
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";
         //   var team = await _uow.Teams.FirstOrDefaultAsync(t => t.ChapterId == chapterId);
            // Lấy hackathon để check chapter
            var hackathon = await _uow.Hackathons.FirstOrDefaultAsync(h => h.HackathonId == hackathonId);
            if (hackathon == null)
                return "Hackathon not found.";

            //if (team.ChapterId != chapterId)
            //    return "You are not authorized to approve teams for this hackathon.";

            if (registration.Status != "Pending")
                return "Only pending registrations can be approved.";

            // Update status
            registration.Status = "Approved";

            await _uow.SaveAsync();

            return "Team registration approved successfully.";
        }
        public async Task<string> RejectTeamAsync(int chapterId, int hackathonId, int teamId, string cancelReason)
        {
            // Lấy đăng ký của team
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.HackathonId == hackathonId);

            if (registration == null)
                return "Team has not registered for this hackathon.";

            // Lấy team để check chapter
            var team = await _uow.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId);
            if (team == null)
                return "Team not found.";

            //  if (team.ChapterId != chapterId)
            //    return "You are not authorized to reject this team.";

            if (registration.Status == "Cancelled")
                return "Cannot reject a registration that is already cancelled.";


            // Update status và ghi log hủy
            registration.Status = "Cancelled";
            registration.CancelledAt = DateTime.UtcNow;
            registration.CancelReason = cancelReason ?? "";

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