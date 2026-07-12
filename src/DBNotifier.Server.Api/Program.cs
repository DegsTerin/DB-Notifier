using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576;
    options.ConfigureHttpsDefaults(httpsOptions =>
        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate);
});
builder.Services.AddProblemDetails();
builder.Services.AddDbContextFactory<ServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ServerDatabase")));
builder.Services.AddScoped<IObservationIngestionStore, ServerObservationIngestionStore>();
builder.Services.AddScoped<ObservationBatchIngestor>();
builder.Services.AddScoped<AgentCertificateIdentityValidator>();
builder.Services
    .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.Chained;
        options.ValidateCertificateUse = true;
        options.ValidateValidityPeriod = true;
        options.RevocationMode = X509RevocationMode.Online;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = async context =>
            {
                try
                {
                    AgentCertificateIdentityValidator validator = context.HttpContext.RequestServices
                        .GetRequiredService<AgentCertificateIdentityValidator>();
                    Guid? agentId = await validator
                        .ValidateAsync(context.ClientCertificate, context.HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                    if (agentId is null)
                    {
                        context.Fail("The Agent certificate is not enrolled or active.");
                        return;
                    }

                    ClaimsIdentity identity = new(
                        [new Claim(AgentIdentityClaimTypes.AgentId, agentId.Value.ToString("D"))],
                        context.Scheme.Name);
                    context.Principal = new ClaimsPrincipal(identity);
                    context.Success();
                }
                catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    context.Fail("Agent certificate validation was cancelled.");
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    context.Fail("Agent certificate validation is unavailable.");
                }
            },
        };
    });
builder.Services.AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AgentObservationIngestion", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AgentRouteRequirement());
    });

WebApplication app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));
app.MapPost(
        "/api/v1/agents/{agentId:guid}/observations:batch",
        async Task<IResult> (
            Guid agentId,
            ObservationBatchRequest request,
            ObservationBatchIngestor ingestor,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!httpContext.Request.Headers.TryGetValue("DBN-Protocol-Version", out var protocol) ||
                protocol.Count != 1 || protocol[0] != "1")
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status426UpgradeRequired,
                    title: "Unsupported DB-Notifier protocol version",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "protocol.version_unsupported",
                        ["retryable"] = false,
                    });
            }

            try
            {
                if (request.AgentId != agentId)
                {
                    throw new ArgumentException("The observation batch Agent does not match the authorized route.");
                }

                ObservationBatchResult result = await ingestor
                    .HandleAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (ArgumentException)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid observation batch",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "observation.batch_invalid",
                        ["retryable"] = false,
                    });
            }
        })
    .RequireAuthorization("AgentObservationIngestion");

app.Run();

public partial class Program;
