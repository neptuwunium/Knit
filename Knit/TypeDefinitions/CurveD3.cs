// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveD3<T> : CurveValue where T : unmanaged {
	[GrannyMember("OneOverKnotScaleTrunc")] public ushort OneOverKnotScale { get; set; }
	public Vector3 ControlScales { get; set; }
	public Vector3 ControlOffsets { get; set; }
	public T[] KnotsControls { get; set; } = [];
}
