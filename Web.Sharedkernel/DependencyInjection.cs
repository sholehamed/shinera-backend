using Microsoft.Extensions.DependencyInjection;

namespace Web.SharedKernel
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBaseApiServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            return services;
        }
    }
}