namespace n_ate.Swagger.Examples
{
    /// <summary>
    /// Interface that allows configuring the Swagger example class.
    /// </summary>
    /// <typeparam name="TExample">The example type implementing the interface.</typeparam>
    public interface ISwaggerExample<TExample>
        where TExample : ISwaggerExample<TExample>
    {
        void ConfigureSwaggerExample(VisibilityBuilder<TExample> builder);
    }

    internal class MockSwaggerExample : ISwaggerExample<MockSwaggerExample>
    {
        public void ConfigureSwaggerExample(VisibilityBuilder<MockSwaggerExample> builder)
        {
        }
    }
}