using Application.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Web.SharedKernel.Authorization;
using Web.SharedKernel.Util;

namespace Web.SharedKernel
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBaseApiServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<
                IAuthorizationMiddlewareResultHandler,
                StableAuthorizationMiddlewareResultHandler>();

            return services;
        }
    }
}