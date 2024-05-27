namespace n_ate.Swagger.Examples
{
    internal static class VisibilityBuilderRepository
    {
        private static Dictionary<Type, Dictionary<string, object?>> _store = new Dictionary<Type, Dictionary<string, object?>>();

        internal static void AddHiddenField<TExample>(string fieldName)
            where TExample : ISwaggerExample<TExample>
        {
            Dictionary<string, object?> fieldNames;
            if (_store.TryGetValue(typeof(TExample), out var names))
            {
                fieldNames = names;
            }
            else
            {
                fieldNames = new Dictionary<string, object?>();
                _store[typeof(TExample)] = fieldNames;
            }
            fieldNames[fieldName] = null; //uses dictionary to avoid saving duplicates..
        }

        internal static string[] GetHiddenFields(object example)
        {
            if (_store.TryGetValue(example.GetType(), out var values)) return values.Keys.ToArray();
            return new string[0];
        }
    }
}