// Module purpose: Grants test assemblies access to isolated PostgreSQL provider seams without widening product APIs.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DBNotifier.IntegrationTests")]
[assembly: InternalsVisibleTo("DBNotifier.UnitTests")]
