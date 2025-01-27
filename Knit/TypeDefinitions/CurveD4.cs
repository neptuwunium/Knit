// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class CurveD4<T> : CurveValue where T : unmanaged {
	public ushort ScaleOffsetTableEntries { get; set; }
	public float OneOverKnotScale { get; set; }
	public T[] KnotsControls { get; set; } = [];
}
