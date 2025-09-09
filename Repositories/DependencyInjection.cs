using Microsoft.Extensions.DependencyInjection;
using Repositories.Interface;
using Repositories.Repos;
using Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepository(this IServiceCollection service)
        {
            service.AddScoped<IRoleRepository, RoleRepository>();
            service.AddScoped<IUOW, UOW>();
            service.AddScoped<IAuthRepository, AuthRepository>();
            return service;
        }
    }
}
