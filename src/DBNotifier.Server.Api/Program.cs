// Module purpose: Composes the authorised server API, protected identity boundaries and central persistence services.
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Operations;
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
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
builder.Services.AddScoped<IAgentFleetStore, AgentFleetStore>();
builder.Services.AddScoped<AgentFleetService>();
builder.Services.AddSingleton<IAgentCertificateIssuer, UnavailableAgentCertificateIssuer>();
builder.Services.AddScoped<IObservationIngestionStore, ServerObservationIngestionStore>();
builder.Services.AddScoped<ObservationBatchIngestor>();
builder.Services.AddScoped<IServerCommandDeliveryStore, ServerCommandDeliveryStore>();
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
app.MapPost(
        "/api/v1/instances/{instanceId:guid}/commands",
        async Task<IResult> (
            Guid instanceId,
            CreateAdministrativeCommandRequest request,
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

            CommandCreationResult result = await operations
                .CreateCommandAsync(actor, instanceId, request, cancellationToken)
                .ConfigureAwait(false);
            return result.Disposition switch
            {
                CommandCreationDisposition.Created => Results.Created(
                    $"/api/v1/commands/{result.Command!.CommandId:D}",
                    result.Command),
                CommandCreationDisposition.Duplicate => Results.Ok(result.Command),
                CommandCreationDisposition.Denied => Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Command creation denied",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode }),
                CommandCreationDisposition.IdempotencyConflict => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Command idempotency conflict",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode }),
                CommandCreationDisposition.CapabilityUnavailable => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Administrative capability unavailable",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode }),
                _ => Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid command request",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode }),
            };
        })
    .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
    .RequireRateLimiting("HumanApiRateLimit");
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
app.MapPost(
        "/api/v1/agents/{agentId:guid}/commands:poll",
        async Task<IResult> (
            Guid agentId,
            CommandPollRequest request,
            IServerCommandDeliveryStore store,
            TimeProvider timeProvider,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!HasCommandProtocolVersion(httpContext, request.SchemaVersion) || request.AgentId != agentId ||
                !string.Equals(httpContext.Request.Headers["DBN-Agent-Version"], request.AgentVersion, StringComparison.Ordinal))
            {
                return ProtocolOrPayloadProblem(request.AgentId == agentId);
            }

            try
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                IReadOnlyList<CommandEnvelope> commands = await store
                    .PollAsync(request, now, cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new CommandPollResponse(
                    Guid.NewGuid(), 1, agentId, request.Sequence, now, timeProvider.GetUtcNow(), commands));
            }
            catch (ArgumentException)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid command poll", extensions: new Dictionary<string, object?>
                    { ["code"] = "command.poll_invalid" });
            }
        })
    .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
    .RequireRateLimiting("AgentApiRateLimit");
app.MapPost(
        "/api/v1/agents/{agentId:guid}/commands:ack",
        async Task<IResult> (
            Guid agentId,
            CommandAcknowledgementRequest request,
            IServerCommandDeliveryStore store,
            TimeProvider timeProvider,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!HasCommandProtocolVersion(httpContext, request.SchemaVersion) || request.AgentId != agentId)
            {
                return ProtocolOrPayloadProblem(request.AgentId == agentId);
            }

            try
            {
                IReadOnlyList<CommandAcknowledgementResult> results = await store
                    .AcknowledgeAsync(request, timeProvider.GetUtcNow(), cancellationToken)
                    .ConfigureAwait(false);
                return Results.Ok(new CommandAcknowledgementResponse(results));
            }
            catch (ArgumentException)
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid command acknowledgement", extensions: new Dictionary<string, object?>
                    { ["code"] = "command.ack_invalid" });
            }
        })
    .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
    .RequireRateLimiting("AgentApiRateLimit");
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
    .RequireAuthorization(ApiSecurityDefaults.AgentObservationIngestionPolicy)
    .RequireRateLimiting("AgentApiRateLimit");
app.MapAgentFleetEndpoints();

app.Run();

static bool HasCommandProtocolVersion(HttpContext context, int schemaVersion) =>
    schemaVersion == 1 &&
    context.Request.Headers.TryGetValue("DBN-Protocol-Version", out var protocol) &&
    protocol.Count == 1 && protocol[0] == "1" &&
    context.Request.Headers.TryGetValue("DBN-Message-Schema", out var schema) &&
    schema.Count == 1 && schema[0] == "1" &&
    context.Request.Headers.TryGetValue("DBN-Agent-Version", out var agentVersion) &&
    agentVersion.Count == 1 && !string.IsNullOrWhiteSpace(agentVersion[0]);

static IResult ProtocolOrPayloadProblem(bool agentMatches) => agentMatches
    ? Results.Problem(statusCode: StatusCodes.Status426UpgradeRequired,
        title: "Unsupported DB-Notifier command protocol version",
        extensions: new Dictionary<string, object?> { ["code"] = "protocol.command_version_unsupported" })
    : Results.Problem(statusCode: StatusCodes.Status400BadRequest,
        title: "Command Agent does not match the authorized route",
        extensions: new Dictionary<string, object?> { ["code"] = "command.agent_mismatch" });

/// <summary>Exposes the server API entry-point marker for local integration testing.</summary>
public partial class Program;
