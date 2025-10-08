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
    public class PhaseChallengeService : IPhaseChallengeService
    {
        private readonly IUOW _uow;

        public PhaseChallengeService(IUOW uow)
        {
            _uow = uow;
        }

        public async Task<bool> AssignRandomChallengesToHackathonPhasesAsync(int hackathonId, int perPhase)
        {
            // Lấy tất cả phase thuộc hackathon này
            var phases = await _uow.HackathonPhaseRepository.GetAllAsync(p => p.HackathonId == hackathonId);
            if (phases == null || !phases.Any())
                throw new Exception("No phases found for this hackathon.");

            // Lấy tất cả challenge có status = "Complete"
            var challenges = await _uow.ChallengeRepository.GetAllAsync(c => c.Status == "Complete");
            if (challenges == null || !challenges.Any())
                throw new Exception("No available challenges with status = 'Complete'.");

            // Shuffle ngẫu nhiên danh sách challenge
            var random = new Random();
            var shuffledChallenges = challenges.OrderBy(c => random.Next()).ToList();

            // Tổng số đề có thể gán
            int totalAvailable = shuffledChallenges.Count;
            int totalNeeded = phases.Count() * perPhase;

            // Dùng index để theo dõi challenge đã dùng
            int currentIndex = 0;

            foreach (var phase in phases)
            {
                var selectedChallenges = new List<Challenge>();

                // Nếu còn đủ đề để gán cho phase này
                for (int i = 0; i < perPhase; i++)
                {
                    if (currentIndex >= totalAvailable)
                        break; // hết đề

                    selectedChallenges.Add(shuffledChallenges[currentIndex]);
                    currentIndex++;
                }

                // Nếu phase này không có đủ đề -> bỏ qua
                if (!selectedChallenges.Any())
                    continue;

                // Gán vào bảng PhaseChallenge
                foreach (var challenge in selectedChallenges)
                {
                    var phaseChallenge = new PhaseChallenge
                    {
                        PhaseId = phase.PhaseId,
                        ChallengeId = challenge.ChallengeId,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _uow.PhaseChallenges.AddAsync(phaseChallenge);
                }
            }

            await _uow.SaveAsync();
            return true;
        }

    }
}
