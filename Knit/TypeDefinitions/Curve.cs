// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class Curve<T> : CurveValue where T : unmanaged {
	[GrannyMember("OneOverKnotScaleTrunc")] public ushort OneOverKnotScale { get; set; }
	public float[] ControlScaleOffsets { get; set; } = [];
	public T[] KnotsControls { get; set; } = [];
}
