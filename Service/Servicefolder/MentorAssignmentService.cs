using AutoMapper;
using Common.DTOs.AssignedTeamDto;
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
        public MentorAssignmentService(IUOW uow, IMapper mapper, IEmailService emailService, IConfiguration configuration)
        {
            _uow = uow;
            _mapper = mapper;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<IEnumerable<AssignedTeamDto>> ViewAssignedTeamsAsync(int mentorId)
        {
            var assignments = await _uow.MentorAssignmentRepository.GetTeamsByMentorIdAsync(mentorId);
            return assignments.Select(a => new AssignedTeamDto
            {
                AssignmentId = a.AssignmentId,
                TeamId = a.Team?.TeamId ?? 0,
                TeamName = a.Team?.TeamName ?? string.Empty,
                ChapterId = a.Chapter?.ChapterId ?? 0,
                ChapterName = a.Chapter?.ChapterName ?? string.Empty,
                AssignedAt = a.AssignedAt,
                LeaderId = a.Team?.LeaderId,
                LeaderName = a.Team?.Leader?.FullName,
                Status = a.Status
            });
        }

        public async Task<MentorAssignmentResponseDto> RegisterAsync(MentorAssignmentCreateDto dto)
        {
            // 1. Validate Mentor
            var mentor = await _uow.Users.GetByIdAsync(dto.MentorId);
            if (mentor == null || mentor.RoleId != 5) // 5 = Mentor
                throw new Exception("Invalid mentor!");

            if (string.IsNullOrEmpty(mentor.Email))
                throw new Exception("Mentor email not found!");

            // 2. Validate Team
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found!");

            // 3. Validate Chapter
            var chapter = await _uow.Chapters.GetByIdAsync(dto.ChapterId);
            if (chapter == null)
                throw new Exception("Chapter not found!");

            // 4. Tạo bản ghi mới
            var assignment = new MentorAssignment
            {
                MentorId = dto.MentorId,
                ChapterId = dto.ChapterId,
                TeamId = dto.TeamId,
                Status = "Pending",
                AssignedAt = null
            };

            await _uow.MentorAssignments.AddAsync(assignment);
            await _uow.SaveAsync();

            // 5. Tạo link accept / reject
            var baseUrl = _configuration["App:BaseUrl"];
            var acceptUrl = $"{baseUrl}/api/mentorassignments/accept/{assignment.AssignmentId}";
            var rejectUrl = $"{baseUrl}/api/mentorassignments/reject/{assignment.AssignmentId}";

            // 6. Soạn email đẹp hơn
            string subject = "📩 Mentor Assignment Request";
            string body = $@"
<p>Dear <b>{mentor.FullName}</b>,</p>
<p>The team <b>{team.TeamName}</b> from Chapter <b>{chapter.ChapterName}</b> has requested you as their mentor.</p>
<p>Please choose one of the following options:</p>
<p>
    <a href='{acceptUrl}' style='padding:10px 20px;background:#28a745;color:white;text-decoration:none;border-radius:5px;'>✅ Accept</a>
    &nbsp;&nbsp;
    <a href='{rejectUrl}' style='padding:10px 20px;background:#dc3545;color:white;text-decoration:none;border-radius:5px;'>❌ Reject</a>
</p>
<p>Best regards,<br/>Seal System</p>";

            // 7. Gửi email
            await _emailService.SendEmailAsync(mentor.Email, subject, body);

            // 8. Trả kết quả về client
            return _mapper.Map<MentorAssignmentResponseDto>(assignment);
        }



        public async Task<MentorAssignmentResponseDto> ApproveAsync(int assignmentId)
        {
            var assignment = await _uow.MentorAssignments.GetByIdAsync(assignmentId);
            if (assignment == null) throw new Exception("Assignment not found");

            assignment.Status = "Approved";
            assignment.AssignedAt = DateTime.UtcNow;

            _uow.MentorAssignments.Update(assignment);
            await _uow.SaveAsync();

            // Gửi mail cho TeamLeader
            var team = await _uow.Teams.GetByIdAsync(assignment.TeamId);
            var leader = team?.Leader;

            if (leader != null && !string.IsNullOrEmpty(leader.Email))
            {
                string subject = "Mentor Assignment Approved";
                string body = $@"
            <p>Dear {leader.FullName},</p>
            <p>Your team <b>{team.TeamName}</b> has been approved by Mentor ID {assignment.MentorId}.</p>
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
            assignment.AssignedAt = null;

            _uow.MentorAssignments.Update(assignment);
            await _uow.SaveAsync();

            // Gửi mail cho TeamLeader
            var team = await _uow.Teams.GetByIdAsync(assignment.TeamId);
            var leader = team?.Leader;

            if (leader != null && !string.IsNullOrEmpty(leader.Email))
            {
                string subject = "Mentor Assignment Rejected";
                string body = $@"
            <p>Dear {leader.FullName},</p>
            <p>Unfortunately, your mentor request for team <b>{team.TeamName}</b> was rejected.</p>
            <p>You may try requesting another mentor.</p>
            <p>Best regards,<br/>Seal System</p>";

                await _emailService.SendEmailAsync(leader.Email, subject, body);
            }

            return _mapper.Map<MentorAssignmentResponseDto>(assignment);
        }


        public async Task<IEnumerable<MentorAssignmentResponseDto>> GetByMentorAsync(int mentorId)
        {
            var assignments = await _uow.MentorAssignments.GetAllAsync(
                filter: a => a.MentorId == mentorId,
                includeProperties: "Team,Chapter"
            );

            return _mapper.Map<IEnumerable<MentorAssignmentResponseDto>>(assignments);
        }
    }
}
