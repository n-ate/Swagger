using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using n_ate.Swagger.HealthChecks;

namespace n_ate.Swagger
{
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adds standard health checks.
        /// </summary>
        public static IHealthChecksBuilder AddFreshHealthCheckEndpoints(this IServiceCollection services)
        {
            return services.AddFreshHealthCheckEndpoints(c => { });
        }

        /// <summary>
        /// Adds standard health checks.
        /// </summary>
        public static IHealthChecksBuilder AddFreshHealthCheckEndpoints(this IServiceCollection services, Action<HealthCheckConfigurator> configure)
        {
            HealthChecksManager.HealthCheckEndpointsAdded();
            var builder = services.AddHealthChecks();
            configure.Invoke(new HealthCheckConfigurator(builder));
            return builder;
        }

        /// <summary>
        /// Maps startup, live, and ready health checks to the service collection.
        /// </summary>
        public static IEndpointRouteBuilder MapFreshHealthChecks(this IEndpointRouteBuilder builder)
        {
            HealthChecksManager.MapHealthChecks(builder);
            return builder;
        }
    }
}