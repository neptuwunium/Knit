// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class TransformTrack {
	public string Name { get; set; } = "";
	public TrackFlags Flags { get; set; }
	public CurveData? OrientationCurve { get; set; }
	public CurveData? PositionCurve { get; set; }
	[GrannyMember("ScaleShearCurve")] public CurveData? ScaleCurve { get; set; }
}
