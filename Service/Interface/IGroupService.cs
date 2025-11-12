using Common.DTOs.GroupDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IGroupService
    {
        Task<List<GroupDto>> CreateGroupsByTrackAsync(CreateGroupsRequestDto dto);
    }
}
