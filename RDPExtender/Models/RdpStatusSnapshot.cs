namespace RDPExtender.Models;

public sealed record StatusItem(string Label, StatusLevel Level, string Text);

public sealed record RdpStatusSnapshot(
    StatusItem OsCompatibility,
    StatusItem PatchState,
    StatusItem Backup,
    StatusItem RdpService,
    bool IsReady,
    string BottomBarMessage,
    StatusLevel BottomBarLevel);
