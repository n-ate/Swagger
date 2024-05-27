using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using n_ate.Swagger.RequiresRequestType;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;

namespace n_ate.Swagger
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Swagger generation and API explorer endpoints with defaults.
        /// </summary>
        public static IServiceCollection AddFreshSwaggerGen(this IServiceCollection services)
        {
            return services.AddFreshSwaggerGen(_ => { });
        }

        /// <summary>
        /// Adds Swagger generation and API explorer endpoints with defaults.
        /// </summary>
        public static IServiceCollection AddFreshSwaggerGen(this IServiceCollection services, Action<SwaggerGenOptions> options)
        {
            var serviceNames = string.Join('\n', services.Select(d => d.ServiceType.AssemblyQualifiedName));
            return services
                .AddEndpointsApiExplorer()
                //.AddSingleton<IApiDescriptionGroupCollectionProvider, FreshApiDescriptionGroupCollectionProvider>()//Replaces default
                //.AddSingleton<ApiDescriptionGroupCollectionProvider, FreshApiDescriptionGroupCollectionProvider>()//Replaces default
                .AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptionsGenerator>()
                .AddSwaggerGen(c =>
                {
                    var methodsOrder = new[] { "post", "put", "patch", "delete", "options", "trace", "get" };
                    c.CustomSchemaIds(x => x.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName ?? x.Name);
                    c.OrderActionsBy(d => $"{d.ActionDescriptor.RouteValues["controller"]}_{Array.IndexOf(methodsOrder, d.HttpMethod!.ToLower())}_{d.RelativePath!.Replace('[', '~').Replace(']', '~')}");
                    options.Invoke(c);
                });
        }

        /// <summary>
        /// Makes it possible to configure the behavior of the RequiresRequestTypeAttribute.
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddRequiresRequestTypeAttributeConfiguration(this IServiceCollection services, Action<RequiresRequestTypeAttributeBuilder> options)
        {
            options.Invoke(RequiresRequestTypeAttributeBuilder.Instance);
            return services;
        }
    }
}