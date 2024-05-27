using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using n_ate.Swagger.UI;
using Swashbuckle.AspNetCore.Swagger;

namespace n_ate.Swagger
{
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Register the Swagger and SwaggerUI middleware with defaults.
        /// </summary>
        public static IApplicationBuilder UseFreshSwagger(this IApplicationBuilder app)
        {
            return app.UseFreshSwagger(NullLogger.Instance);
        }

        /// <summary>
        /// Register the Swagger and SwaggerUI middleware with defaults.
        /// </summary>
        public static IApplicationBuilder UseFreshSwagger(this IApplicationBuilder app, ILogger logger)
        {
            return app.UseFreshSwagger(logger, _ => { }, _ => { });
        }

        /// <summary>
        /// Register the Swagger and SwaggerUI middleware with defaults.
        /// </summary>
        public static IApplicationBuilder UseFreshSwagger(this IApplicationBuilder app, Action<SwaggerOptions> options, Action<FreshSwaggerUIOptions> uiOptions)
        {
            return app.UseFreshSwagger(NullLogger.Instance, options, uiOptions);
        }

        /// <summary>
        /// Register the Swagger and SwaggerUI middleware with defaults.
        /// </summary>
        public static IApplicationBuilder UseFreshSwagger(this IApplicationBuilder app, ILogger logger, Action<SwaggerOptions> options, Action<FreshSwaggerUIOptions> uiOptions)
        {
            FreshSwaggerUIOptions? freshUIOptions = null;
            return app
                .UseSwagger(o =>
                {
                    o.PreSerializeFilters.Add((swagger, httpRequest) =>
                    {
                        //var logger = new AboutControllerLogger("ROUTING");
                        logger.LogDebug("Adding servers collection to swagger.json");
                        logger.LogDebug($"Scheme: {httpRequest?.Scheme}");
                        logger.LogDebug($"Host: {httpRequest?.Host.Value}");
                        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpRequest!.Host.Value}"/*, Description = "<description>"*/ } };
                        logger.LogDebug("Added servers collection to swagger.json successfully.");
                    });
                    options.Invoke(o);
                })
                .UseSwaggerUI(o =>
                {
                    freshUIOptions = FreshSwaggerUIOptions.Init(app, o, uiOptions);
                })
                .UseEndpoints(builder =>
                {
                    freshUIOptions!.Complete(builder);
                });
        }

        /// <summary>
        /// Registers the wwwroot directory wherever it is found for static file use as the static file root.
        /// </summary>
        public static IApplicationBuilder UseWwwRootDirecortyAsStaticFilesRoot(this IApplicationBuilder app)
        {
            return app.UseWwwRootDirecortyAsStaticFilesRoot(NullLogger.Instance);
        }

        /// <summary>
        /// Registers the wwwroot directory wherever it is found for static file use as the static file root.
        /// </summary>
        public static IApplicationBuilder UseWwwRootDirecortyAsStaticFilesRoot(this IApplicationBuilder app, ILogger logger)
        {
            logger.LogDebug("Registering the wwwroot directory for static file serving.");
            var wwwRootDirectories = Directory.GetDirectories(Environment.CurrentDirectory, "wwwroot", SearchOption.AllDirectories);
            if (wwwRootDirectories.Any())
            {
                var wwwRootDirectory = wwwRootDirectories.First();
                logger.LogDebug("Found the following wwwroot directories:");
                foreach (var directory in wwwRootDirectories) logger.LogDebug(directory.PadLeft(3));
                logger.LogDebug("Using: " + wwwRootDirectory);
                app = app.UseStaticFiles(new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(wwwRootDirectory),
                    RequestPath = new PathString("")
                });
                logger.LogError("Static file registration completed successfully!");
            }
            else logger.LogError("No wwwroot directory was found. Static file registration failed!");
            return app;
        }
    }
}