using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using n_ate.Essentials;
using n_ate.Swagger.Attributes;

namespace n_ate.Swagger.RequestBody
{
    public static class RequestBodyExtensions
    {
        /// <summary>
        /// Adds a JSON input formatter that enables the propertySet filtering declared with <see cref="RequestBodyAttribute"/>.
        /// </summary>
        /// <returns></returns>
        public static MvcOptions AddRequestBodyAttributeInputFormatter(this MvcOptions options)
        {
            var jsonFormatter = options.InputFormatters.FirstOrDefault(f => f.GetType().IsAssignableTo(typeof(SystemTextJsonInputFormatter)));
            if (jsonFormatter != null)
            {
                var formatter = (jsonFormatter as SystemTextJsonInputFormatter)!;
                const string FIELD_JSONOPTIONS = "_jsonOptions";
                const string FIELD_LOGGER = "_logger";
                if (
                    formatter.TryGetValue<JsonOptions>(FIELD_JSONOPTIONS, out var jsonOptions) &&
                    formatter.TryGetValue<ILogger<SystemTextJsonInputFormatter>>(FIELD_LOGGER, out var logger)
                )
                {
                    var index = options.InputFormatters.IndexOf(formatter);
                    options.InputFormatters.Remove(formatter);
                    var requestInputFormatter = new RequestBodyAttributeJsonInputFormatter(jsonOptions!, logger!);
                    options.InputFormatters.Insert(index, requestInputFormatter);
                }
                else throw new ArgumentException($"The structure of {nameof(formatter)} has changed. Correct code and rebuild.", nameof(formatter));
            }
            return options;
        }
    }
}