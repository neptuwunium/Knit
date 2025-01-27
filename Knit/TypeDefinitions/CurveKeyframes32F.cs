// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class CurveKeyframes32F : CurveValue {
	public short Dimension { get; set; }
	public float[] Controls { get; set; } = [];
}
