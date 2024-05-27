using Microsoft.AspNetCore.Mvc.Filters;

namespace n_ate.Swagger.Attributes
{
    //[AttributeUsage(AttributeTargets.Method)]
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Response != null)
            {
                context.HttpContext.Response.Headers.CacheControl = "max-age=0,no-cache,no-store";
            }
        }
    }
}