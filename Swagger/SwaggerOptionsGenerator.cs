using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using n_ate.Essentials;
using n_ate.Swagger.Attributes;
using n_ate.Swagger.Versioning;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace n_ate.Swagger
{
    /// <summary>Called by Startup.cs.</summary>
    public class SwaggerOptionsGenerator : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _apiVersionDescriptionProvider;
        private readonly OpenApiSecurityRequirement _securityRequirement;
        private readonly OpenApiSecurityScheme _securityScheme;

        /// <summary>Called by Startup.cs.</summary>
        public SwaggerOptionsGenerator(IApiVersionDescriptionProvider apiVersionDescriptionProvider, OpenApiSecurityScheme securityScheme, OpenApiSecurityRequirement securityRequirement)
        {
            //var v = collectionProvider.GetValue<ActionDescriptorCollection>("_collection");
            //if (v is not null)
            //{
            //    var descriptor = new ControllerActionDescriptor() {
            //        AttributeRouteInfo = new AttributeRouteInfo { Template = "health/health" },
            //        ControllerName = "Health",
            //        DisplayName = "aggregate health",
            //        EndpointMetadata = new List<object> { new FreshApiVersionAttribute("health-v1") },
            //        RouteValues = new Dictionary<string, string?>() { { "action", "Health" }, { "controller", "Health" } }
            //    };
            //    var updatedValue = v.Items.Concat(descriptor.ToSingleItemArray()).ToList().AsReadOnly();
            //    v.SetPropertyBackingField(nameof(v.Items), updatedValue);
            //}
            //var getCompositeChangeToken = collectionProvider.GetType().GetMethod("GetCompositeChangeToken", BindingFlags.Instance | BindingFlags.NonPublic);
            //var updateCollection = collectionProvider.GetType().GetMethod("UpdateCollection", BindingFlags.Instance | BindingFlags.NonPublic);
            //Debug.Assert(getCompositeChangeToken is not null);
            //Debug.Assert(updateCollection is not null);
            //ChangeToken.OnChange(
            //    () => { return (IChangeToken)getCompositeChangeToken.Invoke(collectionProvider, null)!; },
            //    () => updateCollection.Invoke(collectionProvider, null)
            //);

            _apiVersionDescriptionProvider = apiVersionDescriptionProvider;
            _securityScheme = securityScheme;
            _securityRequirement = securityRequirement;
        }

        /// <summary>
        /// Does the version indicate that the test graph should be used?
        /// </summary>
        /// <param name="version">The version string for the api.</param>
        /// <returns>Whether the version string indicates that the test graph should be used.</returns>
        public static bool IsTestGraph(string version)
        {
            return version == "test" || version.StartsWith("test-") || version.StartsWith("test - ");
        }

        /// <summary>Called by Startup.cs.</summary>
        public void Configure(SwaggerGenOptions options)
        {
            var entryAssemblyName = Assembly.GetEntryAssembly()!.GetName().Name ?? string.Empty;
            var xmlPath = GetXmlDocumentationPath();

            options.DocumentFilter<ApiDocumentFilter>();
            options.IncludeXmlComments(xmlPath);
            options.OperationFilter<ApiOperationFilter>();
            options.SchemaFilter<ApiSchemaFilter>();
            options.AddSecurityDefinition(_securityScheme.Name, _securityScheme);
            options.AddSecurityRequirement(_securityRequirement);
            //options.AddServer(new OpenApiServer() { Url = "" });

            foreach (var description in _apiVersionDescriptionProvider.GetVersionDescriptions())
            {
                var dockerImageVersion = Environment.GetEnvironmentVariable("IMAGE_VERSION");
                var desc = (dockerImageVersion == null ? " Image: " : $"Image: v{dockerImageVersion}") + (description.IsDeprecated ? " (deprecated)" : "");
                if (IsTestGraph(description.GroupName)) desc += $" (connects to test sandbox graph)"; //TODO: remove this check from this package and replace with a startup.cs or program.cs hook

                if (description.ApiVersion is FreshVersion apiVersion)
                {
                    var version = apiVersion.ToString();
                    options.SwaggerDoc(
                        version,
                        new OpenApiInfo()
                        {
                            Title = entryAssemblyName.CamelCaseToFriendly(),
                            Version = version,
                            Description = desc
                        }
                    );
                }
                else if (description.ApiVersion is not null)
                {
                    var version = description.ApiVersion.ToString();
                    options.SwaggerDoc(
                        version,
                        new OpenApiInfo()
                        {
                            Title = entryAssemblyName.CamelCaseToFriendly(),
                            Version = version,
                            Description = desc
                        }
                    );
                }
                else throw new ArgumentException($"Each controller must use {nameof(FreshApiVersionAttribute)}. If you are using {nameof(ApiVersionAttribute)}, use {nameof(FreshApiVersionAttribute)} instead. If you find it necessary to support legacy versioning hard-code the version in the route and add the same version on the {nameof(FreshApiVersionAttribute)}.");
            }
        }

        private static string GetXmlDocumentationPath()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var assembliesToSearch = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !(a.GetName().Name ?? "").StartsWith("Microsoft") && !(a.GetName().Name ?? "").StartsWith("System")) //filter out obvious Microsoft assemblies
                .Where(a => a != Assembly.GetExecutingAssembly()) //filter out this assembly
                .Where(a => a != entryAssembly) //filter out entry assembly
                .ToList();
            if (entryAssembly is not null) assembliesToSearch.Insert(0, entryAssembly); //add entry assembly first
            foreach (var assembly in assembliesToSearch)
            {
                var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(path)) return path;
            }
            var generalSetupLink = $"{Settings.RepositoryUrl}?path=/about/general-setup.md&_a=preview";
            var ex = new ApplicationException($"XML documentation must be turned on in the project properties. see: {generalSetupLink} ");
            ex.HelpLink = generalSetupLink;
            ex.Data.Add("help-link", generalSetupLink);
            throw ex;
        }
    }
}