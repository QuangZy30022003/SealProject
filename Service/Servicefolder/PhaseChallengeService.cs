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
            // 🔹 1. Lấy các phase thuộc hackathon
            var phases = await _uow.HackathonPhaseRepository.GetAllAsync(p => p.HackathonId == hackathonId);
            if (phases == null || !phases.Any())
                throw new Exception("No phases found for this hackathon.");

            // 🔹 2. Lấy danh sách challenge "Complete"
            var challenges = await _uow.ChallengeRepository.GetAllAsync(c => c.Status == "Complete");
            if (challenges == null || !challenges.Any())
                throw new Exception("No challenges with status = 'Complete'.");

            // 🔹 3. Lấy các challenge đã được sử dụng
            var usedChallengeIds = await _uow.PhaseChallengeRepository.GetUsedChallengeIdsAsync();
            var availableChallenges = challenges
                .Where(c => !usedChallengeIds.Contains(c.ChallengeId))
                .ToList();

            if (!availableChallenges.Any())
                throw new Exception("No unused challenges available for assignment.");

            // 🔹 4. Kiểm tra đủ số lượng
            int totalNeeded = phases.Count() * perPhase;
            int totalAvailable = availableChallenges.Count;

            if (totalAvailable < totalNeeded)
                throw new Exception($"Not enough challenges to assign. Needed: {totalNeeded}, available: {totalAvailable}.");

            // 🔹 5. Random và gán đề cho từng phase
            var random = new Random();
            var shuffled = availableChallenges.OrderBy(c => random.Next()).ToList();

            int currentIndex = 0;

            foreach (var phase in phases)
            {
                for (int i = 0; i < perPhase; i++)
                {
                    var challenge = shuffled[currentIndex++];
                    var phaseChallenge = new PhaseChallenge
                    {
                        PhaseId = phase.PhaseId,
                        ChallengeId = challenge.ChallengeId,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _uow.PhaseChallengeRepository.AddAsync(phaseChallenge);
                }
            }

            await _uow.SaveAsync();
            return true;
        }
    }
}
