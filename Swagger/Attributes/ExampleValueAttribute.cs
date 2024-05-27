namespace n_ate.Swagger.Attributes
{
    /// <summary>
    /// Attribute used to decorate properties with example values available for swagger example payloads.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExampleValueAttribute : Attribute
    {
        /// <param name="exampleValue">Example values exposed by decorating action methods with <see cref="RequiresRequestTypeAttribute"/>.</param>
        public ExampleValueAttribute(object exampleValue)
        {
        }
    }
}