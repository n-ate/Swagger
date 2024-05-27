using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using n_ate.Essentials;
using n_ate.Swagger.Attributes;
using n_ate.Swagger.HealthChecks;
using n_ate.Swagger.Versioning;
using System.Reflection;

namespace n_ate.Swagger
{
    public static class VersioningExtensions
    {
        /// <summary>
        /// Adds service API versioning and API explorer that is API version aware to the specified services collection.
        /// </summary>
        public static IServiceCollection AddFreshApiVersioning(this IServiceCollection services, Action<FreshApiVersioningOptions> versionOptions, Action<ApiExplorerOptions> explorerOptions)
        {
            var controllers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try
                {
                    return a.GetTypesWithAttribute<ApiControllerAttribute>();
                }
                catch
                {
                    return new Type[0];
                }
            });
            var missingApiAttribute = controllers.Where(c => !c.GetCustomAttributes<FreshApiVersionAttribute>().Any());
            if (missingApiAttribute.Any())
            {
                throw new InvalidOperationException($"All API controllers must implement {nameof(FreshApiVersionAttribute)}. Invalid controllers: {string.Join(", ", missingApiAttribute.Select(a => a.Name))}");
            }

            FreshVersionConstraint.ConfigureService(services);
            return services
                .AddApiVersioning(options =>
                {
                    var freshApiVersioningOptions = FreshApiVersioningOptions.Init(options);
                    versionOptions.Invoke(freshApiVersioningOptions);
                })
                .AddVersionedApiExplorer(options =>
                {
                    options.SubstituteApiVersionInUrl = true;
                    explorerOptions.Invoke(options);
                });
        }

        /// <summary>
        /// Adds service API versioning and API explorer that is API version aware to the specified services collection.
        /// </summary>
        public static IServiceCollection AddFreshApiVersioning(this IServiceCollection services)
        {
            return services.AddFreshApiVersioning(_ => { }, _ => { });
        }

        /// <summary>
        /// Retrieves the <see cref="FreshVersion"/> from route path.
        /// </summary>
        /// <returns>The <see cref="FreshVersion"/> for the HTTP request to the controller.</returns>
        /// <exception cref="ArgumentException">Thrown when no route segment key of "version" or "apiVersion" is found.</exception>
        public static FreshVersion GetFreshVersion(this ControllerBase controller)
        {
            var versionMatchingRouteSegments = controller.RouteData.Values.Keys.Where(k => ApiDocumentFilter.VersionPathParameter.IsMatch(k)).ToArray();
            if (versionMatchingRouteSegments.Length == 1)
            {
                var apiVersionKey = versionMatchingRouteSegments[0];
                var version = controller.RouteData.Values[apiVersionKey]!.ToString()!;
                return FreshVersion.Get(version);
            }
            throw new ArgumentException("Expected 1 and only 1 route data matching 'version' or 'apiVersion'. Found these route data keys: " + string.Join(", ", versionMatchingRouteSegments));
        }

        /// <summary>
        /// This returns all natural version descriptions with shimmed version descriptions provided by the n_ate.Swagger package.
        /// </summary>
        public static IReadOnlyList<ApiVersionDescription> GetVersionDescriptions(this IApiVersionDescriptionProvider provider)
        {
            var result = provider.ApiVersionDescriptions.ToList();
            if (HealthChecksManager.IsAdded) result.Add(HealthChecksManager.Description);
            return result.AsReadOnly();
        }
    }
}