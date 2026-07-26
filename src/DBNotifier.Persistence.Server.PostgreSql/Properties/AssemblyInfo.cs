// Module purpose: Grants unit tests access to internal PostgreSQL persistence safety primitives without widening product APIs.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DBNotifier.UnitTests")]
