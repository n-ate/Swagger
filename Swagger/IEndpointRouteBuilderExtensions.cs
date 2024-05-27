using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using n_ate.Swagger.Versioning;
using System.Diagnostics;

namespace n_ate.Swagger
{
    public static class IEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Returns all registered endpoints as a text document string.
        /// </summary>
        public static string GetEndpointsAuditDocument(this IEndpointRouteBuilder builder, StackFrame? frame = null)
        {
            if (frame is null) frame = new StackFrame(1);
            return string.Join("\n", builder.GetEndpointsAuditList(frame));
        }

        /// <summary>
        /// Returns all registered endpoint information as strings.
        /// </summary>
        public static string[] GetEndpointsAuditList(this IEndpointRouteBuilder builder, StackFrame? frame = null)
        {
            if (frame is null) frame = new StackFrame(1);
            var ns = frame.GetMethod()?.DeclaringType?.Namespace ?? "";
            var nsx = $" ({ns})";
            return builder.DataSources
                .SelectMany(s => s.Endpoints)
                .SelectMany(e =>
                {
                    string info;
                    if (e is RouteEndpoint re)
                    {
                        var classInfo = re.DisplayName ?? string.Empty;
                        var controllers = "Controllers.";
                        if (classInfo.StartsWith(ns)) classInfo = classInfo.Substring(ns.Length + 1);
                        if (classInfo.StartsWith(controllers)) classInfo = $"Ctr..{classInfo.Substring(controllers.Length)}";
                        if (classInfo.EndsWith(nsx)) classInfo = $"{classInfo.Substring(0, classInfo.Length - nsx.Length)}()";
                        info = $"{(re.RoutePattern.RawText ?? string.Empty).Replace(FreshVersion.ROUTE_KEY, "{api-ver..}"),-82} {classInfo}";
                    }
                    else info = e.DisplayName ?? string.Empty;
                    var verbs = (e.Metadata.FirstOrDefault(m => m is HttpMethodMetadata) as HttpMethodMetadata)?.HttpMethods?.ToArray() ?? new string[0];
                    return verbs.Select(v => $"{v,7} {info}");
                })
                .ToArray();
        }

        /// <summary>
        /// Registers a route that lists all registered routes.
        /// </summary>
        /// <param name="route">The path for the audit endpoint. E.g. "/audit/routes"</param>
        public static IEndpointRouteBuilder MapAuditEndpoint(this IEndpointRouteBuilder builder, string route = "/audit/routes", StackFrame? frame = null)
        {
            if (frame is null) frame = new StackFrame(1);
            builder.MapGet(route, () => builder.GetEndpointsAuditDocument(frame));
            return builder;
        }
    }
}