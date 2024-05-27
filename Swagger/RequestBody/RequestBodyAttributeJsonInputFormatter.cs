using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using n_ate.Essentials;
using n_ate.Essentials.Serialization;
using n_ate.Swagger.Attributes;
using System.Text.Json;

namespace n_ate.Swagger.RequestBody
{
    internal class RequestBodyAttributeJsonInputFormatter : SystemTextJsonInputFormatter
    {
        /// <summary></summary>
        public RequestBodyAttributeJsonInputFormatter(JsonOptions options, ILogger<SystemTextJsonInputFormatter> logger) : base(options, logger)
        {
            AllowExceptionMessages = options.AllowInputFormatterExceptionMessages;
            Logger = logger;
        }

        internal bool AllowExceptionMessages { get; }
        internal ILogger<SystemTextJsonInputFormatter> Logger { get; }

        /// <summary></summary>
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            const string PROPERTY_IDENTITY = "Identity";
            if (context.Metadata.TryGetValue<ModelMetadataIdentity>(PROPERTY_IDENTITY, out var identity))
            {
                var requestBodyAttribute = identity.ParameterInfo?.CustomAttributes.FirstOrDefault(a => a.AttributeType.IsAssignableTo(typeof(RequestBodyAttribute)));
                if (requestBodyAttribute is not null) //has a RequestBodyAttribute
                {
                    var serializerOptions = new JsonSerializerOptions(this.SerializerOptions);
                    if (requestBodyAttribute.ConstructorArguments.Any())
                    {
                        var propertySetName = requestBodyAttribute.ConstructorArguments.First().Value as string;
                        serializerOptions.Converters.Insert(0, new PropertySetConverter(propertySetName!));
                    }
                    var jsonOptions = new JsonOptions { AllowInputFormatterExceptionMessages = this.AllowExceptionMessages };
                    jsonOptions.SetPropertyBackingField(nameof(jsonOptions.JsonSerializerOptions), serializerOptions);
                    var formatter = new SystemTextJsonInputFormatter(jsonOptions, this.Logger);
                    return formatter.ReadRequestBodyAsync(context);
                }
                return base.ReadRequestBodyAsync(context);
            }
            else throw new NotImplementedException();
        }
    }
}