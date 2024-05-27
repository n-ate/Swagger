namespace n_ate.Swagger.Examples
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class SwaggerExampleHiddenAttribute : Attribute
    {
    }
}