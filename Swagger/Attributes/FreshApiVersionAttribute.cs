using Microsoft.AspNetCore.Mvc;
using n_ate.Swagger.Versioning;

namespace n_ate.Swagger.Attributes
{
    /// <summary>
    /// ApiVersionAttribute that automatically adds a "v" prefix. e.g. "v1.0".
    /// </summary>
    public class FreshApiVersionAttribute : ApiVersionAttribute
    {
        /// <summary>
        /// ApiVersionAttribute that automatically adds a "v" prefix. e.g. "v1.0". Also supports a named version. e.g. "blue-v1.3"
        /// </summary>
        public FreshApiVersionAttribute(string version) : base(FreshVersion.Get(version)) { }
    }
}