using AutoMapper;
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

            // Dùng ExistsAsync thay cho IsEmailInvitedAsync
            var alreadyInvited = await _uow.TeamInvitations.ExistsAsync(i =>
                i.TeamId == teamId &&
                i.InvitedEmail.ToLower() == email.ToLower() &&
                i.Status == InvitationStatus.Pending);

            if (alreadyInvited)
                throw new Exception("This email has already been invited");

            var invitation = new TeamInvitation
            {
                TeamId = teamId,
                InvitedEmail = email,
                InvitedByUserId = inviterUserId,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _uow.TeamInvitations.AddAsync(invitation);
            await _uow.SaveAsync();

            var inviteLink = $"https://sealfall25.somee.com/api/TeamInvitation/accept-link?code={invitation.InvitationCode}";
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

        public async Task<AcceptInvitationResult> AcceptInvitationAsync(Guid code, int userId)
        {
            var invitation = await _uow.TeamInvitations.FirstOrDefaultAsync(i => i.InvitationCode == code);
            if (invitation == null || invitation.Status != "Pending" || invitation.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invitation is invalid or expired.");

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("This invitation is not for your account");

            // Đánh dấu accepted
            invitation.Status = "Accepted";
            _uow.TeamInvitations.Update(invitation);
            await _uow.SaveAsync();

            // Lấy các lời mời đã accept
            var acceptedInvitations = await _uow.TeamInvitations.GetAllAsync(i => i.TeamId == invitation.TeamId && i.Status == "Accepted");

            var team = await _uow.Teams.GetByIdAsync(invitation.TeamId);
            if (team == null) throw new Exception("Team not found");

            int memberCount = acceptedInvitations.Count() + 1; // +1 leader
            bool teamCreated = false;

            if (memberCount >= 3)
            {
                // Thêm leader
                if (team.LeaderId.HasValue &&
                    !await _uow.TeamMembers.ExistsAsync(m => m.TeamId == team.TeamId && m.UserId == team.LeaderId.Value))
                {
                    await _uow.TeamMembers.AddAsync(new TeamMember
                    {
                        TeamId = team.TeamId,
                        UserId = team.LeaderId.Value,
                        RoleInTeam = "Leader"
                    });
                }

                // Thêm các member
                foreach (var inv in acceptedInvitations)
                {
                    var invitedUser = await _uow.Users.FirstOrDefaultAsync(u => u.Email == inv.InvitedEmail);
                    if (invitedUser == null) continue;

                    if (!await _uow.TeamMembers.ExistsAsync(m => m.TeamId == team.TeamId && m.UserId == invitedUser.UserId))
                    {
                        await _uow.TeamMembers.AddAsync(new TeamMember
                        {
                            TeamId = team.TeamId,
                            UserId = invitedUser.UserId,
                            RoleInTeam = "Member"
                        });
                    }
                }

                await _uow.SaveAsync();
                teamCreated = true;
            }

            return new AcceptInvitationResult
            {
                Message = teamCreated
            ? "Invitation accepted successfully, team members created."
            : "Invitation accepted successfully, waiting for more members.",
                TeamCreated = teamCreated
            };
        }





        public async Task<string> RejectInvitationAsync(Guid invitationCode, int userId)
        {
            var invitation = await _uow.TeamInvitationRepository.GetByCodeAsync(invitationCode);
            if (invitation == null)
                throw new Exception("Invitation not found.");

            if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invitation is invalid or expired.");

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null || !string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("This invitation is not for your account");

            invitation.Status = InvitationStatus.Rejected;
            _uow.TeamInvitations.Update(invitation);
            await _uow.SaveAsync();

            return "You have rejected the invitation.";
        }

        public async Task<InvitationStatusDto> GetInvitationStatusAsync(Guid invitationCode)
        {
            var invitation = await _uow.TeamInvitationRepository.GetByCodeAsync(invitationCode);
            if (invitation == null)
                throw new Exception("Invitation not found");

            
            var dto = _mapper.Map<InvitationStatusDto>(invitation);

            return dto;
        }

        public async Task<string> ConfirmTeamAsync(int teamId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId);
            if (team == null) throw new Exception("Team not found");

            // get leader
            var leader = await _uow.Users.GetByIdAsync(team.LeaderId);

            // get list invitation accepted
            var acceptedInvitations = await _uow.TeamInvitationRepository
                .GetAllAsync(i => i.TeamId == teamId && i.Status == InvitationStatus.Accepted);

            var memberCount = acceptedInvitations.Count() + 1; 

            if (memberCount < 3)
                throw new Exception("Not enough members to form a team. Minimum is 3.");

            if (memberCount > 5)
                throw new Exception("Too many accepted members. Maximum is 5.");

            // add leader
            if (!await _uow.TeamMembers.ExistsAsync(m => m.TeamId == teamId && m.UserId == leader.UserId))
            {
                await _uow.TeamMembers.AddAsync(new TeamMember
                {
                    TeamId = teamId,
                    UserId = leader.UserId,
                    RoleInTeam = "Leader"
                });
            }

            // add accepted members
            foreach (var inv in acceptedInvitations)
            {
                var user = await _uow.Users.FirstOrDefaultAsync(u => u.Email == inv.InvitedEmail);
                if (user == null) continue;

                if (!await _uow.TeamMembers.ExistsAsync(m => m.TeamId == teamId && m.UserId == user.UserId))
                {
                    await _uow.TeamMembers.AddAsync(new TeamMember
                    {
                        TeamId = teamId,
                        UserId = user.UserId,
                        RoleInTeam = "Member"
                    });
                }
            }


            await _uow.SaveAsync();

            return "Team has been officially created with " + memberCount + " members.";
        }


    }
}
