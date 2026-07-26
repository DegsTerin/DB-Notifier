// Module purpose: Grants test assemblies access to internal infrastructure seams without widening product APIs.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DBNotifier.IntegrationTests")]
[assembly: InternalsVisibleTo("DBNotifier.UnitTests")]
