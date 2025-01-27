// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class CurveConstant32F : CurveValue {
	public short Padding { get; set; }
	public float[] Controls { get; set; } = [];
}
