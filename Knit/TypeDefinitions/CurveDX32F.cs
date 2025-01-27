// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

namespace Knit.TypeDefinitions;

public class CurveDX32F : CurveValue {
	public short Padding { get; set; }
	public float[] Knots { get; set; } = [];
	public float[] Controls { get; set; } = [];
}
