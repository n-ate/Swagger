using System.Diagnostics;

namespace n_ate.Swagger.CommandLine
{
    public static class Help
    {
        public static readonly string ApiDllFileName = Process.GetCurrentProcess().MainModule?.FileName ?? "<my-api>.dll";

        public static readonly string General =
        @$"******************************************** Commands ********************************************
    help                    Prints help information.
    swagger-merge           Command used for merging multiple Swagger JSON files.
    swagger-gen             Command for generating Swagger JSON files.

********************************************   Usages  ********************************************
dotnet {ApiDllFileName} swagger-gen --help

dotnet {ApiDllFileName} swagger-merge --help
";

        public static readonly string SwaggerGenerate =
@$"******************************************** Arguments ********************************************
    ? = optional, ! = required
    --help                  Prints help information.
 ?  --build-version         The build version. This will be added to the Swagger JSON description.
 ?  --catalog-info-dir      The directory containing a catalog-info.yaml and a catalog-info.definition.template.yaml.
                            Specifying this argument will cause the catalog-info to be updated using the template.
                            e.g. --catalog-info-dir=C:\\
                                 --catalog-info-dir='$(Build.SourcesDirectory)/<repo>/<solution>/Documentation/'
 ?  --output-dir            The directory where the Swagger JSON files should be written.
                            e.g. --output-dir=C:\\
                                 --output-dir='$(Build.SourcesDirectory)/<repo>/<solution>/cache/'
 !  --server                The server URL that should be added to the Swagger JSON.
                            e.g. --server=https://api.dev.domain.com
 !  --stage                 The environment/stage of the server.
                            e.g. --stage=Development
                                 --stage=Testing
                                 --stage=Staging
                                 --stage=Production

********************************************   Usages  ********************************************
dotnet {ApiDllFileName} swagger-gen --stage=development --server=https://api.dev.domain.com

dotnet {ApiDllFileName} swagger-gen --stage=development --server=https://api.dev.domain.com --catalog-info-dir='$(Build.SourcesDirectory)/<repo>/<solution>/Documentation/'";

        public static readonly string SwaggerMerge =
                $@"******************************************** Arguments ********************************************
    ? = optional, ! = required
    --help                  Prints help information.
 ?  --catalog-info-dir      The directory containing a catalog-info.yaml and a catalog-info.definition.template.yaml.
                            Specifying this argument will cause the catalog-info to be updated using the template.
                            e.g. --catalog-info-dir=C:\\
                                 --catalog-info-dir='$(Build.SourcesDirectory)/<repo>/<solution>/Documentation/'
 ?  --merge-dir             The directory containing Swagger JSON files for merging.'
                            e.g. --merge-dir=C:\\
                                 --merge-dir='$(Build.SourcesDirectory)/<repo>/<solution>/cache/swagger/'
 ?  --output-dir            The directory where the Swagger JSON files should be written.
                            e.g. --output-dir=C:\\
                                 --output-dir='$(Build.SourcesDirectory)/<repo>/<solution>/cache/'
 ?  --recursive             Switch indicating subdirectories should be searched recursively.'
                            e.g. --recursive
    --stage                 The environment/stage of the server.
                            e.g. --stage=development
                                 --stage=testing
                                 --stage=staging
                                 --stage=production

********************************************   Usages  ********************************************
dotnet {ApiDllFileName} swagger-merge --merge-dir='$(Build.SourcesDirectory)/<repo>/<solution>/cache/swagger/'

dotnet {ApiDllFileName} swagger-merge --merge-dir='C:\\' --recursive --server=https://api.dev.domain.com --catalog-info-dir='$(Build.SourcesDirectory)/<repo>/<solution>/Documentation/'";

        //case!!
    }
}