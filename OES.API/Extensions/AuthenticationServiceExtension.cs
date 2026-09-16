using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace OES.API.Extensions
{
    public static class AuthenticationServiceExtension
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = true,
                    ValidAudiences = configuration["JWT:ValidAudiences"]!.Split(", "),
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"])),
                    ValidateLifetime = true,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) &&
                           (path.StartsWithSegments("/notificationHub") ||
                            path.StartsWithSegments("/syncDashboardHub"))
                        )
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                    //,
                    //OnTokenValidated = async context =>
                    //{
                    //    var request = context.HttpContext.Request;

                    //    var currentIp = request.Headers[MiscConstants.XOriginalClientIpHeader].FirstOrDefault()
                    //        ?? request.Headers[MiscConstants.CfConnectingIpHeader].FirstOrDefault()
                    //        ?? request.Headers[MiscConstants.XForwardedForHeader].FirstOrDefault()?.Split(',')[0].Trim()
                    //        ?? context.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                    //        ?? string.Empty;

                    //    var tokenIp = context.Principal?.FindFirst(CustomJwtClaimsTypes.DeviceIpAddress)?.Value;

                    //    if (!string.Equals(currentIp, tokenIp, StringComparison.Ordinal))
                    //    {
                    //        context.Fail("IP address mismatch.");
                    //    }
                    //}
                };
            });

            return services;
        }
    }
}