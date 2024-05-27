namespace n_ate.Swagger.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequiresRequestTypeAttribute : Attribute
    {
        /// <param name="exampleType">A class that inherits from the action method body type.</param>
        public RequiresRequestTypeAttribute(Type exampleType)
        {
        }
    }
}