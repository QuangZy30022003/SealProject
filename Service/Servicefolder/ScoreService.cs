using AutoMapper;
using Common.DTOs.ScoreDto;
using Common.DTOs.Submission;
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
    public class ScoreService : IScoreService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public ScoreService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        
        public async Task UpdateAverageAndRankAsync(int submissionId)
        {
            // Lấy submission
            var submission = await _uow.Submissions.GetByIdAsync(submissionId);
            if (submission == null) return;

            // Lấy GroupTeam của submission
            var groupTeam = await _uow.GroupsTeams.FirstOrDefaultAsync(gt => gt.TeamId == submission.TeamId);
            if (groupTeam == null) return;

            // Lấy tất cả submission của team trong phase
            var teamSubmissions = await _uow.Submissions
                .GetAllAsync(s => s.TeamId == groupTeam.TeamId && s.PhaseId == submission.PhaseId);

            decimal totalAverageScore = 0;
            var scoredSubmissions = new List<Submission>();
            foreach (var sub in teamSubmissions)
            {
                var allScores = await _uow.Scores.GetAllAsync(s => s.SubmissionId == sub.SubmissionId);
                if (allScores.Any())
                {
                    scoredSubmissions.Add(sub);

                    // Nhóm theo JudgeId
                    var scoresByJudge = allScores
                        .GroupBy(s => s.JudgeId)
                        .Select(g =>
                        {
                            var sumScore = g.Sum(s => s.Score1);
                            Console.WriteLine($"JudgeID {g.Key} - Total score: {sumScore}");
                            return sumScore;
                        })
                        .ToList();

                    // Trung bình submission nếu nhiều judge
                    decimal submissionAverage = scoresByJudge.Any() ? scoresByJudge.Average() : 0;

                    totalAverageScore += submissionAverage;
                }
            }

            // Trung bình tất cả submission đã chấm
            //     groupTeam.AverageScore = scoredSubmissions.Any() ? totalAverageScore / scoredSubmissions.Count : 0;
            decimal averageScore = scoredSubmissions.Any()
         ? totalAverageScore / scoredSubmissions.Count
         : 0;

            // 🔥 5. CỘNG / TRỪ PENALTY - BONUS
            var penalties = await _uow.PenaltiesBonuses.GetAllAsync(p =>
                p.TeamId == groupTeam.TeamId &&
                p.PhaseId == submission.PhaseId &&
                !p.IsDeleted);

            decimal penaltyTotal = penalties.Sum(p => p.Points);

            groupTeam.AverageScore = averageScore + penaltyTotal;


            _uow.GroupsTeams.Update(groupTeam);
            await _uow.SaveAsync();

            // Cập nhật Rank: lấy tất cả team trong group
            var teamsInGroup = await _uow.GroupsTeams.GetAllAsync(gt => gt.GroupId == groupTeam.GroupId);
            var rankedTeams = teamsInGroup.OrderByDescending(gt => gt.AverageScore).ToList();

            for (int i = 0; i < rankedTeams.Count; i++)
            {
                rankedTeams[i].Rank = i + 1;
                _uow.GroupsTeams.Update(rankedTeams[i]);
            }

            await _uow.SaveAsync();
        }

      

        public async Task<List<TeamScoreDto>> GetTeamScoresByGroupAsync(int groupId)
        {
            var groupTeams = await _uow.GroupsTeams.GetAllIncludingAsync(
                gt => gt.GroupId == groupId,
                gt => gt.Team // Nếu bạn muốn lấy thông tin team, cần relation Team trong GroupTeam
            );

            if (!groupTeams.Any())
                throw new Exception("No teams found for this group.");

            return groupTeams.Select(gt => new TeamScoreDto
            {
                TeamId = gt.TeamId,
                TeamName = gt.Team?.TeamName ?? "Unknown", // nếu có relation Team
                AverageScore = gt.AverageScore.Value,
                Rank = gt.Rank.Value,
            })
            .OrderByDescending(t => t.AverageScore)
            .ToList();
        }
        public async Task<List<SubmissionScoresGroupedDto>> GetMyScoresGroupedBySubmissionAsync(int judgeId, int phaseId)
        {
            // Lấy tất cả score của judge trong phase
            var scores = await _uow.Scores.GetAllIncludingAsync(
                s => s.JudgeId == judgeId && s.Submission.PhaseId == phaseId,
                s => s.Submission,
                s => s.Criteria
            );

            if (!scores.Any())
                return new List<SubmissionScoresGroupedDto>();

            // Nhóm theo SubmissionId
            var grouped = scores
                .GroupBy(s => s.SubmissionId)
                .Select(g =>
                {
                    var total = g.Sum(s => s.Score1); // tổng điểm thay vì trung bình
                    return new SubmissionScoresGroupedDto
                    {
                        SubmissionId = g.Key,
                        SubmissionName = g.First().Submission.Title,
                        TotalScore = total,
                        Scores = _mapper.Map<List<ScoreResponseDto>>(g.ToList())
                    };
                })
                .ToList();

            return grouped;
        }


     

            public async Task<SubmissionScoresResponseDto> ScoreSubmissionAsync(int judgeId, ScoreSubmissionRequestDto request)
            {

                if (request.CriteriaScores == null || !request.CriteriaScores.Any())
                    throw new Exception("No scores provided.");

                // ---------------------------
                // 1. Validate submission
                // ---------------------------
                var submission = await _uow.Submissions.GetByIdAsync(request.SubmissionId);
                if (submission == null)
                    throw new Exception("Submission not found");


                var phase = await _uow.HackathonPhases.GetByIdAsync(submission.PhaseId);
                if (phase == null)
                    throw new Exception("Phase not found");

                int hackathonId = phase.HackathonId;

                // Find Final Phase
                var allPhases = await _uow.HackathonPhases
                    .GetAllAsync(x => x.HackathonId == hackathonId);

                var finalPhase = allPhases.OrderByDescending(x => x.EndDate).First();

                bool isFinal = (submission.PhaseId == finalPhase.PhaseId);

                // ---------------------------
                // 2. Validate judge assignment (PHASE ONLY)
                // ---------------------------
                Console.WriteLine($"Validating JudgeAssignment (Phase only)...");

                var assignments = await _uow.JudgeAssignments.GetAllAsync(a =>
                    a.JudgeId == judgeId &&
                    a.HackathonId == hackathonId &&
                    (a.PhaseId == null || a.PhaseId == submission.PhaseId)
                );


                if (!assignments.Any())
                    throw new Exception("Judge is not assigned to this phase");

                // ---------------------------
                // 3. Validate Criteria
                // ---------------------------
                Console.WriteLine("Validating criteria...");

                foreach (var item in request.CriteriaScores)
                {
                    var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                        c.CriteriaId == item.CriterionId &&
                        c.PhaseId == submission.PhaseId
                    );

                    if (criterion == null)
                        throw new Exception($"Invalid criterion {item.CriterionId}");

                    if (item.Score > criterion.Weight || item.Score < 0)
                        throw new Exception($"Score for criterion {item.CriterionId} must be between 0 and {criterion.Weight}.");
                }

                // ---------------------------
                // 3.5 Check already scored
                var existingScores = await _uow.Scores.GetAllAsync(x =>
                    x.SubmissionId == request.SubmissionId &&
                    x.JudgeId == judgeId);

                if (existingScores.Any())
                    throw new Exception("You have already scored this submission. Please use update API.");

                // ---------------------------
                // 5. Insert new scores
                // ---------------------------
                Console.WriteLine("Inserting new scores...");

                foreach (var item in request.CriteriaScores)
                {
                    var score = new Score
                    {
                        SubmissionId = submission.SubmissionId,
                        JudgeId = judgeId,
                        CriteriaId = item.CriterionId,
                        Score1 = item.Score,
                        Comment = item.Comment,
                        ScoredAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"Adding Score: Criteria={item.CriterionId}, Score={item.Score}");

                    await _uow.Scores.AddAsync(score);
                }

                await _uow.SaveAsync();

                // ---------------------------
                // 6. Ranking logic
                // ---------------------------
                if (!isFinal)
                {
                    Console.WriteLine("Updating average + rank (normal round)...");
                    await UpdateAverageAndRankAsync(submission.SubmissionId);
                }
                else
                {
                    Console.WriteLine("Updating FINAL ranking...");
                    await UpdateFinalRankingAsync(submission, hackathonId);
                }

                // ---------------------------
                // 7. Build response
                // ---------------------------
                var result = new SubmissionScoresResponseDto
                {
                    SubmissionId = submission.SubmissionId,
                    Scores = request.CriteriaScores
                        .Select(x => new ScoreItemDto
                        {
                            CriteriaId = x.CriterionId,
                            ScoreValue = x.Score,
                            Comment = x.Comment
                        })
                        .ToList()
                };

                Console.WriteLine("[END] ScoreSubmission Completed");

                return result;
            }

        public async Task UpdateFinalRankingAsync(Submission submission, int hackathonId)
        {
            var allScores = await _uow.Scores.GetAllAsync(x => x.SubmissionId == submission.SubmissionId);

            decimal totalScore = allScores
                .GroupBy(s => s.JudgeId)
                .Select(g => g.Sum(s => s.Score1))
                .Average();

            var penalties = await _uow.PenaltiesBonuses.GetAllAsync(p =>
      p.TeamId == submission.TeamId &&
      p.PhaseId == submission.PhaseId &&
      !p.IsDeleted);

            decimal penaltyTotal = penalties.Sum(p => p.Points);

            totalScore += penaltyTotal;

            var ranking = await _uow.Rankings.FirstOrDefaultAsync(x =>
                x.TeamId == submission.TeamId &&
                x.HackathonId == hackathonId);

            if (ranking == null)
            {
                ranking = new Ranking
                {
                    TeamId = submission.TeamId,
                    HackathonId = hackathonId,
                    TotalScore = totalScore,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.Rankings.AddAsync(ranking);
            }
            else
            {
                ranking.TotalScore = totalScore;
                ranking.UpdatedAt = DateTime.UtcNow;
                _uow.Rankings.Update(ranking);
            }

            await _uow.SaveAsync();

            // Re-rank all
            var allRankings = (await _uow.Rankings.GetAllAsync(
                x => x.HackathonId == hackathonId,
                orderBy: q => q.OrderByDescending(r => r.TotalScore)
            )).ToList();

            int rank = 1;
            foreach (var r in allRankings)
            {
                r.Rank = rank++;
                _uow.Rankings.Update(r);
            }

            await _uow.SaveAsync();
        }


        public async Task<ScoreDetailDto> UpdateScoreByIdAsync(
    int judgeId,
    int scoreId,
    ScoreUpdateByIdDto request)
        {
            // 1. Lấy score
            var score = await _uow.Scores.FirstOrDefaultAsync(s =>
                s.ScoreId == scoreId);

            if (score == null)
                throw new Exception("Score not found");

            // 2. BẢO MẬT: chỉ judge tạo mới được update
            if (score.JudgeId != judgeId)
                throw new Exception("You are not allowed to update this score");

            // 3. Validate criterion weight
            var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                c.CriteriaId == score.CriteriaId);

            if (criterion == null)
                throw new Exception("Criterion not found");

            if (request.ScoreValue < 0 || request.ScoreValue > criterion.Weight)
                throw new Exception($"Score must be between 0 and {criterion.Weight}");

            // 4. Update
            score.Score1 = request.ScoreValue;
            score.Comment = request.Comment;
            score.ScoredAt = DateTime.UtcNow;

            _uow.Scores.Update(score);
            await _uow.SaveAsync();

            // 5. Update ranking
            var submission = await _uow.Submissions.GetByIdAsync(score.SubmissionId);

            if (submission != null)
            {
                await UpdateAverageAndRankAsync(submission.SubmissionId);
            }

            // 6. Response
            return _mapper.Map<ScoreDetailDto>(score);
        }

        public async Task<TeamOverviewDto> GetTeamOverviewAsync(
    int teamId,
    int phaseId)
        {
            var team = await _uow.Teams.GetByIdAsync(teamId)
                ?? throw new ArgumentException("Team not found");

            // 1. GroupTeam (AverageScore, Rank)
            var groupTeam = (await _uow.GroupsTeams.GetAllIncludingAsync(
                gt => gt.TeamId == teamId
                      && gt.Group.Track.PhaseId == phaseId,
                gt => gt.Group,
                gt => gt.Group.Track
            )).FirstOrDefault();

            // 2. Lấy tất cả score của team trong phase
            var scores = await _uow.Scores.GetAllIncludingAsync(
                s => s.Submission.TeamId == teamId
                     && s.Criteria.PhaseId == phaseId,
                s => s.Criteria,
                s => s.Submission
            );

            // 3. Group theo Criterion → tính điểm TB
            var criteriaScores = scores
           .GroupBy(s => s.CriteriaId)
           .Select(g => new CriterionScoreDto
           {
               CriterionId = g.Key,
               Score = Math.Round(g.Average(x => x.Score1), 2),

               // 🔥 lấy comment đầu tiên KHÔNG NULL
               Comment = g
                   .Where(x => !string.IsNullOrEmpty(x.Comment))
                   .Select(x => x.Comment)
                   .FirstOrDefault()
           })
           .ToList();


            return new TeamOverviewDto
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                PhaseId = phaseId,
                AverageScore = groupTeam?.AverageScore,
                Rank = groupTeam?.Rank,
                CriteriaScores = criteriaScores
            };
        }


    }
}