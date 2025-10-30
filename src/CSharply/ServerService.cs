using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CSharply;

public class ServerService
{
    private readonly WebApplication _app;
    private readonly IgnoreFileService _ignoreService = new();

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
                async (HttpContext context) =>
                {
                    try
                    {
                        using StreamReader reader = new(context.Request.Body);
                        string code = await reader.ReadToEndAsync();

                        return OrganizeService.OrganizeCode(code);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        return ex.Message;
                    }
                }
            )
            .WithName("organize")
            .Accepts<string>("text/plain");

        _app.MapPost(
                "/ignore",
                async (HttpContext context) =>
                {
                    try
                    {
                        using StreamReader reader = new(context.Request.Body);
                        string filePath = await reader.ReadToEndAsync();

                        FileInfo file = new(filePath);
                        if (!file.Exists)
                            return "invalid";

                        return _ignoreService.Ignore(file) ? "true" : "false";
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        return ex.Message;
                    }
                }
            )
            .WithName("ignore")
            .Accepts<string>("text/plain");
    }
}
