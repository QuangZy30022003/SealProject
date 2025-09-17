using AutoMapper;
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
        public TeamInvitationService(IUOW uow, IMapper mapper, IEmailService emailService)
        {
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
        }
        public async Task<string> InviteMemberAsync(int teamId, string email, int inviterUserId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null)
                throw new Exception("Team not found");

            if (team.LeaderId != inviterUserId)
                throw new UnauthorizedAccessException("Only team leader can invite");

            var alreadyInvited = await _uow.TeamInvitationRepository.IsEmailInvitedAsync(teamId, email);
            if (alreadyInvited)
                throw new Exception("This email has already been invited");

            var invitation = new TeamInvitation
            {
                TeamId = teamId,
                InvitedEmail = email,
                InvitedByUserId = inviterUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _uow.TeamInvitations.AddAsync(invitation);
            await _uow.SaveAsync();

            // Gửi email / in link (demo)
            var inviteLink = $"https://localhost:7268/api/TeamInvitation/accept-link?code={invitation.InvitationCode}";
            var subject = $"Lời mời tham gia nhóm: {team.TeamName}";
            var body = $@"
        <p>Xin chào,</p>
        <p>Bạn đã được mời tham gia nhóm <strong>{team.TeamName}</strong> trên hệ thống SEAL.</p>
        <p>Nhấn vào nút bên dưới để chấp nhận lời mời:</p>
        <p><a href='{inviteLink}' style='padding: 10px 15px; background-color: #28a745; color: white; text-decoration: none;'>Chấp nhận lời mời</a></p>
        <p>Hoặc bạn có thể dán link này vào trình duyệt: <br /><a href='{inviteLink}'>{inviteLink}</a></p>
        <br />
        <p><i>Lưu ý: Lời mời sẽ hết hạn sau 7 ngày.</i></p>
    ";

            await _emailService.SendEmailAsync(email, subject, body);
            Console.WriteLine($"[Invitation Link]: {inviteLink}");

            return inviteLink;
        }

        public async Task<string> AcceptInvitationAsync(Guid invitationCode, int userId)
        {
            var invitation = await _uow.TeamInvitationRepository.GetByCodeAsync(invitationCode);
            if (invitation == null || invitation.IsAccepted || invitation.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invitation is invalid or expired.");

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || user.Email.ToLower() != invitation.InvitedEmail.ToLower())
                throw new UnauthorizedAccessException("This invitation is not for your account");

            // Get all members of the team
            var allMembers = await _uow.TeamMembers.GetAllAsync(m => m.TeamId == invitation.TeamId);
            var currentMemberCount = allMembers.Count();

            // Giới hạn số lượng max là 5
            if (currentMemberCount >= 5)
                throw new Exception("The team already has the maximum number of members (5).");

            // Nếu chưa tồn tại thì thêm vào team
            var alreadyInTeam = allMembers.Any(m => m.UserId == userId);
            if (!alreadyInTeam)
            {
                await _uow.TeamMembers.AddAsync(new TeamMember
                {
                    TeamId = invitation.TeamId,
                    UserId = userId,
                    RoleInTeam = "Member"
                });
            }

            invitation.IsAccepted = true;
            _uow.TeamInvitations.Update(invitation);
            await _uow.SaveAsync();

            return "You have joined the team.";
        }
    }
}
