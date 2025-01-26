using System.Numerics;

namespace Knit.TypeDefinitions;

public class ArtToolInfo : MarshalledGrannyType {
	[GrannyMember("FromArtToolName")] public string Name { get; set; } = "";
	[GrannyMember("ArtToolMajorRevision")] public int MajorRevision { get; set; }
	[GrannyMember("ArtToolMinorRevision")] public int MinorRevision { get; set; }
	[GrannyMember("ArtToolPointerSize")] public int PointerSize { get; set; }
	[GrannyMember] public float UnitsPerMeter { get; set; }
	[GrannyMember] public Vector3 Origin { get; set; }
	[GrannyMember] public Vector3 RightVector { get; set; }
	[GrannyMember] public Vector3 UpVector { get; set; }
	[GrannyMember] public Vector3 BackVector { get; set; }
}
