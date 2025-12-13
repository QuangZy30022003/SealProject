using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.TeamInvitationDto;
using Common.Enums;
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
    public class TeamInvitationService : ITeamInvitationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        public TeamInvitationService(IUOW uow, IMapper mapper, IEmailService emailService, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _notificationService = notificationService;
        }
        public async Task<string> InviteMemberAsync(int teamId, string email, int inviterUserId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new Exception("Team not found.");

            if (team.TeamLeaderId != inviterUserId)
                throw new UnauthorizedAccessException("Only team leader can invite members.");

            // Check member count (max 5)
            var memberCount = await _uow.TeamMembers.CountAsync(m => m.TeamId == teamId);
            if (memberCount >= 5)
                throw new Exception("Team already has maximum number of members (5).");

            // Check nếu user đã ở team khác
            var invitedUser = await _uow.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (invitedUser != null)
            {
                if (team.HackathonId == null)
                {
                    // 🧩 Trường hợp team chưa có hackathon — user chỉ được ở 1 team "chưa đăng ký hackathon"
                    bool alreadyInUnregisteredTeam = await _uow.Teams.ExistsAsync(t =>
                        t.HackathonId == null &&
                        (t.TeamLeaderId == invitedUser.UserId || t.TeamMembers.Any(tm => tm.UserId == invitedUser.UserId)) &&
                        t.TeamId != team.TeamId);

                    if (alreadyInUnregisteredTeam)
                        throw new InvalidOperationException("User is already in another team that hasn't registered for any hackathon.");
                }
                else
                {
                    // 🧩 Trường hợp team đã thuộc hackathon — check cùng hackathon
                    bool alreadyInTeamSameHackathon = await _uow.Teams.ExistsAsync(t =>
                        t.HackathonId == team.HackathonId &&
                        (t.TeamLeaderId == invitedUser.UserId || t.TeamMembers.Any(tm => tm.UserId == invitedUser.UserId)) &&
                        t.TeamId != team.TeamId);

                    if (alreadyInTeamSameHackathon)
                        throw new InvalidOperationException("User is already in another team in this hackathon.");
                }
            }

            // Check đã có lời mời pending chưa
            var alreadyInvited = await _uow.TeamInvitations.ExistsAsync(i =>
                i.TeamId == teamId &&
                i.InvitedEmail.ToLower() == email.ToLower() &&
                i.Status == InvitationStatus.Pending);

            if (alreadyInvited)
                throw new Exception("This email has already been invited.");

            var invitation = new TeamInvitation
            {
                InvitationId = Guid.NewGuid(),
                InvitationCode = Guid.NewGuid(),
                TeamId = teamId,
                InvitedEmail = email,
                InvitedByUserId = inviterUserId,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _uow.TeamInvitations.AddAsync(invitation);
            await _uow.SaveAsync();

            // ✅ GỬI NOTIFICATION
            if (invitedUser != null)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = invitedUser.UserId,
                    Message = $"You have been invited to join team {team.TeamName}"
                });
            }


            var inviteLink = $"https://sealfall25.somee.com/api/TeamInvitation/accept-link?code={invitation.InvitationCode}";
            var subject = $"Lời mời tham gia nhóm: {team.TeamName}";
            var body = $@"
                <p>Xin chào,</p>
                <p>Bạn đã được mời tham gia nhóm <strong>{team.TeamName}</strong> trên hệ thống SEAL.</p>
                <p><a href='{inviteLink}' style='padding:10px 15px;background:#28a745;color:white;text-decoration:none;'>Chấp nhận lời mời</a></p>
                <p>Hoặc dán link này vào trình duyệt: <a href='{inviteLink}'>{inviteLink}</a></p>
                <p><i>Lưu ý: Lời mời hết hạn sau 7 ngày.</i></p>";

            await _emailService.SendEmailAsync(email, subject, body);
            return inviteLink;
        }

        public async Task<InvitationResult> AcceptInvitationAsync(Guid code, int userId)
        {
            // 1️ Kiểm tra invitation hợp lệ
            var invitation = await _uow.TeamInvitations.FirstOrDefaultAsync(i => i.InvitationCode == code);
            if (invitation == null || invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt < DateTime.UtcNow)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "Invitation is invalid or expired."
                };

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "User not found."
                };

            if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "This invitation is not for your account."
                };

            // 2️ Lấy thông tin team & hackathon
            var team = await _uow.Teams.GetByIdAsync(invitation.TeamId);
            if (team == null)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "Team not found."
                };

            var hackathonId = team.HackathonId;

            // 3️ Kiểm tra nếu team đã full (trước khi accept)
            var memberCount = await _uow.TeamMembers.CountAsync(m => m.TeamId == team.TeamId);
            if (memberCount >= 5)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "Team already has maximum number of members (5).",
                    TeamId = team.TeamId,
                    TeamName = team.TeamName
                };


            // 4️ Kiểm tra xem user đã ở team khác (cùng hackathon hoặc chưa có hackathon)
            if (hackathonId == null)
            {
                // 🔸 Nếu team chưa có hackathon → user chỉ được ở 1 team "chưa đăng ký hackathon"
                bool alreadyInUnregisteredTeam = await _uow.Teams.ExistsAsync(t =>
                    t.HackathonId == null &&
                    (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)) &&
                    t.TeamId != team.TeamId);

                if (alreadyInUnregisteredTeam)
                    return new InvitationResult
                    {
                        Status = "Failed",
                        Message = "You are already in another team that hasn't registered for any hackathon."
                    };
            }
            else
            {
                // 🔸 Nếu team có hackathon → user không thể ở team khác cùng hackathon
                bool alreadyInTeamSameHackathon = await _uow.Teams.ExistsAsync(t =>
                    t.HackathonId == hackathonId &&
                    (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)) &&
                    t.TeamId != team.TeamId);

                if (alreadyInTeamSameHackathon)
                    return new InvitationResult
                    {
                        Status = "Failed",
                        Message = "You are already a member of another team in this hackathon."
                    };
            }

            // 5️ Cập nhật invitation
            invitation.Status = InvitationStatus.Accepted;
            _uow.TeamInvitations.Update(invitation);

            // 6️ Thêm user vào team
            await _uow.TeamMembers.AddAsync(new TeamMember
            {
                TeamId = team.TeamId,
                UserId = userId,
                RoleInTeam = "Member"
            });
            await _uow.SaveAsync();

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = $"{user.FullName} has joined your team {team.TeamName}."
            });


            // 7️ Kết quả trả về
            return new InvitationResult
            {
                Status = "Success",
                Message = "You have successfully joined the team.",
                TeamId = team.TeamId,
                TeamName = team.TeamName,
            };
        }

        public async Task<InvitationResult> RejectInvitationAsync(Guid invitationCode, int userId)
        {
            // 1️ Kiểm tra invitation hợp lệ
            var invitation = await _uow.TeamInvitations.FirstOrDefaultAsync(i => i.InvitationCode == invitationCode);
            if (invitation == null || invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt < DateTime.UtcNow)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "Invitation is invalid or expired."
                };

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "User not found."
                };

            if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                return new InvitationResult
                {
                    Status = "Failed",
                    Message = "This invitation is not for your account."
                };

            // 2️ Cập nhật trạng thái từ chối
            invitation.Status = InvitationStatus.Rejected;
            _uow.TeamInvitations.Update(invitation);
            await _uow.SaveAsync();

            var team = await _uow.Teams.GetByIdAsync(invitation.TeamId);

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = invitation.InvitedByUserId,
                Message = $"An invitation to join team {team.TeamName} was rejected."
            });


            return new InvitationResult
            {
                Status = "Success",
                Message = "You have rejected the invitation."
            };
        }

        public async Task<InvitationStatusDto> GetInvitationStatusAsync(Guid invitationCode)
        {
            var invitation = await _uow.TeamInvitations.FirstOrDefaultAsync(x => x.InvitationCode == invitationCode);
            if (invitation == null)
                throw new Exception("Invitation not found");


            var dto = _mapper.Map<InvitationStatusDto>(invitation);

            return dto;
        }

        public async Task<List<InvitationStatusDto>> GetTeamInvitationsByTeamIdAsync(int teamId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new Exception("Team not found.");

            var invitations = await _uow.TeamInvitations.GetAllAsync(i => i.TeamId == teamId);
            var dtos = _mapper.Map<List<InvitationStatusDto>>(invitations);

            return dtos;
        }
    }
}
