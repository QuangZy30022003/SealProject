using AutoMapper;
using Common.DTOs.AssignedTeamDto;
using Common.DTOs.NotificationDto;
using Microsoft.Extensions.Configuration;
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
    public class MentorAssignmentService : IMentorAssignmentService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        public MentorAssignmentService(IUOW uow, IMapper mapper, IEmailService emailService, IConfiguration configuration, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<AssignedTeamDto>> ViewAssignedTeamsAsync(int mentorId)
        {
            var assignments = await _uow.MentorAssignmentRepository.GetTeamsByMentorIdAsync(mentorId);
            return assignments.Select(a => new AssignedTeamDto
            {
                AssignmentId = a.AssignmentId,
                TeamId = a.Team?.TeamId ?? 0,
                TeamName = a.Team?.TeamName ?? string.Empty,
                AssignedAt = a.AssignedAt,
                LeaderId = a.Team?.TeamLeaderId,
                LeaderName = a.Team?.TeamLeader?.FullName,
                Status = a.Status
            });
        }

        public async Task<MentorAssignmentResponseDto> RegisterAsync(int userId, MentorAssignmentCreateDto dto)
        {
            // 0. Check leader
            var member = await _uow.TeamMembers.FirstOrDefaultAsync(
                tm => tm.TeamId == dto.TeamId && tm.UserId == userId
            );

            if (member == null)
                throw new Exception("You are not a member of this team!");

            if (!string.Equals(member.RoleInTeam, "TeamLeader", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(member.RoleInTeam, "Leader", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Only TeamLeader or Leader can register a mentor!");

            // 1. Validate Mentor
            var mentor = await _uow.Users.GetByIdAsync(dto.MentorId);
            if (mentor == null || mentor.RoleId != 5)
                throw new Exception("Invalid mentor!");

            if (string.IsNullOrEmpty(mentor.Email))
                throw new Exception("Mentor email not found!");

            var verification = await _uow.MentorVerifications.FirstOrDefaultAsync(
          mv => mv.UserId == dto.MentorId &&
          mv.HackathonId == dto.HackathonId &&
          mv.Status == "Approved"
);

            if (verification == null)
                throw new Exception("This mentor is not verified for this hackathon!");

            // 2. Validate Team
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found!");

            // 3. Validate Hackathon
            var hackathon = await _uow.Hackathons.GetByIdAsync(dto.HackathonId);
            if (hackathon == null)
                throw new Exception("Hackathon not found!");

            // 🔥 SINGLE registration variable
            var registration = await _uow.HackathonRegistrations.FirstOrDefaultAsync(
                r => r.TeamId == dto.TeamId &&
                     r.HackathonId == dto.HackathonId
            );

            if (registration == null)
                throw new Exception("Team has not registered for this hackathon!");

            //if (!string.Equals(registration.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            //    throw new Exception("Team must be approved in this hackathon before requesting a mentor.");

            // 🔥 Check WaitingMentor
            if (!string.Equals(registration.Status, "WaitingMentor", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Team cannot register mentor because the registration status is not 'WaitingMentor'.");

            // 4. Validate previous assignments
            var existing = await _uow.MentorAssignments.GetAllAsync(
                filter: a => a.TeamId == dto.TeamId && a.HackathonId == dto.HackathonId && a.MentorId == dto.MentorId
            );

            var latest = existing.OrderByDescending(a => a.AssignmentId).FirstOrDefault();

            if (latest != null)
            {
                if (latest.Status == "Pending")
                {
                    if (latest.AssignedAt.AddDays(3) < DateTime.UtcNow)
                    {
                        latest.Status = "Rejected";
                        _uow.MentorAssignments.Update(latest);
                        await _uow.SaveAsync();
                    }
                    else
                    {
                        throw new Exception("This assignment is still pending, cannot register again!");
                    }
                }
                else if (latest.Status == "Approved")
                {
                    throw new Exception("This assignment is already approved, cannot register again!");
                }
            }

            // 5. Create new assignment
            var assignment = new MentorAssignment
            {
                MentorId = dto.MentorId,
                HackathonId = dto.HackathonId,
                TeamId = dto.TeamId,
                Status = "WaitingMentor",
                AssignedAt = DateTime.UtcNow
            };

            await _uow.MentorAssignments.AddAsync(assignment);
            await _uow.SaveAsync();

            // 6. Send email
            string subject = "📩 Mentor Assignment Notification";
            string body = $@"
            <p>Dear <b>{mentor.FullName}</b>,</p>
            <p>You have been requested as a mentor for team <b>{team.TeamName}</b> in Hackathon <b>{hackathon.HackathonId}</b>.</p>
            <p>Status: <b>{assignment.Status}</b></p>
            <p>Best regards,<br/>Seal System</p>";

            await _emailService.SendEmailAsync(mentor.Email, subject, body);

            return _mapper.Map<MentorAssignmentResponseDto>(assignment);
        }


        public async Task<MentorAssignmentResponseDto> ApproveAsync(int assignmentId)
        {
            var assignment = await _uow.MentorAssignments.GetByIdIncludingAsync(
                a => a.AssignmentId == assignmentId,
                a => a.Mentor,
                a => a.Team,
                a => a.Hackathon
            );
            if (assignment == null) throw new Exception("Assignment not found");

            assignment.Status = "Approved";
            assignment.AssignedAt = DateTime.UtcNow;
            _uow.MentorAssignments.Update(assignment);

            // ⭐ Update HackathonRegistration status
            var registration = await _uow.HackathonRegistrations.GetByIdIncludingAsync(
                r => r.TeamId == assignment.TeamId,
                r => r.Team,
                r => r.Hackathon
            );

            if (registration != null)
            {
                registration.Status = "Approved";
                _uow.HackathonRegistrations.Update(registration);
            }

            await _uow.SaveAsync();

            // ✅ TỰ ĐỘNG TẠO CHATGROUP
            await CreateChatGroupForAssignmentAsync(assignment);

            // ✅ GỬI NOTIFICATION CHO TẤT CẢ TEAM MEMBERS
            var teamMembers = await _uow.TeamMembers.GetAllAsync(tm => tm.TeamId == assignment.TeamId);
            var teamMemberIds = teamMembers.Select(tm => tm.UserId).ToList();

            await _notificationService.CreateNotificationsAsync(
                teamMemberIds,
                $"Mentor {assignment.Mentor?.FullName ?? "Unknown"} has been assigned to your team"
            );

            // Gửi mail cho TeamLeader
            var team = await _uow.Teams.GetByIdAsync(assignment.TeamId);
            var leader = team?.TeamLeader;

            if (leader != null && !string.IsNullOrEmpty(leader.Email))
            {
                string subject = "Mentor Assignment Approved";
                string body = $@"
                <p>Dear {leader.FullName},</p>
                <p>Your team <b>{registration?.Team?.TeamName}</b> has been approved by Mentor {assignment.Mentor?.FullName}.</p>
                <p>Congratulations! You can now start working with your mentor.</p>
                <p>Best regards,<br/>Seal System</p>";

                await _emailService.SendEmailAsync(leader.Email, subject, body);
            }

            return _mapper.Map<MentorAssignmentResponseDto>(assignment);
        }
        public async Task<MentorAssignmentResponseDto> RejectAsync(int assignmentId)
        {
            var assignment = await _uow.MentorAssignments.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found");

            assignment.Status = "Rejected";
            assignment.AssignedAt = DateTime.UtcNow;
            _uow.MentorAssignments.Update(assignment);

            // ⭐ Update HackathonRegistration status
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == assignment.TeamId);

            if (registration != null)
            {
                // Team phải quay lại waiting mentor để request mentor mới
                registration.Status = "WaitingMentor";
                _uow.HackathonRegistrations.Update(registration);
            }

            await _uow.SaveAsync();

            // Send email TeamLeader
            var team = await _uow.Teams.GetByIdAsync(assignment.TeamId);
            var leader = team?.TeamLeader;

            if (leader != null && !string.IsNullOrEmpty(leader.Email))
            {
                string subject = "Mentor Assignment Rejected";
                string body = $@"
                <p>Dear {leader.FullName},</p>
                <p>Your mentor request for team <b>{team.TeamName}</b> was rejected.</p>
                <p>You may try requesting another mentor.</p>
                <p>Best regards,<br/>Seal System</p>";

                await _emailService.SendEmailAsync(leader.Email, subject, body);
            }

            // ✅ GỬI NOTIFICATION CHO TEAM LEADER
            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = team.TeamLeaderId,
                Message = "Mentor has rejected your request. Please request another mentor."
            });

            return _mapper.Map<MentorAssignmentResponseDto>(assignment);
        }



        public async Task<IEnumerable<MentorAssignmentResponseDto>> GetByMentorAsync(int mentorId)
        {
            var assignments = await _uow.MentorAssignments.GetAllAsync(
                filter: a => a.MentorId == mentorId,
                includeProperties: "Team"
            );

            return _mapper.Map<IEnumerable<MentorAssignmentResponseDto>>(assignments);
        }

        // ✅ METHOD MỚI: Tạo ChatGroup
        private async Task CreateChatGroupForAssignmentAsync(MentorAssignment assignment)
        {
            // Kiểm tra xem ChatGroup đã tồn tại chưa
            var existingGroup = await _uow.ChatGroups.FirstOrDefaultAsync(
                cg => cg.MentorId == assignment.MentorId &&
                      cg.TeamId == assignment.TeamId &&
                      cg.HackathonId == assignment.HackathonId);

            if (existingGroup != null)
            {
                // Đã có rồi, không cần tạo nữa
                return;
            }

            // Lấy thông tin để tạo GroupName
            var mentor = await _uow.Users.GetByIdAsync(assignment.MentorId);
            var team = await _uow.Teams.GetByIdAsync(assignment.TeamId);
            var hackathon = await _uow.Hackathons.GetByIdAsync(assignment.HackathonId);

            // Tạo ChatGroup mới
            var chatGroup = new ChatGroup
            {
                MentorId = assignment.MentorId,
                TeamId = assignment.TeamId,
                HackathonId = assignment.HackathonId,
                GroupName = $"{team?.TeamName} - {mentor?.FullName} - {hackathon?.Name}",
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = null // Chưa có message
            };

            await _uow.ChatGroups.AddAsync(chatGroup);
            await _uow.SaveAsync();

            // Optional: Log hoặc gửi notification
            // _logger.LogInformation("ChatGroup {ChatGroupId} created for Mentor {MentorId} and Team {TeamId}", 
            //     chatGroup.ChatGroupId, assignment.MentorId, assignment.TeamId);
        }
    }
}
