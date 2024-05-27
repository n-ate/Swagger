using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using n_ate.Essentials;
using n_ate.Essentials.Serialization;
using n_ate.Swagger.Versioning;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace n_ate.Swagger.UI
{
    public class FreshSwaggerUIOptions
    {
        public const string FIELDNAME_EXPLANATION_DEFAULT = "__DEFAULT_EXPLANATION__";
        public const string ROUTE_AUTOMATIC_AUTHORIZATION_JS = $"{ROUTE_BASE}/automatic-authorization.js";
        public const string ROUTE_BASE = "/n-ate-swagger";
        public const string ROUTE_DEFINITION_ONLOAD_JS = $"{ROUTE_BASE}/definition-onload.js";
        public const string ROUTE_EXPLANATION_INJECTOR_JS = $"{ROUTE_BASE}/explanation-injector.js";
        public const string ROUTE_EXTENDER_CSS = $"{ROUTE_BASE}/swagger-extender.css";
        public const string ROUTE_EXTENDER_JS = $"{ROUTE_BASE}/swagger-extender.js";
        public const string ROUTE_OPERATIONS_FILTER_JS = $"{ROUTE_BASE}/swagger-operations-filter.js";
        public const string ROUTE_STACKTRACE_FORMATTER_JS = $"{ROUTE_BASE}/stacktrace-formatter.js";
        private static Regex JSON_FIELD_NAME = new Regex("[a-z0-9_$@~#%^&!*?<>|=+-]*");
        private bool _completed;
        private List<string> _definitionLoadScripts = new List<string>();
        private Dictionary<FreshVersion, string> _explanationHtml = new Dictionary<FreshVersion, string>();
        private List<string> _responseStackTraceFormatFields = new List<string>();

        private FreshSwaggerUIOptions(SwaggerUIOptions standardOptions)
        {
            More = standardOptions;
        }

        /// <summary>
        /// Swagger page will attempt to auto-authorize users.
        /// <see>See README.md for info.</see>
        /// </summary>
        public bool AutoAuthorize { get; private set; }

        /// <summary>
        /// HTML content that is injected into the explanation section of the Swagger page.
        /// </summary>
        public string DefaultExplanationHtml { get; private set; } = string.Empty;

        public string[] DefinitionLoadScripts
        { get { return _definitionLoadScripts.ToArray(); } }

        public ReadOnlyDictionary<FreshVersion, string> ExplanationHtml
        { get { return new ReadOnlyDictionary<FreshVersion, string>(_explanationHtml); } }

        public SwaggerUIOptions More { get; }

        /// <summary>
        /// A widget that supports the filtering of operations will be injected into the Swagger page.
        /// </summary>
        public bool OperationsFilter { get; private set; }

        /// <summary>
        /// JSON response field names that will be formatted as a .NET stacktrace.
        /// </summary>
        public string[] ResponseStackTraceFormatFields
        { get { return _responseStackTraceFormatFields.ToArray(); } }

        /// <summary>
        /// Adds an HTML explanation to the top of each Swagger page.
        /// </summary>
        /// <param name="filePath">An HTML file.</param>
        public void InjectDefaultExplanationFile(string filePath)
        {
            CheckCompleted();
            filePath = TryGetFilePath(filePath);
            var html = File.ReadAllText(filePath);
            AddDefaultExplanationHtml(filePath, html);
        }

        /// <summary>
        /// Adds an HTML explanation to the top of each Swagger page.
        /// </summary>
        /// <param name="htmlContent">HTML content.</param>
        public void InjectDefaultExplanationHtml(string htmlContent)
        {
            CheckCompleted();
            AddDefaultExplanationHtml("inline", htmlContent);
        }

        /// <summary>
        /// Adds a version specific HTML explanation to the top of the versioned Swagger page overriding any default explanation added.
        /// </summary>
        /// <param name="version">The version to which the explanation applies.</param>
        /// <param name="htmlContent">HTML content.</param>
        public void InjectDefaultExplanationHtml(FreshVersion version, string htmlContent)
        {
            CheckCompleted();
            AddExplanationHtml("inline", version, htmlContent);
        }

        /// <summary>
        /// Adds a version specific HTML explanation to the top of the versioned Swagger page overriding any default explanation added.
        /// </summary>
        /// <param name="version">The version to which the explanation applies.</param>
        /// <param name="filePath">An HTML file.</param>
        public void InjectExplanationFile(FreshVersion version, string filePath)
        {
            CheckCompleted();
            filePath = TryGetFilePath(filePath);
            var html = File.ReadAllText(filePath);

            AddExplanationHtml(filePath, version, html);
        }

        /// <summary>
        /// Injects an operations filter widget into the Swagger page.
        /// </summary>
        public void InjectOperationsFilter()
        {
            CheckCompleted();
            OperationsFilter = true;
        }

        /// <summary>
        /// Executes the script each time a Swagger definition is loaded. This is the main injection event provided by the n_ate.Swagger package.
        /// </summary>
        /// <param name="filePath">A JavaScript file.</param>
        public void OnDefinitionLoadExecuteFile(string filePath)
        {
            CheckCompleted();
            filePath = TryGetFilePath(filePath);
            AddOnDefinitionLoadScript(filePath, File.ReadAllText(filePath));
        }

        /// <summary>
        /// Executes the script each time a Swagger definition is loaded. This is the main injection event provided by the n_ate.Swagger package.
        /// </summary>
        /// <param name="executionScript">JavaScript as a string.</param>
        public void OnDefinitionLoadExecuteScript(string executionScript)
        {
            CheckCompleted();
            AddOnDefinitionLoadScript("inline", executionScript);
        }

        /// <summary>
        /// Adds a field name that when returned in a JSON response will be formatted as a .NET stacktrace.
        /// </summary>
        /// <param name="fieldName">The name of the field to format as a stacktrace.</param>
        public void SpecifyStracktraceFormattingField(string fieldName)
        {
            CheckCompleted();
            if (!JSON_FIELD_NAME.IsMatch(fieldName)) throw new ArgumentException("The field name contained invalid characters.", nameof(fieldName));
            _responseStackTraceFormatFields.Add(fieldName);
        }

        /// <summary>
        /// Injects an auto-authorize script that will attempt to automate user Authorization.
        /// <see>See README.md for info.</see>
        /// </summary>
        public void UseAutomaticAuthorization()
        {
            CheckCompleted();
            AutoAuthorize = true;
        }

        internal static FreshSwaggerUIOptions Init(IApplicationBuilder app, SwaggerUIOptions standardOptions, Action<FreshSwaggerUIOptions> uiOptionsConfigurator)
        {
            var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "<entry-assembly-has-no-name>";
            if (app is IHost host)
            {
                var apiVersionDescriptionProvider = host.Services.GetService<IApiVersionDescriptionProvider>()!;
                var descriptions = apiVersionDescriptionProvider.GetVersionDescriptions()
                    .OrderBy(v =>
                    { //order definitions by 'v' (e.g. v1.0, v4.8) first and then alphabetical..
                        var version = (v.ApiVersion as FreshVersion)?.Raw ?? string.Empty;
                        return version.StartsWith("v") ? $"_{version}" : version;
                    });
                foreach (var description in descriptions)
                {
                    standardOptions.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"{entryAssemblyName.CamelCaseToFriendly()} - {description.GroupName}");////!!!!!!!
                }
            }
            standardOptions.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            standardOptions.DefaultModelExpandDepth(-1);
            standardOptions.EnableTryItOutByDefault();
            //defaultSwaggerUIOptions.EnablePersistAuthorization(); //NOTE: this works great until authorization expires, then its not clear why requests are failing.
            standardOptions.DisplayOperationId();

            //Inject Main CSS and JS Routes//
            standardOptions.InjectStylesheet(ROUTE_EXTENDER_CSS);
            standardOptions.InjectJavascript(ROUTE_EXTENDER_JS);

            //Inject Automatic Authorization Route//
            standardOptions.InjectJavascript(ROUTE_AUTOMATIC_AUTHORIZATION_JS);

            //Inject Stacktrace Formatter Route//
            standardOptions.InjectJavascript(ROUTE_STACKTRACE_FORMATTER_JS);

            //Inject Explanation Html Routes//
            standardOptions.InjectJavascript(ROUTE_EXPLANATION_INJECTOR_JS);

            //Inject Definition Load Script Routes//
            standardOptions.InjectJavascript(ROUTE_DEFINITION_ONLOAD_JS);

            var freshOptions = new FreshSwaggerUIOptions(standardOptions);
            uiOptionsConfigurator.Invoke(freshOptions);

            //Inject Main CSS and JS Routes//
            if (freshOptions.OperationsFilter) standardOptions.InjectJavascript(ROUTE_OPERATIONS_FILTER_JS);

            return freshOptions;
        }

        internal void Complete(IEndpointRouteBuilder builder)
        {
            CheckCompleted();
            _completed = true;

            var manifest = new StringBuilder();
            var manifestPad = 30;

            //Map Main CSS and JS Routes//
            builder.MapGet(ROUTE_EXTENDER_CSS, () => Results.Content(Resources.Resources.swagger_extender_css, "text/css"));
            manifest.AppendLine(ROUTE_EXTENDER_CSS.PadLeft(manifestPad));
            builder.MapGet(ROUTE_EXTENDER_JS, () => Results.Content(Resources.Resources.swagger_extender_js, "application/javascript"));
            manifest.AppendLine(ROUTE_EXTENDER_JS.PadLeft(manifestPad));

            //Map Automatic Authorization Route//
            var script = new StringBuilder($"// Injected via: ApplicationBuilder.{nameof(IApplicationBuilderExtensions.UseFreshSwagger)}( options => options.{nameof(UseAutomaticAuthorization)}.. )");
            script.AppendLine();
            script.AppendLine(JSConsoleLogFileLoad(ROUTE_AUTOMATIC_AUTHORIZATION_JS));
            script.AppendLine("swaggerExtender.autoAuthorize();");
            var autoAuthorizeScript = script.ToString();
            builder.MapGet(ROUTE_AUTOMATIC_AUTHORIZATION_JS, () => Results.Content(autoAuthorizeScript, "application/javascript"));
            manifest.AppendLine(ROUTE_AUTOMATIC_AUTHORIZATION_JS.PadLeft(manifestPad));

            //Map Stacktrace Formatter Route//
            script.Clear();
            script.AppendLine($"// Injected via: ApplicationBuilder.UseFreshSwagger( options => options.{nameof(SpecifyStracktraceFormattingField)}.. )");
            script.AppendLine();
            script.AppendLine(JSConsoleLogFileLoad(ROUTE_STACKTRACE_FORMATTER_JS));
            foreach (var field in ResponseStackTraceFormatFields) script.AppendLine(@$"swaggerExtender.registerResultStackTraceFormatter(""{field}"");");
            var stacktraceFormatterScript = script.ToString();
            builder.MapGet(ROUTE_STACKTRACE_FORMATTER_JS, () => Results.Content(stacktraceFormatterScript, "application/javascript"));
            manifest.AppendLine(ROUTE_STACKTRACE_FORMATTER_JS.PadLeft(manifestPad));

            //Map Explanation Html Routes//
            script.Clear();
            var defaultKey = FreshVersion.Get(FIELDNAME_EXPLANATION_DEFAULT);
            var simplifiedExplanations = _explanationHtml.ToDictionary(kv => kv.Key.Raw, kv => kv.Value);
            simplifiedExplanations.Add(FIELDNAME_EXPLANATION_DEFAULT, DefaultExplanationHtml);
            var serializer = new JsonSerializerOptions { WriteIndented = true };
            serializer.Converters.Add(new StringDictionaryConverter<string>());
            var explanationJson = JsonSerializer.Serialize(simplifiedExplanations, serializer);
            script.AppendLine($"// Injected via: ApplicationBuilder.UseFreshSwagger()");
            script.AppendLine();
            script.AppendLine(JSConsoleLogFileLoad(ROUTE_EXPLANATION_INJECTOR_JS));
            script.AppendLine("(function(){");
            script.AppendLine("const explanations = ");
            script.AppendLine(explanationJson);
            script.AppendLine(";");
            script.AppendLine();
            script.AppendLine("swaggerExtender.registerDefinitionLoadedListener(() => {");
            script.AppendLine("    const version = swaggerExtender.getVersion();");
            script.AppendLine($@"    let html = explanations[version] ?? explanations.{FIELDNAME_EXPLANATION_DEFAULT};");
            script.AppendLine("    swaggerExtender.appendExplanationHtml(html);");
            script.AppendLine("});");
            script.AppendLine();
            script.AppendLine("})();");
            var explanationInjectorScript = script.ToString();
            builder.MapGet(ROUTE_EXPLANATION_INJECTOR_JS, () => Results.Content(explanationInjectorScript, "application/javascript"));
            manifest.Append(ROUTE_EXPLANATION_INJECTOR_JS.PadLeft(manifestPad));

            //Map Main CSS and JS Routes//
            if (OperationsFilter)
            {
                builder.MapGet(ROUTE_OPERATIONS_FILTER_JS, () => Results.Content(Resources.Resources.swagger_operations_filter_js, "text/css"));
            }
            manifest.AppendLine(ROUTE_OPERATIONS_FILTER_JS.PadLeft(manifestPad));

            //Map Definition Load Script Routes//
            script.Clear();
            script.AppendLine($"// Injected via: ApplicationBuilder.UseFreshSwagger( options => options.OnDefinitionLoadExecute.. )");
            script.AppendLine();
            script.AppendLine(JSConsoleLogFileLoad(ROUTE_DEFINITION_ONLOAD_JS));
            foreach (var loadScript in DefinitionLoadScripts)
            {
                script.AppendLine("swaggerExtender.registerDefinitionLoadedListener(() => {");
                script.AppendLine(loadScript);
                script.AppendLine("});");
                script.AppendLine();
            }
            var onloadScripts = script.ToString();
            builder.MapGet(ROUTE_DEFINITION_ONLOAD_JS, () => Results.Content(onloadScripts, "application/javascript"));
            manifest.AppendLine(ROUTE_DEFINITION_ONLOAD_JS.PadLeft(manifestPad));

            //Map Files Manifest//
            var manifestHtml = manifest.ToString();
            builder.MapGet("n-ate-swagger", () => manifestHtml);
        }

        private static string JSConsoleLogFileLoad(string fileNameOrPath)
        {
            var segments = fileNameOrPath.Split("/");
            return $@"console.log(""{segments[segments.Length - 1]} loaded."");";
        }

        private static string TryGetFilePath(string filePath)
        {
            if (filePath is not null)
            {
                var paths = Files.FindMatchingAbsoluteFilePaths(filePath);
                if (!paths.Any()) throw new ArgumentException("File could not be found. Ensure file path is valid.", nameof(filePath));
                filePath = paths[0];
                //if (Path.DirectorySeparatorChar != '\\') filePath = filePath.Replace('\\', Path.DirectorySeparatorChar); //converts Windows to Linux
                //if (!File.Exists(filePath))
                //{
                //    filePath = $"{Path.GetFullPath(".")}{(filePath.StartsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar)}{filePath}";
                //    if (!File.Exists(filePath)) throw new ArgumentException("File does not exist. Ensure file path is valid.", nameof(filePath));
                //}
            }
            return filePath ?? string.Empty;
        }

        private void AddDefaultExplanationHtml(string source, string html)
        {
            if (DefaultExplanationHtml.Any())
            {
                DefaultExplanationHtml =
                    $@"{DefaultExplanationHtml}

        <hr/>

        <!-- ~~~~~~~ Source: {source} ~~~~~~~ -->
{html}";
            }
            else
            {
                DefaultExplanationHtml =
                    $@"
<!-- Injected via: ApplicationBuilder.{nameof(IApplicationBuilderExtensions.UseFreshSwagger)}( options => options.{nameof(FreshSwaggerUIOptions.InjectDefaultExplanationHtml)}/{nameof(FreshSwaggerUIOptions.InjectDefaultExplanationFile)}.. ) -->

        <!-- ~~~~~~~ Source: {source} ~~~~~~~ -->
{html}";
            }
        }

        private void AddExplanationHtml(string source, FreshVersion version, string html)
        {
            if (_explanationHtml.ContainsKey(version))
            {
                _explanationHtml[version] =
                    $@"{_explanationHtml[version]}

        <hr/>

        <!-- ~~~~~~~ Source: {source} ~~~~~~~ -->
{html}";
            }
            else
            {
                _explanationHtml[version] =
                    $@"
<!-- Injected via: ApplicationBuilder.{nameof(IApplicationBuilderExtensions.UseFreshSwagger)}( options => options.{nameof(FreshSwaggerUIOptions.InjectDefaultExplanationHtml)}/{nameof(FreshSwaggerUIOptions.InjectDefaultExplanationFile)}.. ) -->

        <!-- ~~~~~~~ Source: {source} ~~~~~~~ -->
{html}";
            }
        }

        private void AddOnDefinitionLoadScript(string source, string script)
        {
            _definitionLoadScripts.Add(
                $@"        <!-- ~~~~~~~ Source: {source} ~~~~~~~ -->
{script}");
        }

        private void CheckCompleted([CallerMemberName] string memberName = "<unknown>")
        {
            if (_completed) throw new InvalidOperationException($"Cannot invoke {memberName} after {nameof(Complete)}() has been called.");
        }
    }
}