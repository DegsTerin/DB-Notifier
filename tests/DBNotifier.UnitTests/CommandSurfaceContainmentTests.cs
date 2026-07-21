// Module purpose: Proves the legacy v1 command tombstones reject every request before body binding or service resolution.
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DBNotifier.UnitTests;

/// <summary>Verifies stable, non-sensitive and persistence-free responses from the contained command surface.</summary>
public sealed class CommandSurfaceContainmentTests
{
    /// <summary>Proves each legacy route ignores its payload and returns the same fail-closed problem.</summary>
    /// <param name="routePattern">Contained route pattern selected from the fixed v1 inventory.</param>
    [Theory]
    [InlineData("/api/v1/instances/{instanceId:guid}/commands")]
    [InlineData("/api/v1/agents/{agentId:guid}/commands:poll")]
    [InlineData("/api/v1/agents/{agentId:guid}/commands:ack")]
    public async Task LegacyRouteReturnsStableUnavailableProblemWithoutBoundServices(string routePattern)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        await using WebApplication app = builder.Build();
        app.MapContainedCommandSurface();
        IEnumerable<RouteEndpoint> endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>();
        RouteEndpoint endpoint = Assert.Single(
            endpoints,
            candidate => string.Equals(candidate.RoutePattern.RawText, routePattern, StringComparison.Ordinal));
        await using MemoryStream responseBody = new();
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            Response = { Body = responseBody },
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{not-valid-json"));

        await endpoint.RequestDelegate!(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        responseBody.Position = 0;
        using JsonDocument problem = await JsonDocument.ParseAsync(responseBody);
        Assert.Equal(
            CommandSurfaceContainmentEndpointExtensions.UnavailableCode,
            problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(problem.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Empty(app.Services.GetServices<IServerCommandDeliveryStore>());
    }
}
