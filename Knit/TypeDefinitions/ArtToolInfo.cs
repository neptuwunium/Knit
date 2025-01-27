// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class ArtToolInfo : GrannyExtendable {
	[GrannyMember("FromArtToolName")] public string Name { get; set; } = "";
	[GrannyMember("ArtToolMajorRevision")] public int MajorRevision { get; set; }
	[GrannyMember("ArtToolMinorRevision")] public int MinorRevision { get; set; }
	[GrannyMember("ArtToolPointerSize")] public int PointerSize { get; set; }
	public float UnitsPerMeter { get; set; }
	public Vector3 Origin { get; set; }
	public Vector3 RightVector { get; set; }
	public Vector3 UpVector { get; set; }
	public Vector3 BackVector { get; set; }
}
