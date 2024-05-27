using Microsoft.AspNetCore.Mvc.Versioning;

namespace n_ate.Swagger.Versioning
{
    public class FreshApiVersioningOptions
    {
        private FreshApiVersioningOptions(ApiVersioningOptions standardOptions)
        {
            More = standardOptions;
        }

        public ApiVersioningOptions More { get; }

        internal static FreshApiVersioningOptions Init(ApiVersioningOptions standardOptions)
        {
            standardOptions.AssumeDefaultVersionWhenUnspecified = true;

            return new FreshApiVersioningOptions(standardOptions);
        }
    }
}