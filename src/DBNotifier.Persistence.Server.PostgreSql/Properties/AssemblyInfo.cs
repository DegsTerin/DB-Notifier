// Module purpose: Grants test assemblies access to internal PostgreSQL persistence seams without widening product APIs.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DBNotifier.IntegrationTests")]
[assembly: InternalsVisibleTo("DBNotifier.UnitTests")]
