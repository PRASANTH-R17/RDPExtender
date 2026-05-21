namespace RDPExtender.Os;

internal sealed record OsInfo(
    string CurrentBuild,
    string BuildRevision,
    string FullOsBuild,
    string DisplayVersion,
    string InstallationType);
