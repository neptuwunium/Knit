namespace Knit.TypeDefinitions;

public class ExporterInfo : MarshalledGrannyType {
	[GrannyMember("ExporterName")] public string Name { get; set; } = "";
	[GrannyMember("ExporterMajorRevision")] public int MajorRevision { get; set; }
	[GrannyMember("ExporterMinorRevision")] public int MinorRevision { get; set; }
	[GrannyMember("ExporterCustomization")] public int Customization { get; set; }
	[GrannyMember("ExporterBuildNumber")] public int BuildNumber { get; set; }
}
