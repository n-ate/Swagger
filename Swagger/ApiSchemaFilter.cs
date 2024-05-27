using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using n_ate.Essentials;
using n_ate.Essentials.PropertySets;
using n_ate.Swagger.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace n_ate.Swagger
{
    public class ApiSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// PropertySet names to expose in the swagger.json document and Swagger UI. <seealso cref="PropertySetAttribute"/>
        /// </summary>
        internal static string[] ApiPropertySetNames = new string[0];

        private static Dictionary<MemberInfo, OpenApiSchema> _processedMemberSchemas = new Dictionary<MemberInfo, OpenApiSchema>();

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.MemberInfo is MemberInfo member)
            {
                _processedMemberSchemas[member] = schema;
            }

            if (context.MemberInfo is null && context.ParameterInfo is null) //this is usually a POCO type object
            {
                var hasMemberWithPropertySetAttribute = context.Type.GetMembers().Any(m => m.GetCustomAttribute<PropertySetAttribute>(true) is not null);
                if (hasMemberWithPropertySetAttribute) //this should be filtered based on PropertySetAttribute argument names
                {
                    //schema.Properties.Clear();

                    var membersToInclued = context.Type.GetPropertiesOfSets(ApiPropertySetNames).ToDictionary(m => m.Name.FirstCharToLower(), m => m);

                    // remove those properties to not be included //
                    foreach (var key in schema.Properties.Select(kv => kv.Key).ToArray())
                    {
                        if (!membersToInclued.ContainsKey(key))
                        {
                            schema.Properties.Remove(key);
                        }
                    }

                    UpdateSchemaPropertiesByType(schema, context.Type, membersToInclued);
                }
            }
        }

        private static void UpdateSchemaPropertiesByType(OpenApiSchema schema, Type? queryType, Dictionary<string, PropertyInfo> membersToInclued)
        {
            if (queryType == null) return;
            UpdateSchemaPropertiesByType(schema, queryType.BaseType, membersToInclued); //recursive update base types

            var processedMembers = _processedMemberSchemas.Where(kv => kv.Key.DeclaringType == queryType).ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var include in membersToInclued)
            {
                if (processedMembers.TryGetValue(include.Value, out var value))
                {
                    if (schema.Properties[include.Key] != value)
                    {
                        schema.Properties[include.Key] = value;
                    }
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
    }
}