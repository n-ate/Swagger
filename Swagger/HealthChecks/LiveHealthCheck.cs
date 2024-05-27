using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace n_ate.Swagger.HealthChecks
{
    /// <summary>
    /// Application live health check.
    /// </summary>
    public class LiveHealthCheck : IHealthCheck
    {
        /// <summary>Health check endpoint.</summary>
        public virtual async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var healthCheckResultHealthy = true;

            if (healthCheckResultHealthy)
            {
                return HealthCheckResult.Healthy("A healthy result.");
            }

            await Task.CompletedTask; //async for overridding

            return new HealthCheckResult(context.Registration.FailureStatus, "An unhealthy result.");
        }
    }
}