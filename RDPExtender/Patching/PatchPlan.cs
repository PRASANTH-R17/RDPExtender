using System.Text.RegularExpressions;

namespace RDPExtender.Patching;

internal sealed record PatchPlan(Regex Pattern, string Replacement);

internal enum PatchAssessment
{
    NeedsPatch,
    AlreadyPatched,
    UnsupportedOperatingSystem,
    PatternNotFound
}
