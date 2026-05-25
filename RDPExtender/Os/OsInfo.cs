namespace RDPExtender.Os;

internal sealed record OsInfo(
    string CurrentBuild,       // e.g. 26100
    string BuildRevision,      // e.g. 2892
    string FullOsBuild,        // e.g. 26100.2892
    string DisplayVersion,     // e.g. 24H2
    string InstallationType);  // e.g. Client
