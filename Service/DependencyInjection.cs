using Microsoft.Extensions.DependencyInjection;
using Service.Interface;
using Service.Servicefolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddService(this IServiceCollection service)
        {
           service.AddScoped<IRoleService, RoleService>();
            return service;
        }
    }
}
