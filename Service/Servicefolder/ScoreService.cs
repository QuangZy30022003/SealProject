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

        // ✅ Lưu điểm của nhiều tiêu chí
        public async Task<List<Submission>> GetSubmissionsForJudgeAsync(int judgeId, int phaseId)
        {
            var assignments = await _uow.JudgeAssignments.GetAllAsync(a => a.JudgeId == judgeId && a.PhaseId == phaseId);
            if (!assignments.Any()) return new List<Submission>();

            var trackIds = assignments.Where(a => a.TrackId.HasValue).Select(a => a.TrackId.Value).ToList();

            var submissions = (await _uow.Submissions.GetAllAsync(s => s.PhaseId == phaseId)).ToList();

            if (!trackIds.Any()) return submissions; // Judge được chấm toàn phase

            // Lấy track → group → groupTeams để tìm teamId hợp lệ
            var tracks = (await _uow.Tracks.GetAllAsync(t => trackIds.Contains(t.TrackId))).ToList();
            var allowedTeamIds = new List<int>();

            foreach (var track in tracks)
            {
                var groups = await _uow.Groups.GetAllAsync(g => g.TrackId == track.TrackId);
                foreach (var group in groups)
                {
                    var groupTeams = await _uow.GroupsTeams.GetAllAsync(gt => gt.GroupId == group.GroupId);
                    allowedTeamIds.AddRange(groupTeams.Select(gt => gt.TeamId));
                }
            }

            allowedTeamIds = allowedTeamIds.Distinct().ToList();
            return submissions.Where(s => allowedTeamIds.Contains(s.TeamId)).ToList();
        }


        public async Task<SubmissionScoresResponseDto> CreateOrUpdateScoresAsync(int judgeId, List<ScoreCreateDto> dtos)
        {
            if (!dtos.Any())
                throw new Exception("No scores provided.");

            var submissionId = dtos.First().SubmissionId;

            var submission = await _uow.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                throw new Exception("Submission not found.");

            if (dtos.Any(d => d.SubmissionId != submissionId))
                throw new Exception("All scores must belong to the same submission.");

            var assignments = await _uow.JudgeAssignments
                .GetAllAsync(a => a.JudgeId == judgeId && a.PhaseId == submission.PhaseId);

            if (!assignments.Any())
                throw new Exception("You are not authorized to score this submission.");

            var groupTeam = await _uow.GroupsTeams.FirstOrDefaultAsync(gt => gt.TeamId == submission.TeamId);
            if (groupTeam == null)
                throw new Exception("Team not found in any group.");

            var group = await _uow.Groups.GetByIdAsync(groupTeam.GroupId);
            if (group == null)
                throw new Exception("Group not found.");

            var teamTrackId = group.TrackId;

            var authorized = assignments.Any(a => a.TrackId == null || a.TrackId == teamTrackId);
            if (!authorized)
                throw new Exception("You are not authorized to score this submission.");

            if (dtos.GroupBy(d => d.CriteriaId).Any(g => g.Count() > 1))
                throw new Exception("Duplicate CriteriaId in submitted scores.");

            var result = new SubmissionScoresResponseDto { SubmissionId = submissionId };

            foreach (var dto in dtos)
            {
                // Kiểm tra criterion hợp lệ
                var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                    c.CriteriaId == dto.CriteriaId &&
                    c.PhaseId == submission.PhaseId &&
                    (c.TrackId == null || c.TrackId == teamTrackId));

                if (criterion == null)
                    throw new Exception($"Invalid criterion {dto.CriteriaId} for this submission.");
                if (dto.ScoreValue > criterion.Weight && dto.ScoreValue >= 0)
                    throw new Exception($"ScoreValue for CriteriaId {dto.CriteriaId} cannot exceed Weight {criterion.Weight}.");
                // Kiểm tra score đã tồn tại
                var existing = await _uow.Scores.FirstOrDefaultAsync(s =>
                    s.SubmissionId == dto.SubmissionId &&
                    s.JudgeId == judgeId &&
                    s.CriteriaId == dto.CriteriaId);

                if (existing != null)
                {
                    existing.Score1 = dto.ScoreValue;
                    existing.Comment = dto.Comment;
                    existing.ScoredAt = DateTime.UtcNow;
                    _uow.Scores.Update(existing);
                }
                else
                {
                    var newScore = new Score
                    {
                        SubmissionId = dto.SubmissionId,
                        JudgeId = judgeId,
                        CriteriaId = dto.CriteriaId,
                        Score1 = dto.ScoreValue,
                        Comment = dto.Comment,
                        ScoredAt = DateTime.UtcNow
                    };
                    await _uow.Scores.AddAsync(newScore);
                }

                result.Scores.Add(new ScoreItemDto
                {
                    CriteriaId = dto.CriteriaId,
                    ScoreValue = dto.ScoreValue,
                    Comment = dto.Comment
                });
            }

            await _uow.SaveAsync();
            await UpdateAverageAndRankAsync(submissionId);
            return result;
        }


        public async Task<List<ScoreResponseDto>> GetScoresByJudgeAsync(int judgeId, int phaseId)
        {
            var scores = await _uow.Scores.GetAllIncludingAsync(
                s => s.JudgeId == judgeId && s.Submission.PhaseId == phaseId,
                s => s.Submission,
                s => s.Criteria
            );
            return _mapper.Map<List<ScoreResponseDto>>(scores);
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

                    Console.WriteLine($"--- SubmissionID {sub.SubmissionId} ---");
                    Console.WriteLine($"All scores count: {allScores.Count()}");

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
                    Console.WriteLine($"Submission Average: {submissionAverage}");

                    totalAverageScore += submissionAverage;
                }
            }

            // Trung bình tất cả submission đã chấm
            groupTeam.AverageScore = scoredSubmissions.Any() ? totalAverageScore / scoredSubmissions.Count : 0;
            Console.WriteLine($"Team {groupTeam.TeamId} AverageScore: {groupTeam.AverageScore}");

            _uow.GroupsTeams.Update(groupTeam);
            await _uow.SaveAsync();

            // Cập nhật Rank: lấy tất cả team trong group
            var teamsInGroup = await _uow.GroupsTeams.GetAllAsync(gt => gt.GroupId == groupTeam.GroupId);
            var rankedTeams = teamsInGroup.OrderByDescending(gt => gt.AverageScore).ToList();

            for (int i = 0; i < rankedTeams.Count; i++)
            {
                rankedTeams[i].Rank = i + 1;
                Console.WriteLine($"Team {rankedTeams[i].TeamId} - Rank {rankedTeams[i].Rank}");
                _uow.GroupsTeams.Update(rankedTeams[i]);
            }

            await _uow.SaveAsync();
        }

        public async Task<SubmissionScoresResponseDto> UpdateScoresByCriteriaAsync(int judgeId, List<ScoreCreateDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                throw new Exception("No scores provided.");

            int submissionId = dtos.First().SubmissionId;

            // ✅ Kiểm tra submission tồn tại
            var submission = await _uow.Submissions.GetByIdAsync(submissionId);
            if (submission == null)
                throw new Exception("Submission not found.");

            // ✅ Kiểm tra tất cả scores cùng submission
            if (dtos.Any(d => d.SubmissionId != submissionId))
                throw new Exception("All scores must belong to the same submission.");

            // ✅ Lấy assignment judge trong phase
            var assignments = await _uow.JudgeAssignments
                .GetAllAsync(a => a.JudgeId == judgeId && a.PhaseId == submission.PhaseId);

            if (!assignments.Any())
                throw new Exception("You are not authorized to update this submission.");

            // ✅ Lấy team → group → track
            var groupTeam = await _uow.GroupsTeams.FirstOrDefaultAsync(gt => gt.TeamId == submission.TeamId);
            if (groupTeam == null)
                throw new Exception("Team not found in any group.");

            var group = await _uow.Groups.GetByIdAsync(groupTeam.GroupId);
            if (group == null)
                throw new Exception("Group not found.");

            var teamTrackId = group.TrackId;

            // ✅ Kiểm tra judge có quyền chấm track này không
            var authorized = assignments.Any(a => a.TrackId == null || a.TrackId == teamTrackId);
            if (!authorized)
                throw new Exception("You are not authorized to update this submission.");

            // ✅ Không được gửi trùng CriteriaId
            if (dtos.GroupBy(d => d.CriteriaId).Any(g => g.Count() > 1))
                throw new Exception("Duplicate CriteriaId in submitted scores.");

            var result = new SubmissionScoresResponseDto { SubmissionId = submissionId };

            foreach (var dto in dtos)
            {
                // ✅ Kiểm tra criterion hợp lệ với phase/track
                var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                    c.CriteriaId == dto.CriteriaId &&
                    c.PhaseId == submission.PhaseId &&
                    (c.TrackId == null || c.TrackId == teamTrackId));

                if (criterion == null)
                    throw new Exception($"Invalid criterion {dto.CriteriaId} for this submission.");

                if (dto.ScoreValue > criterion.Weight || dto.ScoreValue < 0)
                    throw new Exception($"ScoreValue for CriteriaId {dto.CriteriaId} must be between 0 and {criterion.Weight}.");

                // ✅ Lấy score đã tồn tại (nếu không có thì lỗi)
                var existing = await _uow.Scores.FirstOrDefaultAsync(s =>
                    s.SubmissionId == dto.SubmissionId &&
                    s.JudgeId == judgeId &&
                    s.CriteriaId == dto.CriteriaId);

                if (existing == null)
                    throw new Exception($"Cannot update: Score for CriteriaId {dto.CriteriaId} not found.");

                // ✅ Cập nhật điểm
                existing.Score1 = dto.ScoreValue;
                existing.Comment = dto.Comment;
                existing.ScoredAt = DateTime.UtcNow;
                _uow.Scores.Update(existing);

                result.Scores.Add(new ScoreItemDto
                {
                    CriteriaId = dto.CriteriaId,
                    ScoreValue = dto.ScoreValue,
                    Comment = dto.Comment
                });
            }

            await _uow.SaveAsync();

            // ✅ Cập nhật điểm trung bình & rank
            await UpdateAverageAndRankAsync(submissionId);

            return result;
        }

        public async Task<List<ScoreWithAverageDto>> GetScoresWithTeamAverageBySubmissionAsync(int submissionId)
        {
            var scores = await _uow.Scores.GetAllIncludingAsync(
                s => s.SubmissionId == submissionId,
                s => s.Criteria,
                s => s.Submission
            );

            if (!scores.Any())
                throw new Exception("No scores found for this submission.");

            // Lấy submission
            var submission = scores.First().Submission;

            // Lấy GroupTeam của team
            var groupTeam = await _uow.GroupsTeams.FirstOrDefaultAsync(gt => gt.TeamId == submission.TeamId);
            if (groupTeam == null)
                throw new Exception("Team not found in any group.");

            // Lấy tất cả submission của team trong group
            var teamSubmissions = await _uow.Submissions.GetAllAsync(s => s.TeamId == groupTeam.TeamId);

            decimal teamAverage = 0;

            var scoredSubmissions = new List<Submission>();
            foreach (var sub in teamSubmissions)
            {
                var allScores = await _uow.Scores.GetAllAsync(s => s.SubmissionId == sub.SubmissionId);
                if (allScores.Any())
                {
                    scoredSubmissions.Add(sub);
                    var avg = allScores.GroupBy(s => s.JudgeId)
                                       .Select(g => g.Sum(s => s.Score1))
                                       .DefaultIfEmpty(0)
                                       .Average();
                    teamAverage += avg;
                }
            }

            teamAverage = scoredSubmissions.Any() ? teamAverage / scoredSubmissions.Count : 0;

            return scores.Select(s => new ScoreWithAverageDto
            {
                ScoreId = s.ScoreId,
                SubmissionId = s.SubmissionId,
                CriteriaName = s.Criteria?.Name ?? "Unknown",
                ScoreValue = s.Score1,
                Comment = s.Comment,
                ScoredAt = s.ScoredAt,
                TeamAverageScore = teamAverage
            }).ToList();
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

        //public async Task<FinalScoreResponseDto> FinalScoringAsync(FinalScoreRequestDto request, int userId)
        //{
        //    // ------------------------------------------------
        //    // 1. Validate Submission
        //    // ------------------------------------------------
        //    var submission = await _uow.Submissions.FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId);
        //    if (submission == null)
        //        throw new Exception("Submission not found");

        //    var phase = await _uow.HackathonPhases.FirstOrDefaultAsync(x => x.PhaseId == submission.PhaseId);
        //    if (phase == null)
        //        throw new Exception("Phase not found");

        //    int hackathonId = phase.HackathonId;

        //    // Lấy Final Phase
        //    var allPhases = await _uow.HackathonPhases.GetAllAsync(x => x.HackathonId == hackathonId);
        //    var finalPhase = allPhases.OrderByDescending(x => x.EndDate).First();

        //    if (submission.PhaseId != finalPhase.PhaseId)
        //        throw new Exception("Submission is not from Final Round");


        //    // ------------------------------------------------
        //    // 2. Lấy JudgeId từ token thông qua JudgeAssignments
        //    // ------------------------------------------------
        //    var judgeAssign = await _uow.JudgeAssignments.FirstOrDefaultAsync(x => x.JudgeId == userId);
        //    if (judgeAssign == null)
        //        throw new Exception("Judge not found");

        //    int judgeId = userId;


        //    // ------------------------------------------------
        //    // 3. Kiểm tra Judge có được quyền chấm Team này không (track)
        //    // ------------------------------------------------
        //    var teamTrack = await _uow.TeamTrackSelections.FirstOrDefaultAsync(x => x.TeamId == submission.TeamId);
        //    if (teamTrack == null)
        //        throw new Exception("Team track not found");


        //    bool canJudge = await _uow.JudgeAssignments.ExistsAsync(x =>
        //        x.JudgeId == userId &&
        //        x.TrackId == teamTrack.TrackId &&
        //        x.HackathonId == hackathonId
        //    );

        //    if (!canJudge)
        //        throw new Exception("Judge is not assigned to this track");


        //    // ------------------------------------------------
        //    // 4. Xóa toàn bộ Score cũ của Judge cho Submission này
        //    // ------------------------------------------------
        //    var oldScores = await _uow.Scores.GetAllAsync(x =>
        //        x.SubmissionId == request.SubmissionId &&
        //        x.JudgeId == judgeId
        //    );

        //    foreach (var s in oldScores)
        //        _uow.Scores.Remove(s);


        //    // ------------------------------------------------
        //    // 5. Lưu Score mới theo từng criterion
        //    // ------------------------------------------------
        //    foreach (var item in request.CriteriaScores)
        //    {
        //        var newScore = new Score
        //        {
        //            SubmissionId = request.SubmissionId,
        //            JudgeId = judgeId,
        //            CriteriaId = item.CriterionId,
        //            Score1 = item.Score,
        //            Comment = item.Comment,
        //            ScoredAt = DateTime.UtcNow
        //        };

        //        await _uow.Scores.AddAsync(newScore);
        //    }

        //    await _uow.SaveAsync();


        //    // ------------------------------------------------
        //    // 6. Tính Total Score = SUM(criteria score)
        //    // ------------------------------------------------
        //    var allScores = await _uow.Scores.GetAllAsync(x =>
        //        x.SubmissionId == request.SubmissionId
        //    );

        //    decimal totalScore = allScores.Sum(x => x.Score1);


        //    // ------------------------------------------------
        //    // 7. Save Ranking
        //    // ------------------------------------------------
        //    var ranking = await _uow.Rankings.FirstOrDefaultAsync(x =>
        //        x.TeamId == submission.TeamId &&
        //        x.HackathonId == hackathonId
        //    );

        //    if (ranking == null)
        //    {
        //        ranking = new Ranking
        //        {
        //            TeamId = submission.TeamId,
        //            HackathonId = hackathonId,
        //            TotalScore = totalScore,
        //              UpdatedAt = DateTime.UtcNow
        //        };

        //        await _uow.Rankings.AddAsync(ranking);
        //    }
        //    else
        //    {
        //        ranking.TotalScore = totalScore;
        //        ranking.UpdatedAt = DateTime.UtcNow;
        //        _uow.Rankings.Update(ranking);
        //    }

        //    await _uow.SaveAsync();


        //    // ------------------------------------------------
        //    // 8. Recalculate Rank
        //    // ------------------------------------------------
        //    var allRankings = (await _uow.Rankings.GetAllAsync(
        //        x => x.HackathonId == hackathonId,
        //        orderBy: q => q.OrderByDescending(r => r.TotalScore)
        //    )).ToList();

        //    int rank = 1;
        //    foreach (var r in allRankings)
        //    {
        //        r.Rank = rank++;
        //        _uow.Rankings.Update(r);
        //    }

        //    await _uow.SaveAsync();


        //    // ------------------------------------------------
        //    // 9. RETURN DTO
        //    // ------------------------------------------------
        //    return new FinalScoreResponseDto
        //    {
        //        SubmissionId = submission.SubmissionId,
        //        TeamId = submission.TeamId,
        //        JudgeId = judgeId,
        //        TotalScore = totalScore,
        //        Rank = ranking.Rank
        //    };
        //}

        public async Task<FinalScoreResponseDto> FinalScoringAsync(FinalScoreRequestDto request, int userId)
        {
            // ------------------------------------------------
            // 1. Validate Submission
            // ------------------------------------------------
            var submission = await _uow.Submissions.GetByIdAsync(request.SubmissionId);
            if (submission == null)
                throw new Exception("Submission not found");

            var phase = await _uow.HackathonPhases.GetByIdAsync(submission.PhaseId);
            if (phase == null)
                throw new Exception("Phase not found");

            int hackathonId = phase.HackathonId;

            // Lấy Final Phase
            var allPhases = await _uow.HackathonPhases.GetAllAsync(x => x.HackathonId == hackathonId);
            var finalPhase = allPhases.OrderByDescending(x => x.EndDate).FirstOrDefault();
            if (submission.PhaseId != finalPhase.PhaseId)
                throw new Exception("Submission is not from Final Round");

            // ------------------------------------------------
            // 2. Kiểm tra Judge có được phân công
            // ------------------------------------------------
            var judgeAssign = await _uow.JudgeAssignments.FirstOrDefaultAsync(x => x.JudgeId == userId);
            if (judgeAssign == null)
                throw new Exception("Judge not found");

            int judgeId = userId;

            // ------------------------------------------------
            // 3. Kiểm tra Judge được phép chấm track của Team
            // ------------------------------------------------
            var teamTrack = await _uow.TeamTrackSelections.FirstOrDefaultAsync(x => x.TeamId == submission.TeamId);
            if (teamTrack == null)
                throw new Exception("Team track not found");

            bool canJudge = await _uow.JudgeAssignments.ExistsAsync(x =>
                x.JudgeId == userId &&
                (x.TrackId == null || x.TrackId == teamTrack.TrackId) &&
                x.HackathonId == hackathonId
            );
            if (!canJudge)
                throw new Exception("Judge is not assigned to this track");

            // ------------------------------------------------
            // 4. Validate Scores
            // ------------------------------------------------
            foreach (var item in request.CriteriaScores)
            {
                var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                    c.CriteriaId == item.CriterionId &&
                    (c.TrackId == null || c.TrackId == teamTrack.TrackId));

                if (criterion == null)
                    throw new Exception($"Invalid criterion {item.CriterionId} for this submission.");

                if (item.Score > criterion.Weight || item.Score < 0)
                    throw new Exception($"Score for criterion {item.CriterionId} must be between 0 and {criterion.Weight}.");
            }

            // ------------------------------------------------
            // 5. Xóa Score cũ của Judge cho Submission này
            // ------------------------------------------------
            var oldScores = await _uow.Scores.GetAllAsync(x =>
                x.SubmissionId == request.SubmissionId &&
                x.JudgeId == judgeId
            );
            foreach (var s in oldScores)
                _uow.Scores.Remove(s);

            // ------------------------------------------------
            // 6. Lưu Score mới
            // ------------------------------------------------
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
                await _uow.Scores.AddAsync(score);
            }
            await _uow.SaveAsync();

            // ------------------------------------------------
            // 7. Tính TotalScore trung bình tất cả Judge
            // ------------------------------------------------
            var allScores = await _uow.Scores.GetAllAsync(x => x.SubmissionId == submission.SubmissionId);
            decimal totalScore = allScores
                .GroupBy(s => s.JudgeId)
                .Select(g => g.Sum(s => s.Score1))
                .Average(); // trung bình tổng điểm mỗi judge

            // ------------------------------------------------
            // 8. Lưu Ranking
            // ------------------------------------------------
            var ranking = await _uow.Rankings.FirstOrDefaultAsync(x =>
                x.TeamId == submission.TeamId &&
                x.HackathonId == hackathonId
            );

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

            // ------------------------------------------------
            // 9. Recalculate Rank
            // ------------------------------------------------
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

            // ------------------------------------------------
            // 10. Map Result với AutoMapper
            // ------------------------------------------------
            var result = _mapper.Map<FinalScoreResponseDto>(ranking);
            result.SubmissionId = submission.SubmissionId;
            result.JudgeId = judgeId;

            return result;
        }

        public async Task<FinalScoreResponseDto> UpdateFinalScoreAsync(int userId, FinalScoreRequestDto request)
        {
            if (request.CriteriaScores == null || !request.CriteriaScores.Any())
                throw new Exception("No scores provided.");

            // ------------------------------------------------
            // 1. Validate Submission
            // ------------------------------------------------
            var submission = await _uow.Submissions.GetByIdAsync(request.SubmissionId);
            if (submission == null)
                throw new Exception("Submission not found");

            var phase = await _uow.HackathonPhases.GetByIdAsync(submission.PhaseId);
            if (phase == null)
                throw new Exception("Phase not found");

            int hackathonId = phase.HackathonId;

            // Lấy Final Phase
            var allPhases = await _uow.HackathonPhases.GetAllAsync(x => x.HackathonId == hackathonId);
            var finalPhase = allPhases.OrderByDescending(x => x.EndDate).FirstOrDefault();
            if (submission.PhaseId != finalPhase.PhaseId)
                throw new Exception("Submission is not from Final Round");

            // ------------------------------------------------
            // 2. Kiểm tra Judge được phân công
            // ------------------------------------------------
            var judgeAssign = await _uow.JudgeAssignments.FirstOrDefaultAsync(x => x.JudgeId == userId);
            if (judgeAssign == null)
                throw new Exception("Judge not found");

            int judgeId = userId;

            // ------------------------------------------------
            // 3. Kiểm tra Judge được phép chấm track của Team
            // ------------------------------------------------
            var teamTrack = await _uow.TeamTrackSelections.FirstOrDefaultAsync(x => x.TeamId == submission.TeamId);
            if (teamTrack == null)
                throw new Exception("Team track not found");

            bool canJudge = await _uow.JudgeAssignments.ExistsAsync(x =>
                x.JudgeId == userId &&
                (x.TrackId == null || x.TrackId == teamTrack.TrackId) &&
                x.HackathonId == hackathonId
            );
            if (!canJudge)
                throw new Exception("Judge is not assigned to this track");

            // ------------------------------------------------
            // 4. Validate Scores
            // ------------------------------------------------
            foreach (var item in request.CriteriaScores)
            {
                var criterion = await _uow.Criteria.FirstOrDefaultAsync(c =>
                    c.CriteriaId == item.CriterionId &&
                    (c.TrackId == null || c.TrackId == teamTrack.TrackId));

                if (criterion == null)
                    throw new Exception($"Invalid criterion {item.CriterionId} for this submission.");

                if (item.Score > criterion.Weight || item.Score < 0)
                    throw new Exception($"Score for criterion {item.CriterionId} must be between 0 and {criterion.Weight}.");
            }

            // ------------------------------------------------
            // 5. Xóa Score cũ của Judge cho Submission này
            // ------------------------------------------------
            var oldScores = await _uow.Scores.GetAllAsync(x =>
                x.SubmissionId == request.SubmissionId &&
                x.JudgeId == judgeId
            );
            foreach (var s in oldScores)
                _uow.Scores.Remove(s);

            // ------------------------------------------------
            // 6. Lưu Score mới với comment
            // ------------------------------------------------
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
                await _uow.Scores.AddAsync(score);
            }

            await _uow.SaveAsync();

            // ------------------------------------------------
            // 7. Tính TotalScore trung bình tất cả Judge
            // ------------------------------------------------
            var allScores = await _uow.Scores.GetAllAsync(x => x.SubmissionId == submission.SubmissionId);
            decimal totalScore = allScores
                .GroupBy(s => s.JudgeId)
                .Select(g => g.Sum(s => s.Score1))
                .Average();

            // ------------------------------------------------
            // 8. Lưu Ranking
            // ------------------------------------------------
            var ranking = await _uow.Rankings.FirstOrDefaultAsync(x =>
                x.TeamId == submission.TeamId &&
                x.HackathonId == hackathonId
            );

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

            // ------------------------------------------------
            // 9. Recalculate Rank
            // ------------------------------------------------
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

            // ------------------------------------------------
            // 10. Map Result với AutoMapper
            // ------------------------------------------------
            var result = _mapper.Map<FinalScoreResponseDto>(ranking);
            result.SubmissionId = submission.SubmissionId;
            result.JudgeId = judgeId;

            return result;
        }

    }
}