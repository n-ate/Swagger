# n-ate Health Checks

Standard n-ate health check endpoints that Argo is configured to use can be quickly and easily added.

- The endpoint URL and response bodies are as follows.

  | URL             | Response Body |
  | --------------- | ------------- |
  | /health/startup | Healthy       |
  | /health/live    | Healthy       |
  | /health/ready   | Healthy       |

- The following configurations must be called in **Program.cs** or **Startup.cs**:
  - AddFreshSwaggerGen()
  - AddFreshHealthCheckEndpoints()
  - AddControllers()    
  - UseFreshSwagger()
  - UseEndpoints()
  - MapControllers()
  - MapFreshHealthChecks()












