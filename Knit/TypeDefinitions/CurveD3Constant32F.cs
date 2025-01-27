// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveD3Constant32F : CurveValue {
	public short Padding { get; set; }
	public Vector3 Controls { get; set; }
}
