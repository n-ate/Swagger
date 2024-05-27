using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using n_ate.Swagger.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.RegularExpressions;

namespace n_ate.Swagger
{
    public class ApiDocumentFilter : IDocumentFilter
    {
        public static readonly Regex VersionPathParameter = new Regex(@"\{?(api-)?version(:.+)?\}?");

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var version = swaggerDoc.Info.Version;
            if (version == null) throw new ArgumentNullException(nameof(version));
            var actionLookup = context.ApiDescriptions
                .Select(d => (Key: "/" + (d.RelativePath ?? "").TrimStart('/'), Value: d.ActionDescriptor as ControllerActionDescriptor))
                .DistinctBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            UpdatePathsWithHardCodedApiVersion(swaggerDoc.Paths, version);

            if (version == HealthChecksManager.Version.ToString())
            {
                foreach (var path in HealthChecksManager.HealthPaths)
                {
                    if (swaggerDoc.Paths.ContainsKey(path.Key))
                    {
                        foreach (var operation in path.Value.Operations)
                        {
                            if (swaggerDoc.Paths[path.Key].Operations.ContainsKey(operation.Key))
                            { //theoretically this should never happen because the conflicting routes would error before we get this far..
                                throw new InvalidOperationException($"Cannot add health check route to swagger because it conflicts with an existing operation. {operation.Key} {operation.Value}");
                            }
                            else swaggerDoc.Paths[path.Key].Operations.Add(operation.Key, operation.Value);
                        }
                    }
                    else swaggerDoc.Paths.Add(path.Key, path.Value);
                }
            }

            //TrySwaggerJsonGeneration(swaggerDoc, version);
        }

        private static void TrySwaggerJsonGeneration(OpenApiDocument swaggerDoc, string version)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) // if(environment is windows)
            {
                var root = AppContext.BaseDirectory.Split(@"\bin\")[0];
                var directory = Path.Combine(root, $@"_cached\swagger\{version}\");
                Directory.CreateDirectory(directory);
                using (var fileStream = new FileStream($"{directory}swagger.json", FileMode.Create))
                using (var textWriter = new StreamWriter(fileStream))
                {
                    var jsonWriter = new OpenApiJsonWriter(textWriter);
                    swaggerDoc.SerializeAsV3(jsonWriter);
                    jsonWriter.Flush();
                }
            }
        }

        private OpenApiPathItem RemoveOperationVersionParameters(OpenApiPathItem openApiPathItem)
        {
            foreach (var operation in openApiPathItem.Operations.Values)
            {
                var parameters = operation.Parameters.ToArray();
                foreach (var parameter in parameters)
                {
                    if (VersionPathParameter.Matches(parameter.Name).Any()) operation.Parameters.Remove(parameter);
                }
            }
            return openApiPathItem;
        }

        private void UpdatePathsWithHardCodedApiVersion(OpenApiPaths paths, string version)
        {
            foreach (var pathKey in paths.Keys.ToArray())
            {
                if (VersionPathParameter.Matches(pathKey).Any())
                {
                    var value = RemoveOperationVersionParameters(paths[pathKey]);
                    paths.Remove(pathKey);
                    var modifiedPathKey = VersionPathParameter.Replace(pathKey, version, 1);
                    paths.Add(modifiedPathKey, value);
                }
            }
        }
    }
}