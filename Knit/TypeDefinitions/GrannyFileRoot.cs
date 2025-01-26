namespace Knit.TypeDefinitions;

public class GrannyFileRoot : MarshalledGrannyType {
	[GrannyMember] public ArtToolInfo ArtToolInfo { get; set; } = new();
	[GrannyMember] public ExporterInfo ExporterInfo { get; set; } = new();
	[GrannyMember("FromFileName")] public string Name { get; set; } = "";
}
