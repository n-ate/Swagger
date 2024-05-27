using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace n_ate.Swagger.CommandLine
{
    internal class SwaggerJsonComposer
    {
        private const string BUILD_VERSION_DESTINATION_KEY = "Image:";
        private string? _outputDirectory;
        private string? _stage;

        public SwaggerJsonComposer()
        {
            CatalogComposer = new CatalogYamlComposer(this);
        }

        public string? BuildVersion { get; set; }
        public CatalogYamlComposer CatalogComposer { get; }

        public bool IsEnabled
        { get { return !(string.IsNullOrEmpty(OutputDirectory) || string.IsNullOrEmpty(Stage)) && (Servers?.Any() ?? false); } }

        public string? OutputDirectory
        {
            get { return _outputDirectory; }
            set { _outputDirectory = (string.IsNullOrEmpty(value) || value.EndsWith(Path.DirectorySeparatorChar)) ? value : $"{value}{Path.DirectorySeparatorChar}"; } //ensure trailing slash
        }

        public string ResolvedOutputDirectory
        {
            get
            {
                return IsEnabled ? Path.Combine(OutputDirectory!, $@"swagger{Path.DirectorySeparatorChar}{Stage}{Path.DirectorySeparatorChar}") : throw new InvalidOperationException($"{nameof(ResolvedOutputDirectory)} cannot be called when {nameof(IsEnabled)} is false.");
            }
        }

        public IList<OpenApiServer>? Servers { get; set; }

        public string? Stage
        {
            get { return _stage; }
            set { _stage = value; }
        }

        public void AddServer(OpenApiServer server)
        {
            if (Servers == null) Servers = new List<OpenApiServer>();
            Servers!.Add(server);
        }

        internal OpenApiDocument CreateJsonDescriptionFile(ISwaggerProvider swaggerProvider, ApiDescriptionGroup group)
        {
            var version = group.GroupName!;
            var swaggerDocument = swaggerProvider.GetSwagger(version);
            if (!string.IsNullOrWhiteSpace(BuildVersion))
            {
                Console.WriteLine($"   Injecting build version:             v{BuildVersion}");
                var index = swaggerDocument.Info.Description.IndexOf(BUILD_VERSION_DESTINATION_KEY);
                if (index != -1)
                {
                    index += BUILD_VERSION_DESTINATION_KEY.Length;
                    swaggerDocument.Info.Description = $"{swaggerDocument.Info.Description.Substring(0, index)} v{BuildVersion} {swaggerDocument.Info.Description.Substring(index)}";
                }
                else swaggerDocument.Info.Description += $" Image: v{BuildVersion}";
            }
            return CreateJsonDescriptionFile(swaggerDocument);
        }

        internal OpenApiDocument CreateJsonDescriptionFile(OpenApiDocument swaggerDocument)
        {
            var version = swaggerDocument.Info.Version;
            Console.WriteLine($"   Creating Swagger Json file:          {version}.json");
            if (!Directory.Exists(ResolvedOutputDirectory)) Directory.CreateDirectory(ResolvedOutputDirectory);
            using (var fileStream = new FileStream($"{ResolvedOutputDirectory}{version}.json", FileMode.Create))
            using (var textWriter = new StreamWriter(fileStream))
            {
                var jsonWriter = new OpenApiJsonWriter(textWriter);
                swaggerDocument.SerializeAsV3(jsonWriter);
                jsonWriter.Flush();
            }
            return swaggerDocument;
        }
    }
}