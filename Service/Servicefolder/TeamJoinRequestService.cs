using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.TeamJoinRequestDto;
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
    public class TeamJoinRequestService : ITeamJoinRequestService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public TeamJoinRequestService(IUOW uow, IMapper mapper, INotificationService notificationService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<JoinRequestResponseDto> CreateJoinRequestAsync(CreateJoinRequestDto dto, int userId)
        {
            // 1. Check team tồn tại
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found.");

            // 2. Check user tồn tại
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            // 3. Check user đã ở team khác chưa
            if (team.HackathonId == null)
            {
                // 🧩 Team chưa có hackathon — check xem user đã trong team khác cũng chưa đăng ký chưa
                bool alreadyInUnregisteredTeam = await _uow.Teams.ExistsAsync(t =>
                    t.HackathonId == null &&
                    (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)) &&
                    t.TeamId != team.TeamId);

                if (alreadyInUnregisteredTeam)
                    throw new InvalidOperationException("You are already in another team that hasn't registered for any hackathon.");
            }
            else
            {
                // 🧩 Team có hackathon — check cùng hackathon
                bool alreadyInTeamSameHackathon = await _uow.Teams.ExistsAsync(t =>
                    t.HackathonId == team.HackathonId &&
                    (t.TeamLeaderId == userId || t.TeamMembers.Any(tm => tm.UserId == userId)) &&
                    t.TeamId != team.TeamId);

                if (alreadyInTeamSameHackathon)
                    throw new InvalidOperationException("You are already a member of another team in this hackathon.");
            }

            // 4. check lời mời pending
            var pendingInvite = await _uow.TeamInvitations.FirstOrDefaultAsync(i =>
                i.InvitedEmail.ToLower() == user.Email.ToLower() &&
                i.Status == "Pending");
            if (pendingInvite != null)
                throw new Exception("You already have a pending invitation.");

            // 5. Check đã có request pending chưa
            var existingRequest = await _uow.TeamJoinRequests.FirstOrDefaultAsync(r =>
                r.TeamId == dto.TeamId && r.UserId == userId && r.Status == JoinRequestStatus.Pending);
            if (existingRequest != null)
                throw new Exception("You already have a pending request for this team.");

            // 6. Check team đã đủ member chưa
            var memberCount = await _uow.TeamMembers.CountAsync(tm => tm.TeamId == dto.TeamId);
            if (memberCount >= 5)
                throw new Exception("This team is full.");

            // 7. Tạo request
            var request = new TeamJoinRequest
            {
                TeamId = dto.TeamId,
                UserId = userId,
                Message = dto.Message,
                Status = JoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.TeamJoinRequests.AddAsync(request);
            await _uow.SaveAsync();

            return _mapper.Map<JoinRequestResponseDto>(request);
        }

        public async Task<IEnumerable<JoinRequestResponseDto>> GetJoinRequestsForTeamAsync(int teamId)
        {
            var requests = await _uow.TeamJoinRequests.GetAllAsync(
                r => r.TeamId == teamId,
                includeProperties: "User,Team"
            );

            return _mapper.Map<IEnumerable<JoinRequestResponseDto>>(requests);
        }

        public async Task<IEnumerable<JoinRequestResponseDto>> GetJoinRequestsByUserAsync(int userId)
        {
            var requests = await _uow.TeamJoinRequests.GetAllAsync(
                r => r.UserId == userId,
                includeProperties: "User,Team"
            );

            return _mapper.Map<IEnumerable<JoinRequestResponseDto>>(requests);
        }

        public async Task<JoinRequestResponseDto?> RespondToJoinRequestAsync(int requestId, RespondToJoinRequestDto dto, int leaderId)
        {
            // 1️ Load request
            var request = await _uow.TeamJoinRequests.GetByIdAsync(requestId);
            if (request == null)
                return null;

            // 2️ Xác minh quyền xử lý
            var team = await _uow.Teams.GetByIdAsync(request.TeamId);
            if (team == null)
                throw new Exception("Team not found.");
            if (team.TeamLeaderId != leaderId)
                throw new UnauthorizedAccessException("Only the team leader can respond to join requests.");

            // 3️ Kiểm tra trạng thái hiện tại
            if (request.Status != JoinRequestStatus.Pending)
                throw new Exception("This request has already been processed.");

            string? rejectReason = null; // nếu có lỗi, sẽ dùng để reject

            // 2️ Kiểm tra điều kiện trước khi xét duyệt
            // 2.1️ Check team đã đủ chưa
            var memberCount = await _uow.TeamMembers.CountAsync(tm => tm.TeamId == request.TeamId);
            if (memberCount >= 5)
                rejectReason = "Team is full (maximum 5 members).";

            // 2.2️ Check user đã ở team khác chưa
            var existingMember = await _uow.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == request.UserId);
            if (existingMember != null)
                rejectReason = "User is already a member of another team.";

            // 2.3️ Check hackathon consistency
            var userOtherTeam = await _uow.Teams.FirstOrDefaultAsync(t =>
                (t.TeamLeaderId == request.UserId || t.TeamMembers.Any(tm => tm.UserId == request.UserId)) &&
                t.TeamId != request.TeamId);

            if (userOtherTeam != null)
            {
                if (userOtherTeam.HackathonId != null && team.HackathonId == userOtherTeam.HackathonId)
                    rejectReason = "User is already in another team for the same hackathon.";

                if (userOtherTeam.HackathonId == null && team.HackathonId == null)
                    rejectReason = "User is already in another unregistered team.";
            }

            // 3️ Nếu có lỗi → auto reject
            if (rejectReason != null)
            {
                request.Status = JoinRequestStatus.Rejected;
                request.LeaderResponse = rejectReason;
                request.RespondedAt = DateTime.UtcNow;

                _uow.TeamJoinRequests.Update(request);
                await _uow.SaveAsync();

                var rejected = await _uow.TeamJoinRequests.GetByIdIncludingAsync(
                    r => r.RequestId == requestId,
                    r => r.User,
                    r => r.Team
                );

                return _mapper.Map<JoinRequestResponseDto>(rejected);
            }

            // 4️ Không có lỗi → xử lý theo DTO
            request.Status = dto.Status;
            request.LeaderResponse = dto.LeaderResponse;
            request.RespondedAt = DateTime.UtcNow;

            // 5️ Nếu leader Approve → thêm vào team
            if (dto.Status == JoinRequestStatus.Approved)
            {
                await _uow.TeamMembers.AddAsync(new TeamMember
                {
                    TeamId = request.TeamId,
                    UserId = request.UserId,
                    RoleInTeam = "Member"
                });
                // ✅ GỬI NOTIFICATION KHI APPROVED
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = request.UserId,
                    Message = $"Your request to join team {team.TeamName} has been approved!"
                });
            }
            else if (dto.Status == JoinRequestStatus.Rejected)
            {
                // ✅ GỬI NOTIFICATION KHI REJECTED
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = request.UserId,
                    Message = $"Your request to join team {team.TeamName} has been rejected. Reason: {dto.LeaderResponse}"
                });
            }

            // 6️ Lưu thay đổi
            _uow.TeamJoinRequests.Update(request);
            await _uow.SaveAsync();

            // 7️ Load lại request có Include để map đầy đủ
            var updatedRequest = await _uow.TeamJoinRequests.GetByIdIncludingAsync(
                r => r.RequestId == requestId,
                r => r.User,
                r => r.Team
            );

            return _mapper.Map<JoinRequestResponseDto>(updatedRequest);
        }

        public async Task<JoinRequestResponseDto?> GetJoinRequestByIdAsync(int requestId)
        {
            var request = await _uow.TeamJoinRequests.GetByIdIncludingAsync(
                r => r.RequestId == requestId,
                r => r.User,
                r => r.Team
            );
            return request == null ? null : _mapper.Map<JoinRequestResponseDto>(request);
        }
    }
}
