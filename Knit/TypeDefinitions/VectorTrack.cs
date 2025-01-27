// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class VectorTrack {
	public string Name { get; set; } = "";
	public uint TrackKey { get; set; }
	public int Dimension { get; set; }
	public CurveData? ValueCurve { get; set; }
}
