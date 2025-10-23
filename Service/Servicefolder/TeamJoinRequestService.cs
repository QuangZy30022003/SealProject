using AutoMapper;
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

        public TeamJoinRequestService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
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

            // 3. Check user đã là member của team khác chưa
            var existingMember = await _uow.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == userId);
            if (existingMember != null)
                throw new Exception("You are already a member of another team.");

            // 4. Check đã có request pending chưa
            var existingRequest = await _uow.TeamJoinRequests.FirstOrDefaultAsync(r =>
                r.TeamId == dto.TeamId && r.UserId == userId && r.Status == JoinRequestStatus.Pending);
            if (existingRequest != null)
                throw new Exception("You already have a pending request for this team.");

            // 5. Check team đã đủ member chưa (giả sử max 5 members)
            var memberCount = await _uow.TeamMembers.CountAsync(tm => tm.TeamId == dto.TeamId);
            if (memberCount >= 5)
                throw new Exception("This team is full.");

            // 6. Tạo request mới
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
            var request = await _uow.TeamJoinRequests.GetByIdAsync(requestId);
            if (request == null)
                return null;

            // Check leader có quyền duyệt không
            var team = await _uow.Teams.GetByIdAsync(request.TeamId);
            if (team?.LeaderId != leaderId)
                throw new UnauthorizedAccessException("Only team leader can respond to join requests.");

            // Check request chưa được xử lý
            if (request.Status != JoinRequestStatus.Pending)
                throw new Exception("This request has already been processed.");

            request.Status = dto.Status;
            request.LeaderResponse = dto.LeaderResponse;
            request.RespondedAt = DateTime.UtcNow;

            // Nếu approved, thêm user vào team
            if (dto.Status == JoinRequestStatus.Approved)
            {
                // Check team chưa đủ member
                var memberCount = await _uow.TeamMembers.CountAsync(tm => tm.TeamId == request.TeamId);
                if (memberCount >= 5)
                    throw new Exception("Team is full, cannot add more members.");

                // Check user chưa là member của team khác
                var existingMember = await _uow.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == request.UserId);
                if (existingMember != null)
                    throw new Exception("User is already a member of another team.");

                // Thêm user vào team
                var teamMember = new TeamMember
                {
                    TeamId = request.TeamId,
                    UserId = request.UserId,
                    RoleInTeam = "Member"
                };

                await _uow.TeamMembers.AddAsync(teamMember);
            }

            _uow.TeamJoinRequests.Update(request);
            await _uow.SaveAsync();

            return _mapper.Map<JoinRequestResponseDto>(request);
        }

        public async Task<JoinRequestResponseDto?> GetJoinRequestByIdAsync(int requestId)
        {
            var request = await _uow.TeamJoinRequests.GetByIdAsync(requestId);
            return request == null ? null : _mapper.Map<JoinRequestResponseDto>(request);
        }
    }
}
