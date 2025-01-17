﻿using NSwag.CodeGeneration.TypeScript;

namespace Gears.Host.FastEndpoints;

internal sealed class SwaggerSettings
{
    public bool IsEnabled { get; init; }
}

public sealed class JwtSettings
{
    public string Key { get; set; }
    public int DurationInSeconds { get; set; }
}

internal static class FastEndpointsConfiguration
{
    private const string DocumentName = "default";

    public static WebApplicationBuilder AddFastEndpointsServices(this WebApplicationBuilder builder)
    {
        var key = builder.Configuration.GetValue<string>($"Jwt:{nameof(JwtSettings.Key)}");

        builder.Services
            .Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"))
            .Configure<SwaggerSettings>(builder.Configuration.GetSection("Swagger"))
            .AddFastEndpoints(x =>
            {
                x.DisableAutoDiscovery = true;
                x.Assemblies = new[] { typeof(Application.Features.Users.GetAllUsersEndpoint).Assembly };
            })
            .AddJWTBearerAuth(key)
            .SwaggerDocument(x =>
            {
                x.ShortSchemaNames = true;
                x.DocumentSettings = s =>
                {
                    s.DocumentName = DocumentName;
                };
            })
            .RegisterServicesFromGearsHost();

        return builder;
    }

    public static IApplicationBuilder AddFastEndpointsMiddleware(this IApplicationBuilder builder)
    {
        builder.UseFastEndpoints(x =>
        {
            x.Endpoints.ShortNames = true;
            x.Errors.ProducesMetadataType = typeof(ProblemDetails);
            x.Errors.UseProblemDetails();
        });

        var options = builder.ApplicationServices.GetRequiredService<IOptions<SwaggerSettings>>();
        if (options.Value.IsEnabled)
        {
            builder.UseSwaggerGen();
        }

        return builder;
    }

    public static WebApplication AddGeneratedClientEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<SwaggerSettings>>();
        if (options.Value.IsEnabled)
        {
            app.MapCSharpClientEndpoint("/cs-client", DocumentName, s =>
            {
                s.ClassName = "ApiClient";
                s.CSharpGeneratorSettings.Namespace = "Gears";
            });

            app.MapTypeScriptClientEndpoint("/ts-angular-client", DocumentName, s =>
            {
                s.ClassName = "ApiClient";
                s.InjectionTokenType = InjectionTokenType.InjectionToken;
                s.RxJsVersion = 7.8M;
                s.Template = TypeScriptTemplate.Angular;
                s.TypeScriptGeneratorSettings.Namespace = string.Empty;
                s.TypeScriptGeneratorSettings.TypeScriptVersion = 5.3M;
                s.UseSingletonProvider = true;
            });
        }

        return app;
    }
}