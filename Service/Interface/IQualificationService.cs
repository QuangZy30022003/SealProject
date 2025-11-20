using Common.DTOs.QualifiedFinealTeamDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IQualificationService
    {
        Task<List<QualifiedTeamDto>> GenerateQualifiedTeamsAsync(int phaseId, int quantity);
        }
}
