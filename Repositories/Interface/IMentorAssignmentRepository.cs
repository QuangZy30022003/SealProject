using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IMentorAssignmentRepository : IRepository<MentorAssignment>
    {
        Task<IEnumerable<MentorAssignment>> GetTeamsByMentorIdAsync(int mentorId);
    }
}
