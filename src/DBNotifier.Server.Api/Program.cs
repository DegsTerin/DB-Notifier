// Module purpose: Composes the authorised server API, protected identity boundaries and central persistence services.
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Operations;
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Server.Api;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ServerOperationsOptions serverOperationsOptions = new();
builder.Configuration.GetSection(ServerOperationsOptions.SectionName).Bind(serverOperationsOptions);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576;
    options.ConfigureHttpsDefaults(httpsOptions =>
        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate);
});
builder.Services.AddProblemDetails();
bool dashboardTvSandboxEnabled = builder.Services.AddDashboardTvSandbox(
    builder.Environment,
    builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("HumanApiRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy("AgentApiRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(AgentIdentityClaimTypes.AgentId) ??
                httpContext.Connection.RemoteIpAddress?.ToString() ??
                "unknown-agent",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy(AgentFleetEndpointRouteBuilderExtensions.EnrollmentRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-enrollment-client",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});
builder.Services.AddDbContextFactory<ServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ServerDatabase")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProviderRegistry>(_ => new ProviderRegistry([]));
builder.Services.AddSingleton<IAgentAssignmentValidator, AgentAssignmentValidator>();
builder.Services.AddScoped<IAgentFleetStore, AgentFleetStore>();
builder.Services.AddScoped<AgentFleetService>();
builder.Services.AddSingleton<IAgentCertificateIssuer, UnavailableAgentCertificateIssuer>();
builder.Services.AddScoped<IObservationIngestionStore, ServerObservationIngestionStore>();
builder.Services.AddScoped<ObservationBatchIngestor>();
builder.Services.AddScoped<IAuthorizedOperationsStore, AuthorizedOperationsStore>();
builder.Services.AddScoped<AuthorizedOperationsService>();
builder.Services.AddSingleton(serverOperationsOptions);
builder.Services.AddSingleton<ServerMaintenanceStore>();
builder.Services.AddSingleton<IServerRetentionStore>(services => services.GetRequiredService<ServerMaintenanceStore>());
builder.Services.AddSingleton<IServerOutboxStore>(services => services.GetRequiredService<ServerMaintenanceStore>());
builder.Services.AddSingleton<INotificationDeliveryStore>(services => services.GetRequiredService<ServerMaintenanceStore>());
builder.Services.AddSingleton<IServerMessagePublisher, UnavailableServerMessagePublisher>();
builder.Services.AddHostedService<ServerMaintenanceWorker>();
builder.Services.AddScoped<AgentCertificateIdentityValidator>();
builder.Services.AddSingleton<IAuthenticationAuditGate, AuthenticationAuditGate>();
builder.Services.AddScoped<AuthenticationAuditWriter>();
builder.Services.AddSingleton<HumanActorResolver>();
builder.Services
    .AddAuthentication(ApiSecurityDefaults.RoutedAuthenticationScheme)
    .AddPolicyScheme(
        ApiSecurityDefaults.RoutedAuthenticationScheme,
        ApiSecurityDefaults.RoutedAuthenticationScheme,
        options => options.ForwardDefaultSelector = ApiSecurityDefaults.SelectAuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, PublicEndpointAuthenticationHandler>(
        ApiSecurityDefaults.PublicEndpointAuthenticationScheme,
        _ => { })
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
                        AuthenticationAuditWriter audit = context.HttpContext.RequestServices
                            .GetRequiredService<AuthenticationAuditWriter>();
                        await audit.TryWriteAsync(
                            new AuthenticationAuditEvent(
                                "Agent",
                                "unresolved",
                                context.Scheme.Name,
                                "Denied",
                                "authentication.agent_not_enrolled"),
                            context.HttpContext.RequestAborted).ConfigureAwait(false);
                        context.Fail("The Agent certificate is not enrolled or active.");
                        return;
                    }

                    ClaimsIdentity identity = new(
                        [new Claim(AgentIdentityClaimTypes.AgentId, agentId.Value.ToString("D"))],
                        context.Scheme.Name);
                    context.Principal = new ClaimsPrincipal(identity);
                    AuthenticationAuditWriter successAudit = context.HttpContext.RequestServices
                        .GetRequiredService<AuthenticationAuditWriter>();
                    await successAudit.TryWriteAsync(
                        new AuthenticationAuditEvent(
                            "Agent",
                            agentId.Value.ToString("D"),
                            context.Scheme.Name,
                            "Succeeded",
                            "authentication.agent_certificate_valid"),
                        context.HttpContext.RequestAborted).ConfigureAwait(false);
                    context.Success();
                }
                catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    context.Fail("Agent certificate validation was cancelled.");
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    AuthenticationAuditWriter unavailableAudit = context.HttpContext.RequestServices
                        .GetRequiredService<AuthenticationAuditWriter>();
                    await unavailableAudit.TryWriteAsync(
                        new AuthenticationAuditEvent(
                            "Agent",
                            "unresolved",
                            context.Scheme.Name,
                            "Unknown",
                            "authentication.agent_validation_unavailable"),
                        context.HttpContext.RequestAborted).ConfigureAwait(false);
                    context.Fail("Agent certificate validation is unavailable.");
                }
            },
            OnAuthenticationFailed = async context =>
            {
                AuthenticationAuditWriter audit = context.HttpContext.RequestServices
                    .GetRequiredService<AuthenticationAuditWriter>();
                await audit.TryWriteAsync(
                    new AuthenticationAuditEvent(
                        "Agent",
                        "unresolved",
                        context.Scheme.Name,
                        "Failed",
                        "authentication.agent_certificate_invalid"),
                    context.HttpContext.RequestAborted).ConfigureAwait(false);
            },
        };
    })
    .AddJwtBearer(HumanAuthenticationDefaults.Scheme, options =>
    {
        string? authority = builder.Configuration["HumanAuthentication:Authority"];
        if (!string.IsNullOrWhiteSpace(authority))
        {
            if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri) ||
                authorityUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(authorityUri.UserInfo) ||
                !string.IsNullOrEmpty(authorityUri.Query) ||
                !string.IsNullOrEmpty(authorityUri.Fragment))
            {
                throw new InvalidOperationException(
                    "Human authentication authority must be an absolute HTTPS URI without user info, query, or fragment.");
            }

            options.Authority = authorityUri.AbsoluteUri.TrimEnd('/');
        }

        string? audience = builder.Configuration["HumanAuthentication:Audience"];
        options.Audience = string.IsNullOrWhiteSpace(audience) ? null : audience;
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string actorId = context.Principal?.FindFirstValue("sub") ?? "unresolved";
                AuthenticationAuditWriter audit = context.HttpContext.RequestServices
                    .GetRequiredService<AuthenticationAuditWriter>();
                await audit.TryWriteAsync(
                    new AuthenticationAuditEvent(
                        "Human",
                        actorId.Length <= 300 ? actorId : "unresolved",
                        context.Scheme.Name,
                        "Succeeded",
                        "authentication.human_token_valid"),
                    context.HttpContext.RequestAborted).ConfigureAwait(false);
            },
            OnAuthenticationFailed = async context =>
            {
                AuthenticationAuditWriter audit = context.HttpContext.RequestServices
                    .GetRequiredService<AuthenticationAuditWriter>();
                await audit.TryWriteAsync(
                    new AuthenticationAuditEvent(
                        "Human",
                        "unresolved",
                        context.Scheme.Name,
                        "Failed",
                        "authentication.human_token_invalid"),
                    context.HttpContext.RequestAborted).ConfigureAwait(false);
            },
        };
    });
builder.Services.AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiSecurityDefaults.AgentObservationIngestionPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AgentRouteRequirement());
    })
    .AddPolicy(ApiSecurityDefaults.AgentApiPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AgentRouteRequirement());
    })
    .AddPolicy(ApiSecurityDefaults.HumanApiPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(HumanAuthenticationDefaults.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("sub");
    });

WebApplication app = builder.Build();
app.UseRouting();
app.UseMiddleware<ProtectedTransportMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));
app.MapDashboardTvSandboxEndpoint(dashboardTvSandboxEnabled);
app.MapGet(
        "/api/v1/catalog/instances",
        async Task<IResult> (
            AuthorizedOperationsService operations,
            HumanActorResolver actorResolver,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            HumanActor? actor = actorResolver.Resolve(httpContext.User);
            if (actor is null)
            {
                return Results.Unauthorized();
            }

            IReadOnlyList<AuthorizedInstance> instances = await operations
                .GetCatalogAsync(actor, cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(instances);
        })
    .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
    .RequireRateLimiting("HumanApiRateLimit");
app.MapContainedCommandSurface();
app.MapGet(
        "/api/v1/audit",
        async Task<IResult> (
            int? offset,
            int? pageSize,
            DateTimeOffset? snapshotAt,
            string? actorId,
            string? action,
            string? outcome,
            AuthorizedOperationsService operations,
            HumanActorResolver actorResolver,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            HumanActor? actor = actorResolver.Resolve(httpContext.User);
            if (actor is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                AuthorizedAuditPage page = await operations.QueryAuditAsync(
                    actor,
                    new AuditQuery(offset ?? 0, pageSize ?? 50, snapshotAt, actorId, action, outcome),
                    cancellationToken).ConfigureAwait(false);
                return page.Authorized
                    ? Results.Ok(page)
                    : Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Audit query denied",
                        extensions: new Dictionary<string, object?> { ["code"] = "authorization.denied" });
            }
            catch (ArgumentException)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid audit query",
                    extensions: new Dictionary<string, object?> { ["code"] = "audit.query_invalid" });
            }
        })
    .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
    .RequireRateLimiting("HumanApiRateLimit");
app.MapObservationIngestionEndpoint();
app.MapAgentFleetEndpoints();

app.Run();

/// <summary>Exposes the server API entry-point marker for local integration testing.</summary>
public partial class Program;
