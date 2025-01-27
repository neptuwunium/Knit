// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;
using Knit.Meta;

namespace Knit.TypeDefinitions;

public abstract class CurveValue : IGrannyType {
	public CurveDataHeader Header { get; set; }
	[GrannyIgnoreMember] public string Type { get; set; } = "Curve";

	public virtual bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		if (name.StartsWith("CurveDataHeader_")) {
			Header = MemoryMarshal.Read<CurveDataHeader>(objectLocation);
			Type = name[16..];
			return true;
		}

		return false;
	}

	public static Type SelectVariant(List<Granny2TinyType> members) {
		return members[0].Name switch {
			       "CurveDataHeader_DaKeyframes32f" => typeof(CurveKeyframes32F),
			       "CurveDataHeader_DaK32fC32f" => typeof(CurveDX32F),
			       "CurveDataHeader_DaIdentity" => typeof(CurveIdentity),
			       "CurveDataHeader_DaConstant32f" => typeof(CurveConstant32F),
			       "CurveDataHeader_D3Constant32f" => typeof(CurveD3Constant32F),
			       "CurveDataHeader_D4Constant32f" => typeof(CurveD4Constant32F),
			       "CurveDataHeader_DaK16uC16u" => typeof(Curve<ushort>),
			       "CurveDataHeader_DaK8uC8u" => typeof(Curve<byte>),
			       "CurveDataHeader_D4nK16uC15u" => typeof(CurveD4<ushort>),
			       "CurveDataHeader_D4nK8uC7u" => typeof(CurveD4<byte>),
			       "CurveDataHeader_D3K16uC16u" => typeof(CurveD3<ushort>),
			       "CurveDataHeader_D3K8uC8u" => typeof(CurveD3<byte>),
			       "CurveDataHeader_D9I1K16uC16u" => typeof(CurveD9I1<ushort>),
			       "CurveDataHeader_D9I3K16uC16u" => typeof(CurveD9I3<ushort>),
			       "CurveDataHeader_D9I1K8uC8u" => typeof(CurveD9I1<byte>),
			       "CurveDataHeader_D9I3K8uC8u" => typeof(CurveD9I3<byte>),
			       "CurveDataHeader_D3I1K32fC32f" => typeof(CurveD9I3<float>),
			       "CurveDataHeader_D3I1K16uC16u" => typeof(CurveD9I3<ushort>),
			       "CurveDataHeader_D3I1K8uC8u" => typeof(CurveD9I3<byte>),
			       _ => typeof(CurveFallback),
		       };
	}
}
