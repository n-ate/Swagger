using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using n_ate.Swagger.Versioning;
using System.Net.Mime;

namespace n_ate.Swagger.HealthChecks
{
    public static class HealthChecksManager
    {
        public const string OPERATION_TAG = "Health";
        public static readonly ApiVersion Version = FreshVersion.Get("health-v1");
        public static ApiVersionDescription Description = new ApiVersionDescription(Version, Version.ToString(), false);
        public static Dictionary<string, OpenApiPathItem> HealthPaths { get; private set; } = new Dictionary<string, OpenApiPathItem>();
        public static bool IsAdded { get; private set; }
        public static bool IsMapped { get; private set; }

        internal static void HealthCheckEndpointsAdded() => IsAdded = true;

        internal static void MapHealthChecks(IEndpointRouteBuilder builder)
        {
            if (!IsAdded) throw new InvalidOperationException($"Health checks must be added by calling services.{nameof(HealthCheckExtensions.AddFreshHealthCheckEndpoints)}() in Program.cs or Startup.cs.");

            builder.MapHealthChecks("/health", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("live") && check.Tags.Contains("startup") && check.Tags.Contains("ready"),
            });
            var path = new OpenApiPathItem();
            path.Operations.Add(OperationType.Get, BuildParameterlessPlainTextResponseOperation(OPERATION_TAG, "HealthGet", "Gets the overall health of the application.", "Healthy"));
            HealthPaths.Add("/health", path);

            builder.MapHealthChecks("/health/startup", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("startup"),
            });
            path = new OpenApiPathItem();
            path.Operations.Add(OperationType.Get, BuildParameterlessPlainTextResponseOperation(OPERATION_TAG, "HealthGetStarted", "Gets whether the application is started.", "Healthy"));
            HealthPaths.Add("/health/startup", path);

            builder.MapHealthChecks("/health/live", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("live"),
            });
            path = new OpenApiPathItem();
            path.Operations.Add(OperationType.Get, BuildParameterlessPlainTextResponseOperation(OPERATION_TAG, "HealthGetLive", "Gets whether the application is live.", "Healthy"));
            HealthPaths.Add("/health/live", path);

            builder.MapHealthChecks("/health/ready", new HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("ready"),
            });
            path = new OpenApiPathItem();
            path.Operations.Add(OperationType.Get, BuildParameterlessPlainTextResponseOperation(OPERATION_TAG, "HealthGetReady", "Gets whether the application is ready.", "Healthy"));
            HealthPaths.Add("/health/ready", path);

            IsMapped = true;
        }

        private static OpenApiOperation BuildParameterlessPlainTextResponseOperation(string tag, string id, string description, string exampleSuccessResponse)
        {
            var responseStatusCodes = new[] { "200" };
            var result = new OpenApiOperation()
            {
                OperationId = id,
                Description = description, //this is displayed inside the operation pane
                //Security
                //Servers
                Summary = description //this is displayed on the operation bar
            };
            var response = new OpenApiResponse();
            response.Description = "Success";

            var mediaType = new OpenApiMediaType();
            var schema = new OpenApiSchema();
            mediaType.Schema = schema;

            schema.Type = "string";
            schema.Example = new OpenApiString(exampleSuccessResponse);
            //schema.OneOf.Add(schema);//can't add self, breaks generation due to circular..

            response.Content[MediaTypeNames.Text.Plain] = mediaType;
            foreach (var code in responseStatusCodes) result.Responses[code] = response;
            result.Tags.Add(new OpenApiTag() { Name = tag });
            return result;
        }
    }
}