using Microsoft.AspNetCore.Mvc;

namespace n_ate.Swagger.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequestBodyAttribute : FromBodyAttribute
    {
        /// <param name="propertySet">The property set declared using <see cref="Essentials.PropertySets.PropertySetAttribute"/> on the class used as the action method request body.</param>
        public RequestBodyAttribute(string propertySet)
        {
        }

        public RequestBodyAttribute()
        {
        }
    }
}