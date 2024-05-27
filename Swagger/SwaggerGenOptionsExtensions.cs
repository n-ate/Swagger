using Swashbuckle.AspNetCore.SwaggerGen;

namespace n_ate.Swagger
{
    public static class SwaggerGenOptionsExtensions
    {
        public static void AddFreshApiPropertySetNames(this SwaggerGenOptions options, params string[] setNames)
        {
            ApiSchemaFilter.ApiPropertySetNames = ApiSchemaFilter.ApiPropertySetNames.Concat(setNames).Distinct().ToArray();
        }
    }
}