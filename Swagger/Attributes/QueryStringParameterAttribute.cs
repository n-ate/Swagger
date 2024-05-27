namespace n_ate.Swagger.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class QueryStringParameterAttribute : Attribute
    {
        public QueryStringParameterAttribute(string name, object exampleValue, bool required, string description)
        {
        }

        public override object TypeId => Guid.Parse("9c5c2825-478a-4cfb-a8c4-877a19e1e4f9");
    }
}