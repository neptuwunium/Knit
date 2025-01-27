// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Animation : GrannyExtendable {
	public string Name { get; set; } = "";
	public float Duration { get; set; }
	public float TimeStep { get; set; }
	public float Oversampling { get; set; }
	public List<TrackGroup> TrackGroups { get; set; } = [];
	public int DefaultLoopCount { get; set; }
	public uint Flags { get; set; }
}
