// Module purpose: Grants test-only sandbox assemblies access to isolated command-transport persistence internals.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DBNotifier.AgentFleet.SandboxHost")]
[assembly: InternalsVisibleTo("DBNotifier.IntegrationTests")]
[assembly: InternalsVisibleTo("DBNotifier.UnitTests")]
