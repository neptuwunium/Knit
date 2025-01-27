// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Knit.Meta;

namespace Knit.TypeDefinitions;

public class CurveFallback : CurveValue {
	public float[] Knots { get; set; } = [];
	public float[] Controls { get; set; } = [];

	public override bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		if (name == "Degree") {
			Header = new CurveDataHeader {
				Format = CurveFormat.Unknown,
				Degree = MemoryMarshal.Read<byte>(objectLocation),
			};
			return true;
		}

		return base.Visit(file, name, typeInfo, objectLocation);
	}
}
