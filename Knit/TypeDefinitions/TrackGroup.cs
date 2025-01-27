// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class TrackGroup : GrannyExtendable {
	public string Name { get; set; } = "";
	public List<VectorTrack> VectorTracks { get; set; } = [];
	public List<TransformTrack> TransformTracks { get; set; } = [];
	public float[] TransformLODErrors { get; set; } = [];
	public List<TextTrack> TextTracks { get; set; } = [];
	public XForm InitialPlacement { get; set; }
	public AccumulationFlags AccumulationFlags { get; set; }
	public Vector3 LoopTranslation { get; set; }
	public PeriodicLoop? PeriodicLoop { get; set; }
}
