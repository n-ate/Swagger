using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using n_ate.Essentials;
using n_ate.Swagger.Versioning;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace n_ate.Swagger.CommandLine
{
    internal static class Commands
    {
        private const string AGGREGATE_VERSION_NAME = "__aggregate";

        internal static void ExecuteHelpCommand()
        {
            Console.WriteLine(Help.General);
        }

        internal static void ExecuteSwaggerGenerateCommand(IServiceProvider services, string[] args)
        {
            Dictionary<string, string?> arguments = ExtractSwaggerCommandArguments(args);

            var swaggerComposer = new SwaggerJsonComposer();

            if (arguments.ContainsKey("help"))
            {
                Console.WriteLine(Help.SwaggerGenerate);
            }
            else
            {
                //validate arguments//
                if (arguments.ContainsKey("build-version")) swaggerComposer.BuildVersion = arguments["build-version"];
                if (arguments.ContainsKey("catalog-info-dir"))
                {
                    swaggerComposer.CatalogComposer.Directory = arguments["catalog-info-dir"];
                    if (!File.Exists(swaggerComposer.CatalogComposer.CatalogInfoPath)) MakeArgumentError("catalog-info-dir", $"Catalog directory must contain the {CatalogYamlComposer.CATALOG_INFO_YAML_FILENAME} file.");
                    if (!File.Exists(swaggerComposer.CatalogComposer.DefinitionTemplatePath)) MakeArgumentError("catalog-info-dir", $"Catalog directory must contain the {CatalogYamlComposer.DEFINITION_TEMPLATE_FILENAME} file.");
                    //TODO:inspect catalog for proper comments..
                    //TODO:inspect template for proper placeholders..
                }
                if (arguments.ContainsKey("output-dir")) swaggerComposer.OutputDirectory = arguments["output-dir"];
                else swaggerComposer.OutputDirectory = Path.GetFullPath(".");

                if (arguments.ContainsKey("server")) swaggerComposer.AddServer(new OpenApiServer() { Url = arguments["server"]! });
                else MakeArgumentError("server", "Server argument must be passed. E.g. --server=https://api.dev.domain.com");

                if (arguments.ContainsKey("stage")) swaggerComposer.Stage = arguments["stage"];
                else MakeArgumentError("stage", "Stage  argument must be passed. E.g. --stage=DEV");

                //swagger generate from API//
                if (swaggerComposer.IsEnabled)
                {
                    Console.WriteLine();
                    Console.WriteLine("Starting Swagger JSON generation with the following parameters..");
                    Console.WriteLine($"      - build-version:  {(string.IsNullOrWhiteSpace(swaggerComposer.BuildVersion) ? "(none specified)" : swaggerComposer.OutputDirectory)}");
                    Console.WriteLine($"      - output-dir:     {swaggerComposer.OutputDirectory}");
                    Console.WriteLine($"      - server:         {swaggerComposer.Servers}");
                    Console.WriteLine($"      - stage:          {swaggerComposer.Stage}");
                    if (swaggerComposer.CatalogComposer.IsEnabled)
                    {
                        Console.WriteLine($"      - catalog-path:   {swaggerComposer.CatalogComposer.CatalogInfoPath}");
                        Console.WriteLine($"      - template-path:  {swaggerComposer.CatalogComposer.DefinitionTemplatePath}");
                    }

                    var descriptions = services.GetService<IApiDescriptionGroupCollectionProvider>()!; //TODO: replace default IAPIDescriptionGroupCollectionProvider with a custom provider that respects named versions..
                    var swaggerProvider = services.GetService<ISwaggerProvider>()!;

                    Console.WriteLine($"   Injecting server URL:                {swaggerComposer.Servers}");
                    if (swaggerProvider.TryGetValue("_options", out var _options))
                    {
                        if (_options is SwaggerGeneratorOptions generator)
                        {
                            generator.Servers.AddRange(swaggerComposer.Servers!);
                        }
                        else throw new Exception($"Could not set server URL. Must be able to get {nameof(SwaggerGeneratorOptions)}.");
                    }
                    else throw new Exception($"Could not set server URL. Must be able to get {nameof(_options)}.");

                    if (Directory.Exists(swaggerComposer.ResolvedOutputDirectory))
                    {
                        Console.WriteLine($"   Deleting existing stage directory:   {swaggerComposer.ResolvedOutputDirectory}");
                        Directory.Delete(swaggerComposer.ResolvedOutputDirectory, true); //clear old swagger.json files
                    }
                    if (descriptions.ApiDescriptionGroups.Items.Any())
                    {
                        Console.WriteLine($"   Swagger output directory:            {swaggerComposer.ResolvedOutputDirectory}");
                        foreach (var description in descriptions.ApiDescriptionGroups.Items)
                        {
                            var swaggerDoc = swaggerComposer.CreateJsonDescriptionFile(swaggerProvider, description);
                            if (swaggerComposer.CatalogComposer.IsEnabled)
                            {
                                swaggerComposer.CatalogComposer.AddCatalogInfoDefinition(swaggerDoc);
                            }
                        }
                        if (swaggerComposer.CatalogComposer.IsEnabled)
                        {
                            swaggerComposer.CatalogComposer.UpdateCatalogInfoFileWithAddedDefinitions();
                        }
                    }
                    Console.WriteLine("Finished Swagger JSON generation!");
                    Console.WriteLine();
                }
            }
        }

        internal static async Task ExecuteSwaggerMergeCommand(string[] args)
        {
            Dictionary<string, string?> arguments = ExtractSwaggerCommandArguments(args);

            var recursive = false;
            var swaggerComposer = new SwaggerJsonComposer();
            string mergeDirectory = "";
            if (arguments.ContainsKey("help"))
            {
                Console.WriteLine(Help.SwaggerMerge);
            }
            else
            {
                //validate arguments//
                if (arguments.ContainsKey("catalog-info-dir"))
                {
                    swaggerComposer.CatalogComposer.Directory = arguments["catalog-info-dir"];
                    if (!File.Exists(swaggerComposer.CatalogComposer.CatalogInfoPath)) MakeArgumentError("catalog-info-dir", $"Catalog directory must contain the {CatalogYamlComposer.CATALOG_INFO_YAML_FILENAME} file.");
                    if (!File.Exists(swaggerComposer.CatalogComposer.DefinitionTemplatePath)) MakeArgumentError("catalog-info-dir", $"Catalog directory must contain the {CatalogYamlComposer.DEFINITION_TEMPLATE_FILENAME} file.");
                    //TODO:inspect catalog for proper comments..
                    //TODO:inspect template for proper placeholders..
                }
                if (arguments.ContainsKey("merge-dir")) mergeDirectory = arguments["merge-dir"] ?? "";
                else mergeDirectory = Path.GetFullPath(".");
                if (arguments.ContainsKey("output-dir")) swaggerComposer.OutputDirectory = arguments["output-dir"];
                else swaggerComposer.OutputDirectory = Path.GetFullPath(".");
                if (arguments.ContainsKey("recursive"))
                {
                    if (string.IsNullOrEmpty(arguments["recursive"])) recursive = true;
                    else MakeArgumentError("recursive", "Recursive  argument is a switch and should have no value. E.g. --recursive");
                }
                if (arguments.ContainsKey("stage")) swaggerComposer.Stage = arguments["stage"];
                else MakeArgumentError("stage", "Stage  argument must be passed. E.g. --stage=DEV");

                //swagger merge json documents//
                var jsonFilePaths = Directory.EnumerateFiles(mergeDirectory, "*.json", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                jsonFilePaths = jsonFilePaths.Where(p => Path.GetFileNameWithoutExtension(p) != AGGREGATE_VERSION_NAME);
                Console.WriteLine();
                Console.WriteLine("Starting Swagger JSON merge with the following parameters..");
                if (!string.IsNullOrWhiteSpace(swaggerComposer.BuildVersion)) Console.WriteLine($"      - build-version:  {swaggerComposer.OutputDirectory}");
                Console.WriteLine($"      - output-dir:     {swaggerComposer.OutputDirectory}");
                if (jsonFilePaths.Any())
                {
                    Console.WriteLine($"      - merge-files:    {jsonFilePaths.FirstOrDefault()}");
                    foreach (var path in jsonFilePaths.Skip(1)) Console.WriteLine($"                        {path}");
                }
                else Console.WriteLine($"      - merge-files:    (none found)");
                Console.WriteLine($"      - recursive:      {recursive}");
                Console.WriteLine($"      - stage:          {swaggerComposer.Stage}");
                if (swaggerComposer.CatalogComposer.IsEnabled)
                {
                    Console.WriteLine($"      - catalog-path:   {swaggerComposer.CatalogComposer.CatalogInfoPath}");
                    Console.WriteLine($"      - template-path:  {swaggerComposer.CatalogComposer.DefinitionTemplatePath}");
                }
                var documents = new List<OpenApiDocument>();
                foreach (var jsonFilePath in jsonFilePaths)
                {
                    Console.WriteLine($"   Preparing Swagger JSON document:     {jsonFilePath}");
                    try
                    {
                        using (var stream = new FileStream(jsonFilePath, FileMode.Open))
                        {
                            var reader = new OpenApiStreamReader();
                            var result = await reader.ReadAsync(stream);
                            if (result.OpenApiDiagnostic.Errors.Any())
                            {
                                Console.WriteLine($"   Failed to load file:                 {jsonFilePath}");
                                Console.WriteLine($"   File may not be Swagger JSON document. Skipping file.");
                                foreach (var error in result.OpenApiDiagnostic.Errors) Console.WriteLine(error.Message);
                            }
                            else documents.Add(result.OpenApiDocument);
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"   Failed to load file:                 {jsonFilePath}");
                        Console.WriteLine($"   File may not be Swagger JSON document. Skipping file.");
                    }
                }
                var mergedDocument = new OpenApiDocument()
                {
                    Info = new OpenApiInfo() { Version = AGGREGATE_VERSION_NAME },
                    Paths = new OpenApiPaths() { },
                    Components = new OpenApiComponents() { }
                };
                var versionedDocuments = documents.Select(d => (Version: FreshVersion.Get(d.Info.Version), Document: d));
                var servers = documents.Select(d => d.Servers).DistinctBy(s => string.Join(',', s.Select(x => x.Url.Trim().ToLower())));
                if (servers.Count() > 1) throw new ArgumentException("Merged Swagger JSON files must all have the same servers information.");
                else swaggerComposer.Servers = servers.First();
                var groupedDocuments = versionedDocuments.GroupBy(x => x.Version.Name);
                foreach (var group in groupedDocuments)
                {
                    var ordered = group.OrderByDescending(x => x.Version.MajorVersion).ThenByDescending(x => x.Version.MinorVersion);
                    var primary = group.First().Document;
                    foreach (var x in ordered.Skip(1))
                    {
                        primary.AggregateFrom(x.Document);//This is untested because only 1 version exists right now..
                    }

                    foreach (var path in primary.Paths)
                    {
                        foreach (var operation in path.Value.Operations)
                        {
                            if (mergedDocument.Paths.Any(p => p.Value.Operations.Any(o => o.Value.OperationId == operation.Value.OperationId)))
                            {
                                operation.Value.OperationId += $"^{primary.Info.Version}";
                            }
                        }
                    }
                    mergedDocument.Paths = mergedDocument.Paths.AggregateFrom(primary.Paths);
                    mergedDocument.Info = mergedDocument.Info.AggregateFrom(primary.Info);//fix
                    mergedDocument.Servers = mergedDocument.Servers.AggregateFrom(primary.Servers).DistinctBy(s => s.Url).ToList();
                    mergedDocument.Components.Schemas = mergedDocument.Components.Schemas.AggregateFrom(primary.Components.Schemas);
                    mergedDocument.Components.SecuritySchemes = mergedDocument.Components.SecuritySchemes.AggregateFrom(primary.Components.SecuritySchemes);
                    mergedDocument.SecurityRequirements = mergedDocument.SecurityRequirements.AggregateFrom(primary.SecurityRequirements).DistinctBy(r => string.Join(',', r.Select(s => s.Key))).ToList();
                }

                //swagger generate from merged documents//
                if (swaggerComposer.IsEnabled)
                {
                    swaggerComposer.CreateJsonDescriptionFile(mergedDocument);
                    if (swaggerComposer.CatalogComposer.IsEnabled)
                    {
                        swaggerComposer.CatalogComposer.AddCatalogInfoDefinition(mergedDocument);
                        swaggerComposer.CatalogComposer.UpdateCatalogInfoFileWithAddedDefinitions();
                    }
                }
            }
        }

        internal static bool IsGeneralHelpCommand(string[] args) => args.FirstOrDefault()?.ToLower()?.Contains("help") ?? false;

        internal static bool IsSwaggerGenerateCommand(string[] args) => args.FirstOrDefault()?.ToLower() == "swagger-gen";

        internal static bool IsSwaggerMergeCommand(string[] args) => args.FirstOrDefault()?.ToLower() == "swagger-merge";

        internal static void MakeArgumentError(string argument, string message)
        {
            if (Environment.UserInteractive) Console.WriteLine(message);
            else throw new ArgumentNullException($"--{argument}", message);
        }

        private static Dictionary<string, string?> ExtractSwaggerCommandArguments(string[] args)
        {
            if (!IsSwaggerGenerateCommand(args) && !IsSwaggerMergeCommand(args)) throw new ArgumentException("Cannot extract Swagger command arguments from non-Swagger command.", nameof(args));
            var arguments = args
                .Skip(1)
                .Select(kv => kv.Split('=', 2, StringSplitOptions.TrimEntries))
                .ToDictionary(kv =>
                    kv.First().Substring("--".Length).ToLower(),
                    kv => kv.Length > 1 ? kv.Last().Trim('"') : null
                );
            ////mock merge//
            //arguments["catalog-info-dir"] = "C:\\Users\\nxl01\\source\\repos\\bom\\cache\\BOM.API\\Documentation";
            //arguments["merge-dir"] = "C:\\Users\\nxl01\\source\\repos\\bom\\cache\\BOM.API\\swagger\\Development";
            //arguments["output-dir"] = "C:\\Users\\nxl01\\source\\repos\\bom\\cache\\BOM.API";
            //arguments["recursive"] = "";
            //arguments["stage"] = "Development";

            ////mock generate//
            //arguments["build-version"] = "0.0.0";
            //arguments["stage"] = "Oops";
            //arguments["server"] = "https://api.dev.domain.com";
            //arguments["output-dir"] = "C:\\Users\\nxl01\\source\\repos\\bom\\cache\\BOM.API";
            //arguments["catalog-info-dir"] = "C:\\Users\\nxl01\\source\\repos\\bom\\cache\\BOM.API\\Documentation";
            return arguments;
        }
    }
}