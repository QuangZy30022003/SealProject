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

            // ✅ SEND NOTIFICATION
            if (invitedUser != null)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = invitedUser.UserId,
                    Message = $"You have been invited to join team {team.TeamName}"
                });
            }

            var inviteLink = $"https://seal-fpt.vercel.app/api/TeamInvitation/accept-link?code={invitation.InvitationCode}";
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
            // 1️ Invitation
            var invitation = await _uow.TeamInvitations
                .FirstOrDefaultAsync(i => i.InvitationCode == code);

            if (invitation == null)
            {
                return Fail("Invitation does not exist.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return Fail("This invitation has expired.");
            }

            switch (invitation.Status)
            {
                case InvitationStatus.Pending:
                    break;

                case InvitationStatus.Accepted:
                    return Fail("This invitation has already been accepted.");

                case InvitationStatus.Rejected:
                    return Fail("This invitation has been rejected.");

                default:
                    return Fail("This invitation is no longer valid.");
            }

            // 2️ User
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                return Fail("User not found.");

            if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                return Fail("This invitation is not for your account.");

            // 3️ Team
            var team = await _uow.Teams.GetByIdAsync(invitation.TeamId);
            if (team == null)
            {
                return Fail("Team not found.");
            }

            // 4️ Already member
            bool alreadyMember = await _uow.TeamMembers
                .ExistsAsync(m => m.TeamId == team.TeamId && m.UserId == userId);

            if (alreadyMember)
                return Fail("You are already a member of this team.");

            // 5️ Team size (lock by transaction)
            var memberCount = await _uow.TeamMembers
                .CountAsync(m => m.TeamId == team.TeamId);

            if (memberCount >= 5)
            {
                return Fail("Team already has maximum number of members (5).", team.TeamId, team.TeamName);
            }

            // 6️ Hackathon conflict
            var teamRegistration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == team.TeamId && r.Status != "Cancelled");

            if (teamRegistration != null)
            {
                bool conflict = await _uow.HackathonRegistrations.ExistsAsync(r =>
                    r.HackathonId == teamRegistration.HackathonId &&
                    r.Status != "Cancelled" &&
                    (
                        r.Team.TeamLeaderId == userId ||
                        r.Team.TeamMembers.Any(tm => tm.UserId == userId)
                    ));

                if (conflict)
                    return Fail("You are already participating in another team in this hackathon.");
            }

            // 7️ Accept invitation
            invitation.Status = InvitationStatus.Accepted;
            _uow.TeamInvitations.Update(invitation);

            // 8️ Add user to team
            await _uow.TeamMembers.AddAsync(new TeamMember
            {
                TeamId = team.TeamId,
                UserId = userId,
                RoleInTeam = "Member"
            });

            await _uow.SaveAsync();

            // 9️ Notify team leader
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = $"{user.FullName} has joined your team {team.TeamName}."
            });

            // 🔟 Result
            return Success(team.TeamId, team.TeamName);
        }

        public async Task<InvitationResult> RejectInvitationAsync(Guid invitationCode, int userId)
        {
            // 1️ Invitation
            var invitation = await _uow.TeamInvitations
                .FirstOrDefaultAsync(i => i.InvitationCode == invitationCode);

            if (invitation == null)
            {
                return Fail("Invitation does not exist.");
            }
                
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return Fail("Invitation has expired.");
            }

            if (invitation.Status == InvitationStatus.Accepted)
            {
                return Fail("Invitation has already been accepted.");
            }

            if (invitation.Status == InvitationStatus.Rejected)
            {
                return Fail("Invitation has already been rejected.");
            }
            
            if (invitation.Status != InvitationStatus.Pending)
            {
                return Fail("Invitation is no longer valid.");
            }

            // 2️ User
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return Fail("User not found.");
            }    
                
            if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("This invitation is not for your account.");
            }    

            // 3️ Reject invitation
            invitation.Status = InvitationStatus.Rejected;
            _uow.TeamInvitations.Update(invitation);
            await _uow.SaveAsync();

            // 4️ Notify leader
            var team = await _uow.Teams.GetByIdAsync(invitation.TeamId);
            if (team != null)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = team.TeamLeaderId,
                    Message = $"{user.FullName} has rejected the invitation to join team {team.TeamName}."
                });
            }

            // 5️ result
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

        private static InvitationResult Fail(string message, int? teamId = null, string? teamName = null)
        {
            return new InvitationResult
            {
                Status = "Failed",
                Message = message,
                TeamId = teamId,
                TeamName = teamName
            };
        }

        private static InvitationResult Success(int teamId, string teamName)
        {
            return new InvitationResult
            {
                Status = "Success",
                Message = "You have successfully joined the team.",
                TeamId = teamId,
                TeamName = teamName
            };
        }
    }
}
