using System.Reflection;

namespace n_ate.Swagger
{
    public static class Settings
    {
        private static Dictionary<string, string>? _assemblyMetadataAttributeData = null;

        public static Dictionary<string, string> AssemblyMetadataAttributeData
        {
            get
            {
                if (_assemblyMetadataAttributeData is null)
                {
                    _assemblyMetadataAttributeData = Assembly
                        .GetExecutingAssembly()
                        .GetCustomAttributesData()
                        .Where(attr => attr
                            .AttributeType == typeof(AssemblyMetadataAttribute) &&
                            attr.ConstructorArguments.Count() == 2 &&
                            attr.ConstructorArguments.All(arg => arg.ArgumentType == typeof(string))
                        )
                        .ToDictionary(attr => (string)attr.ConstructorArguments[0].Value!, attr => (string)attr.ConstructorArguments[1].Value!);
                }
                return _assemblyMetadataAttributeData;
            }
        }

        public static string RepositoryUrl => AssemblyMetadataAttributeData.TryGetValue("RepositoryUrl", out var value) ? value.Replace(" ", "%20") : string.Empty;
    }
}