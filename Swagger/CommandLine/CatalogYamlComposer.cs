using Microsoft.OpenApi.Models;
using System.Text.RegularExpressions;

namespace n_ate.Swagger.CommandLine
{
    internal class CatalogYamlComposer
    {
        public const string CATALOG_INFO_YAML_FILENAME = "catalog-info.yaml";
        public const string DEFINITION_TEMPLATE_FILENAME = "catalog-info.definition.template.yaml";
        private string? _directory;

        internal CatalogYamlComposer(SwaggerJsonComposer swaggerComposer)
        {
            SwaggerComposer = swaggerComposer;
        }

        public string CatalogInfoContent
        {
            get
            {
                return IsEnabled ? File.ReadAllText(CatalogInfoPath) : throw new InvalidOperationException($"{nameof(CatalogInfoContent)} cannot be called when {nameof(IsEnabled)} is false.");
            }
        }

        public List<string> CatalogInfoDefintions { get; } = new List<string>();

        public string CatalogInfoPath
        {
            get
            {
                return IsEnabled ? Path.Combine(Directory!, CATALOG_INFO_YAML_FILENAME) : throw new InvalidOperationException($"{nameof(CatalogInfoPath)} cannot be called when {nameof(IsEnabled)} is false.");
            }
        }

        public string DefinitionTemplateContent
        {
            get
            {
                return IsEnabled ? File.ReadAllText(DefinitionTemplatePath) : throw new InvalidOperationException($"{nameof(DefinitionTemplateContent)} cannot be called when {nameof(IsEnabled)} is false.");
            }
        }

        public string DefinitionTemplatePath
        {
            get
            {
                return IsEnabled ? Path.Combine(Directory!, DEFINITION_TEMPLATE_FILENAME) : throw new InvalidOperationException($"{nameof(DefinitionTemplatePath)} cannot be called when {nameof(IsEnabled)} is false.");
            }
        }

        public string? Directory
        {
            get { return _directory; }
            set { _directory = (string.IsNullOrEmpty(value) || value.EndsWith(Path.DirectorySeparatorChar)) ? value : $"{value}{Path.DirectorySeparatorChar}"; } //ensure trailing slash
        }

        public bool IsEnabled
        { get { return !string.IsNullOrEmpty(Directory); } }

        public SwaggerJsonComposer SwaggerComposer { get; }
        private Regex TemplatePlaceholderRegex { get; } = new Regex("\\{(?<key>[a-z-]+)\\:?(?<modifier>[a-z-]*)\\}");

        public void AddCatalogInfoDefinition(OpenApiDocument swaggerDocument)
        {
            var version = swaggerDocument.Info.Version;
            var servers = SwaggerComposer.Servers!;
            var stage = SwaggerComposer.Stage!;
            var description = swaggerDocument.Info.Description;
            var title = swaggerDocument.Info.Title;

            Console.WriteLine($"   Creating Catalog Info Definition.");

            var evaluator = new MatchEvaluator(
                m =>
                {
                    var result = m.Value;
                    var key = m.Groups["key"];
                    var modifier = m.Groups["modifier"];
                    if (key.Success)
                    {
                        switch (key.Value)
                        {
                            case "definition-description": result = description; break;
                            case "definition-servers": result = string.Join("; ", servers.Select(s => s.Url)); break;
                            case "definition-stage": result = stage; break;
                            case "definition-title": result = title; break;
                            case "definition-version": result = version; break;
                            default: throw new ArgumentException($@"Unrecognized placeholder key ""{key.Value}"" in ""{m.Value}"" template placeholder. Please ensure the spelling is correct.");
                        }
                        if (modifier.Success && !string.IsNullOrEmpty(modifier.Value))
                        {
                            switch (modifier.Value)
                            {
                                case "lower": result = result.ToLower(); break;
                                case "upper": result = result.ToUpper(); break;
                                case "no-space":
                                case "no-spaces": result = result.Replace(" ", ""); break;
                                default: throw new ArgumentException($@"Unrecognized placeholder modifier ""{modifier.Value}"" in ""{m.Value}"" template placeholder. Please ensure the spelling is correct.");
                            }
                        }
                    }
                    return result;
                }
            );
            var catalogInfoDefinition = TemplatePlaceholderRegex.Replace(DefinitionTemplateContent, evaluator);
            CatalogInfoDefintions.Add(catalogInfoDefinition);
        }

        internal void UpdateCatalogInfoFileWithAddedDefinitions()
        {
            Console.WriteLine("Starting catalog-info.yaml file update..");
            var beginRegex = new Regex($"#stage-begin:{SwaggerComposer.Stage}.*");
            var endRegex = new Regex($"#stage-end:{SwaggerComposer.Stage}.*");
            var beginMatches = beginRegex.Matches(CatalogInfoContent);
            var endMatches = endRegex.Matches(CatalogInfoContent);
            if (beginMatches.Count == 0) throw new ArgumentException($@"catalog-info must have a beginning comment token for the stage ""{SwaggerComposer.Stage}"". e.g. #stage-begin:{SwaggerComposer.Stage}");
            if (endMatches.Count == 0) throw new ArgumentException($@"catalog-info must have an ending comment token for the stage ""{SwaggerComposer.Stage}"". e.g. #stage-end:{SwaggerComposer.Stage}");
            if (beginMatches.Count > 1) throw new ArgumentException($@"catalog-info must have only 1 beginning comment token for the stage ""{SwaggerComposer.Stage}"". Found {beginMatches.Count} begin tokens. e.g. #stage-begin:{SwaggerComposer.Stage}");
            if (endMatches.Count > 1) throw new ArgumentException($@"catalog-info must have only 1 ending comment token for the stage ""{SwaggerComposer.Stage}"". Found {endMatches.Count} end tokens. e.g. #stage-end:{SwaggerComposer.Stage}");
            var begin = beginMatches.First();
            var end = endMatches.First();
            var replaceStartIndex = begin.Index + begin.Length;
            var replaceEndIndex = end.Index;
            var yaml = $"{CatalogInfoContent.Substring(0, replaceStartIndex)}\n{string.Join('\n', CatalogInfoDefintions)}\n{CatalogInfoContent.Substring(replaceEndIndex)}";
            File.WriteAllText(CatalogInfoPath, yaml);
            Console.WriteLine("Finished catalog-info file update!");
        }
    }
}