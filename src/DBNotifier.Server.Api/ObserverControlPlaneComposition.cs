// Module purpose: Registers only the dormant MOD-12 control plane and unavailable activation authority in normal server composition.
using DBNotifier.Application.AIOps;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DBNotifier.Server.Api;

/// <summary>Composes the normal fail-closed MOD-12 control-plane boundary without stores, evaluators or publishers.</summary>
public static class ObserverControlPlaneComposition
{
    /// <summary>Registers the inactive coordinator and deliberately unavailable activation authority.</summary>
    /// <param name="services">Normal server service collection.</param>
    /// <returns>The same collection for composition chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDormantObserverControlPlane(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IObserverActivationAuthority, UnavailableObserverActivationAuthority>();
        services.TryAddSingleton<IObserverControlPlane, DormantObserverControlPlane>();
        return services;
    }
}
