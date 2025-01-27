// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;

namespace Knit.TypeDefinitions;

public class CurveD4Constant32F : CurveValue {
	public short Padding { get; set; }
	public Vector4 Controls { get; set; }
}
