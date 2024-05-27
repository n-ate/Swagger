# ![n-ate](icon.jpg) n-ate Swagger Package
Note, if using this README from Visual Studio you may want to install a markdown editor extension.

**Quick start with** [general API setup](n-ate-swagger?path=/about/general-setup.md&_a=preview).

**For authorization of a client application see the** [client authorization guide](n-ate-swagger?path=/about/client-authorization-guide.md&_a=preview).

**Package Features:**
The n-ate Swagger package provides tooling common to **API projects** shortening development time and enhancing Swagger pages for greater utility. The following **significant features** are available in this package:

- [Auto-login for Swagger HTML pages](n-ate-swagger?path=/about/auto-login.md&_a=preview)
- [Advanced versioning capabilities](n-ate-swagger?path=/about/versioning.md&_a=preview)
- [Health check endpoint mapping](n-ate-swagger?path=/about/health-checks.md&_a=preview)
- [Azure build pipeline version capture](n-ate-swagger?path=/about/versioning.md&_a=preview)
- [Displaying example request JSON on Swagger pages](n-ate-swagger?path=/about/example-request-json.md&_a=preview)
- [Enhancing a Swagger HTML page with CSS and Javascript](n-ate-swagger?path=/about/js-enhancement-of-swagger-pages.md&_a=preview)

Other **minor features** available in this package:

- listing all registered routes
- Preventing GET endpoint caching
- Defining querystring variables

[For a visual summary of Swagger page features click here.](n-ate-swagger?path=/about/swagger-page-features.md&_a=preview)

---




//TODO: Ensure each of the following features are properly documented:


    Audit Routes Endpoint
        required configuration
            AddFreshSwaggerGen()
            AddControllers()    
            UseFreshSwagger()
            UseEndpoints()
            MapControllers()
            MapAuditEndpoint()

Swagger UI Configuration

     options.InjectDefaultExplanationHtml("<h1>Hello World!</h1>");
     options.InjectExplanationFile(FreshVersion.Get("test-v1.1"), @"wwwroot\html\fragments\swagger-test-definition-explanation.html");
 
     options.InjectOperationsFilter();
 
     options.OnDefinitionLoadExecuteScript(@"console.log(""OnDefinitionLoadExecuteScript() executed successfully again!"");");
     options.OnDefinitionLoadExecuteScript(@"console.log(""OnDefinitionLoadExecuteScript() executed successfully!"");");
 
     options.SpecifyStracktraceFormattingField("api-execution-stack");
     options.SpecifyStracktraceFormattingField("stack");
 
     options.UseAutomaticAuthorization();



**List of attributes used by this package:**

| Attribute name        | Attribute Namespace      |
|:----------------------|:-------------------------|
| Authorize             | Microsoft.AspNetCore.Authorization |
| ApiController         | Microsoft.AspNetCore.Mvc |
| FreshApiVersion       | n_ate.Swagger            |
| ControllerName        | Microsoft.AspNetCore.Mvc |
| HttpDelete            | Microsoft.AspNetCore.Mvc |
| HttpGet               | Microsoft.AspNetCore.Mvc |
| HttpPatch             | Microsoft.AspNetCore.Mvc |
| HttpPost              | Microsoft.AspNetCore.Mvc |
| HttpPut               | Microsoft.AspNetCore.Mvc |
| NoCache               | n_ate.Swagger            |
| Produces              | Microsoft.AspNetCore.Mvc |
| ProducesResponseType  | Microsoft.AspNetCore.Mvc |
| QueryStringParameter  | n_ate.Swagger            |
| RequiresRequestType   | n_ate.Swagger            |
| Route                 | Microsoft.AspNetCore.Mvc |

