using System.Linq.Expressions;
using System.Reflection;

namespace n_ate.Swagger.Examples
{
    /// <summary>
    /// Assists in the configuration of example payloads for Swagger pages.
    /// </summary>
    /// <typeparam name="TExample">The example type being configured.</typeparam>
    public class VisibilityBuilder<TExample>
        where TExample : ISwaggerExample<TExample>
    {
        internal VisibilityBuilder()
        { }

        /// <summary>
        /// Hides a member from the output Swagger example payload.
        /// </summary>
        /// <param name="member">The member to hide.</param>
        /// <returns>The configuration builder.</returns>
        public VisibilityBuilder<TExample> Hide(Expression<Func<TExample, object>> member)
        {
            Expression expression = member.Body;
            if (expression is UnaryExpression unaryExp) expression = unaryExp.Operand;
            if (expression is MemberExpression memberExp)
            {
                VisibilityBuilderRepository.AddHiddenField<TExample>(memberExp.Member.Name);
            }
            else throw new NotImplementedException("Unknown expression type. Expected a member expression.");
            return this;
        }
    }

    internal static class VisibilityBuilder
    {
        internal static bool TryConfigureAsSwaggerExample(object example)
        {
            var exampleType = example.GetType();
            var exampleTypeInterfaceName = typeof(ISwaggerExample<>).FullName!;
            if (exampleType.GetInterfaces().Any(i => i.FullName!.StartsWith(exampleTypeInterfaceName)))
            {
                //TODO: only run if not yet run
                var builderType = typeof(VisibilityBuilder<>);
                var genericBuilderType = builderType.MakeGenericType(exampleType);
                var builderConstructor = genericBuilderType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
                var builder = builderConstructor.Invoke(new object[0]);
                exampleType.InvokeMember(nameof(MockSwaggerExample.ConfigureSwaggerExample), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, example, new[] { builder });
                return true;
            }
            return false;
        }
    }
}