using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace CSharply;

public class ServerService
{
    private readonly WebApplication _app;

    public ServerService(int port)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");

        // Add services
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            )
        );

        _app = builder.Build();
        _app.UseCors();
        ConfigureRoutes();
    }

    public void Run()
    {
        _app.Run();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await _app.RunAsync(cancellationToken);
    }

    private void ConfigureRoutes()
    {
        _app.MapGet("/health", () => string.Empty).WithName("health");

        _app.MapPost(
                "/organize",
                async context =>
                {
                    try
                    {
                        using StreamReader reader = new(context.Request.Body);
                        string code = await reader.ReadToEndAsync();

                        if (
                            context.Request.Headers.TryGetValue(
                                "x-file-path",
                                out StringValues headerValue
                            )
                        )
                        {
                            string? filePath = headerValue.FirstOrDefault() ?? string.Empty;
                            IgnoreFileService ignoreService = new();
                            if (filePath != null && ignoreService.Ignore(new FileInfo(filePath)))
                            {
                                context.Response.Headers.Append("x-outcome", "ignored");
                                await context.Response.WriteAsync(code);
                                return;
                            }
                        }

                        context.Response.Headers.Append("x-outcome", "organized");
                        code = OrganizeService.OrganizeCode(code);
                        await context.Response.WriteAsync(code);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync(ex.Message);
                    }
                }
            )
            .WithName("organize");
    }
}
