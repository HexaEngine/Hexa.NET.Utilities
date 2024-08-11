; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MNF001 | MemoryManagement | Warning | MemberNotFreedAnalyzer
PFA001 | MemoryManagement | Warning | PointerFreeAnalyzer
UF001 | MemoryManagement | Warning | UnreleasedIFreeableAnalyzer
RFS001 | MemoryManagement | Warning | ReadonlyFreeableStructAnalyzer