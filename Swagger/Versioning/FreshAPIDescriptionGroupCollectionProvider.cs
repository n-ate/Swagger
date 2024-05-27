using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace n_ate.Swagger.Versioning
{
    public class FreshAPIDescriptionGroupCollectionProvider : ApiDescriptionGroupCollectionProvider
    {
        public FreshAPIDescriptionGroupCollectionProvider(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders) : base(actionDescriptorCollectionProvider, apiDescriptionProviders)
        {
        }
    }
}