using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace n_ate.Swagger.Versioning
{
    /// <summary>
    ///
    /// </summary>
    public class FreshVersionConstraint : IRouteConstraint, IParameterPolicy
    {
        public const string TYPE_KEY = "FreshApiVersion";

        public static void ConfigureService(IServiceCollection services)
        {
            services.Configure<RouteOptions>(routeOptions =>
            {
                routeOptions.ConstraintMap.Add(TYPE_KEY, typeof(FreshVersionConstraint));
            });
        }

        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (string.IsNullOrEmpty(routeKey))
            {
                return false;
            }

            if (!values.TryGetValue(routeKey, out var v))
            {
                return false;
            }
            var value = v!.ToString()!;

            if (routeDirection == RouteDirection.UrlGeneration)
            {
                return !string.IsNullOrEmpty(value);
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            var apiVersioningFeature = httpContext!.Features.Get<IApiVersioningFeature>()!;
            apiVersioningFeature.RouteParameter = routeKey;
            apiVersioningFeature.RawRequestedApiVersion = value;
            var version = FreshVersion.Get(value);

            apiVersioningFeature.RequestedApiVersion = version;

            return true;
        }
    }
}