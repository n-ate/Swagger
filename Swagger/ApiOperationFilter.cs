using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using n_ate.Essentials;
using n_ate.Swagger.Attributes;
using n_ate.Swagger.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace n_ate.Swagger
{
    /// <summary>
    ///
    /// </summary>
    public class ApiOperationFilter : IOperationFilter
    {
        private readonly OpenApiSecurityRequirement _security;

        public ApiOperationFilter(OpenApiSecurityRequirement security)
        {
            this._security = security;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            ApplyRequiresRequestTypeAttribute(operation, context);

            ApplyRequestBodyAttribute(operation, context);

            ApplyParameterInfo(operation, context);

            ApplyQueryStringParameterAttribute(operation, context);

            ApplyAuthorization(operation, context);

            //ApplyObsoleteAttribute(operation, context);
        }

        private static void ApplyParameterInfo(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            foreach (var parameter in operation?.Parameters ?? new OpenApiParameter[0])
            {
                var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);
                if (parameter.Description is null)
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                if (parameter.Schema.Default is null && description.DefaultValue is not null)
                {
                    parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
                }

                parameter.Required |= description.IsRequired;
            }
        }

        private void ApplyAuthorization(OpenApiOperation operation, OperationFilterContext context)
        {
            var methodAuthorizeAttribute = context.MethodInfo.CustomAttributes.LastOrDefault(a => a.AttributeType.IsAssignableTo(typeof(AuthorizeAttribute)));
            var classAuthorizeAttribute = context.MethodInfo.DeclaringType!.CustomAttributes.LastOrDefault(a => a.AttributeType.IsAssignableTo(typeof(AuthorizeAttribute)));
            if (methodAuthorizeAttribute != null || classAuthorizeAttribute != null)
            {
                operation.Security.Add(this._security);
            }
        }

        private void ApplyObsoleteAttribute(OpenApiOperation operation, OperationFilterContext context)
        {
            var obsoleteAttributes = context.MethodInfo.CustomAttributes.ByType<ObsoleteAttribute>();
            foreach (var attribute in obsoleteAttributes)
            {
                var message = attribute.GetValue<string>(0);
                operation.Deprecated = true;
                var tag = operation.Tags.FirstOrDefault(t => t is OpenApiTag) ?? new OpenApiTag();
                tag.Name = $" {tag.Name} (obsolete)";
                operation.Summary = String.IsNullOrWhiteSpace(operation.Description) ? message : $"{message} {operation.Description}";
            }
        }

        private void ApplyQueryStringParameterAttribute(OpenApiOperation operation, OperationFilterContext context)
        {
            var queryStringAttributes = context.MethodInfo.CustomAttributes.ByType<QueryStringParameterAttribute>();
            foreach (var attribute in queryStringAttributes)
            {
                var args = attribute.GetValues(0, 1, 2, 3);
                var name = (string)args[0];
                var exampleValue = GetOpenApiPrimitive(args[1]);
                var required = (bool)args[2];
                var description = (string)args[3];
                var parameter = new OpenApiParameter
                {
                    AllowEmptyValue = false,
                    Description = description,
                    Example = exampleValue.Value,
                    In = ParameterLocation.Query,
                    Name = name,
                    Required = required,
                    Schema = exampleValue.Schema
                };
                operation.Parameters.Add(parameter);
            }
        }

        private void ApplyRequestBodyAttribute(OpenApiOperation operation, OperationFilterContext context)
        {
            var parameterData = context
                .MethodInfo.GetParameters()
                .SelectMany(p => p
                    .CustomAttributes
                    .Where(a => a.AttributeType.IsAssignableFrom(typeof(RequestBodyAttribute)))
                    .Select(a => (p, a))
                );
            foreach (var (parameter, attribute) in parameterData)
            {
                if (attribute.ConstructorArguments.Count() > 1) throw new NotImplementedException();
                else if (attribute.ConstructorArguments.Count() == 1)
                {
                    var propertySetName = (attribute.ConstructorArguments.First().Value! as string)!;
                    var i = 0;
                    foreach (var content in operation.RequestBody.Content)
                    {
                        i++;
                        var openApiExample = GetOpenApiExampleByType(parameter.ParameterType, propertySetName);
                        var exampleName = $"{MassageTypeName(parameter.ParameterType)}{(propertySetName is null ? null : $" {propertySetName}")}{(i == 1 ? null : $" {i}")}";

                        content.Value.Examples.TryAdd(exampleName, openApiExample);
                    }
                }
                //else 0; ignore
            }
        }

        private void ApplyRequiresRequestTypeAttribute(OpenApiOperation operation, OperationFilterContext context)
        {
            var requestTypeAttributes = context.MethodInfo.CustomAttributes.Where(a => a.AttributeType.IsAssignableTo(typeof(RequiresRequestTypeAttribute)));
            foreach (var attribute in requestTypeAttributes)
            {
                if (attribute.ConstructorArguments.Count() > 2) throw new NotImplementedException();

                var requestBodyExampleType = (attribute.ConstructorArguments.First().Value! as Type)!;
                //var propertySetName = attribute.ConstructorArguments.Count() == 2 ? attribute.ConstructorArguments[1].Value! as string : null;
                var i = 0;
                foreach (var content in operation.RequestBody.Content)
                {
                    i++;
                    var openApiExample = GetOpenApiExampleByType(requestBodyExampleType);
                    var exampleName = $"{MassageTypeName(requestBodyExampleType)}{(i == 1 ? null : $" {i}")}";
                    content.Value.Examples.Add(exampleName, openApiExample);//TODO: consolidate or remove this attribute type...
                }
            }
        }

        private OpenApiExample GetOpenApiExampleByType(Type type, string? propertySetName = null)
        {
            var example = new OpenApiObject();
            var instance = Activator.CreateInstance(type)!;
            var properties = propertySetName == null ? type.GetProperties() : type.GetPropertiesOfSets(propertySetName);
            if (VisibilityBuilder.TryConfigureAsSwaggerExample(instance))
            {
                properties = properties.Where(p => !VisibilityBuilderRepository.GetHiddenFields(instance).Contains(p.Name)).ToArray(); //remove properties hidden by interface
            }
            foreach (var property in properties)
            {
                var value = property.GetValue(instance);

                if (property.HasAttribute<SwaggerExampleHiddenAttribute>()) continue; //do not emit properties with hidden attribute

                switch (property.PropertyType.Name)
                {
                    case nameof(Boolean):
                        var boolValue = (bool)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiBoolean(boolValue);
                        break;

                    case nameof(Byte):
                        var byteValue = (byte)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiByte(byteValue);
                        break;

                    case nameof(Char):
                        var charValue = (char)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiString(charValue.ToString());
                        break;

                    case nameof(DateTime):
                        var dtValue = (DateTime)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiDateTime(dtValue);
                        break;

                    case nameof(Decimal):
                        var decimalValue = (decimal)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiDouble((double)decimalValue);
                        break;

                    case nameof(Double):
                        var doubleValue = (double)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiDouble(doubleValue);
                        break;

                    case nameof(Single):
                        var floatValue = (float)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiFloat(floatValue);
                        break;

                    case nameof(Int16):
                        var shortValue = (short)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiInteger(shortValue);
                        break;

                    case nameof(Int32):
                        var intValue = (int)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiInteger(intValue);
                        break;

                    case nameof(Int64):
                        var longValue = (long)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiLong(longValue);
                        break;

                    case nameof(SByte):
                        var sbyteValue = (sbyte)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiInteger(sbyteValue);
                        break;

                    case nameof(Guid):
                    case nameof(String):
                        var strValue = value as string;
                        example[property.Name.FirstCharToLower()] = new OpenApiString(strValue);
                        break;

                    case nameof(UInt16):
                        var ushortValue = (ushort)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiInteger(ushortValue);
                        break;

                    case nameof(UInt32):
                        var uintValue = (uint)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiLong(uintValue);
                        break;

                    case nameof(UInt64):
                        var ulongValue = (ulong)value!;
                        example[property.Name.FirstCharToLower()] = new OpenApiLong((long)ulongValue);
                        break;

                    default: throw new NotImplementedException();
                }
            }
            return new OpenApiExample { Value = example.Any() ? example : new OpenApiNull() };
        }

        private (IOpenApiAny Value, OpenApiSchema Schema) GetOpenApiPrimitive(object? value)
        {
            (IOpenApiAny, OpenApiSchema) result;
            if (value == null) throw new NotImplementedException();// result = (new OpenApiNull(), "");
            else
            {
                switch (value)
                {
                    case string strValue:
                        if (DateTime.TryParse(strValue, out var dateTime))
                        {
                            result = (new OpenApiDateTime(dateTime.ToUniversalTime()), new OpenApiSchema { Type = "string", Format = "date-time" });
                        }
                        else result = (new OpenApiString(strValue), new OpenApiSchema { Type = "string" }); //no format
                        break;

                    case Guid guidValue:
                        result = (new OpenApiString(guidValue.ToString("D")), new OpenApiSchema { Type = "string", Format = "uuid" });
                        break;

                    case bool boolValue:
                        result = (new OpenApiBoolean(boolValue), new OpenApiSchema { Type = "boolean" }); //no format
                        break;

                    case decimal decimalValue:
                        result = (new OpenApiDouble((double)decimalValue), new OpenApiSchema { Type = "number", Format = "double" });
                        break;

                    case double doubleValue:
                        result = (new OpenApiDouble(doubleValue), new OpenApiSchema { Type = "number", Format = "double" });
                        break;

                    case float floatValue:
                        result = (new OpenApiFloat(floatValue), new OpenApiSchema { Type = "number", Format = "float" });
                        break;

                    case int intValue:
                        result = (new OpenApiInteger(intValue), new OpenApiSchema { Type = "integer", Format = "int32" });
                        break;

                    case long longValue:
                        result = (new OpenApiLong(longValue), new OpenApiSchema { Type = "integer", Format = "int64" });
                        break;

                    case byte byteValue:
                        result = (new OpenApiByte(byteValue), new OpenApiSchema { Type = "string", Format = "byte" });
                        break;

                    default: throw new NotImplementedException();
                }
            }
            return result;
        }

        private string MassageTypeName(Type requestBodyExampleType)
        {
            var result = requestBodyExampleType.Name.Replace("Example", "").Replace("Sample", "").Replace("Create", " Create ").Replace("Patch", " Patch ").Replace("Update", " Update ").Replace("Get", " Get ").Replace("Select", " Select ").Trim();
            int length;
            do //remove duplicate whitespace
            {
                length = result.Length;
                result = result.Replace("  ", " ");
            } while (result.Length != length);
            return result;
        }
    }
}