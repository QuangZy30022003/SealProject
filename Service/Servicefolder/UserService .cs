using AutoMapper;
using Newtonsoft.Json;
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
    public class UserService : IUserService
    {
        private readonly IUOW _uow;
        private readonly IAuditLogService _audit;
        private readonly IMapper _mapper;

        public UserService(IUOW uow, IAuditLogService audit, IMapper mapper)
        {
            _uow = uow;
            _audit = audit;
            _mapper = mapper;
        }

        //public async Task<User> UpdateAsync(int id, User updatedData, int actionUserId)
        //{
        //    var user = await _uow.Users.GetByIdAsync(id);
        //    if (user == null) throw new Exception("User not found");

        //    var oldJson = JsonConvert.SerializeObject(user);

        //    // Map entity -> entity
        //    _mapper.Map(updatedData, user);

        //    _uow.Users.Update(user);
        //    await _uow.SaveAsync();

        //    var newJson = JsonConvert.SerializeObject(user);

        //    await _audit.LogAsync(
        //        userId: actionUserId,
        //        action: "PUT",
        //        details: $"Before: {oldJson}\nAfter: {newJson}"
        //    );

        //    return user;
        //}

        //public async Task<User> CreateAsync(User newUser, int actionUserId)
        //{
        //    await _uow.Users.AddAsync(newUser);
        //    await _uow.SaveAsync();

        //    var json = JsonConvert.SerializeObject(newUser);

        //    await _audit.LogAsync(
        //        userId: actionUserId,
        //        action: "POST",
        //        details: $"Created: {json}"
        //    );

        //    return newUser;
        //}
        //public async Task DeleteAsync(int id, int actionUserId)
        //{
        //    var user = await _uow.Users.GetByIdAsync(id);
        //    if (user == null) throw new Exception("User not found");

        //    var json = JsonConvert.SerializeObject(user);

        //    _uow.Users.Remove(user);
        //    await _uow.SaveAsync();

        //    await _audit.LogAsync(
        //        userId: actionUserId,
        //        action: "DELETE",
        //        details: $"Deleted: {json}"
        //    );
        //}

    }

}
