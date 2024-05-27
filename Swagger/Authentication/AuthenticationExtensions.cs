using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace n_ate.Swagger
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds fresh standard authentication that uses a JWT bearer token.
        /// </summary>
        /// <param name="authorityHostInstance">E.g. https://login.microsoftonline.com</param>
        /// <param name="audienceDomain">E.g. <audience-domain>.onmicrosoft.com</param>
        /// <param name="tenantId">An Azure tenant GUID.</param>
        /// <param name="clientId">An Azure client GUID.</param>
        /// <param name="azureAppId">An Azure app GUID.</param>
        public static AuthenticationBuilder AddFreshJwtBearerAuthentication(this IServiceCollection services, string? authorityHostInstance, string? audienceDomain, string? tenantId, string? clientId, string? azureAppId)
        {
            authorityHostInstance = authorityHostInstance?.TrimEnd('/') ?? string.Empty;
            tenantId = tenantId?.Trim('/') ?? string.Empty;
            clientId = clientId?.Trim('/') ?? string.Empty;
            azureAppId = azureAppId?.Trim('/') ?? string.Empty;

            return services
                .AddSingleton(provider =>
                {
                    var result = new OpenApiSecurityScheme
                    {
                        Name = "bearer",
                        Type = SecuritySchemeType.OAuth2,
                        Description = "Azure AAD Authentication",
                        Flows = new OpenApiOAuthFlows()
                        {
                            Implicit = new OpenApiOAuthFlow()
                            {
                                Scopes = new Dictionary<string, string>
                                {
                                    { $"https://{audienceDomain}/{azureAppId}/Access", "Access Application" }
                                },
                                AuthorizationUrl = new Uri($"{authorityHostInstance}/{tenantId}/oauth2/v2.0/authorize"),
                                TokenUrl = new Uri($"{authorityHostInstance}/{tenantId}/oauth2/v2.0/token"),
                            }
                        }
                    };
                    return result;
                })
                .AddSingleton(provider =>
                {
                    var result = new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" }
                            },
                            new string[] {}
                        }
                    };
                    return result;
                })
                .AddAuthentication(options => options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // Authority will be Your AzureAd Instance and Tenant Id
                        options.Authority = $"{authorityHostInstance}/{tenantId}/v2.0";

                        // The valid audiences are both the Client id(options.Audience) and api://{ClientID}
                        options.TokenValidationParameters.ValidAudiences = new string[] {
                            $"api://{clientId}",
                            $"https://{audienceDomain}/{azureAppId}",
                            $"https://{audienceDomain}/{clientId}",
                            clientId
                        };

                        options.TokenValidationParameters.ValidIssuers = new string[] {
                            $"https://{authorityHostInstance}/{tenantId}/",
                            $"https://sts.windows.net/{tenantId}/"
                        };

                        options.MapInboundClaims = false; //avoid rewriting JWToken claims to older style claim types..
                    });
        }
    }
}