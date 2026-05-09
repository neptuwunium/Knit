// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Pluto.IO;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x10)]
public record struct Granny2TypeDefinition<T> : IGranny2TypeDefinition where T : ISignedNumber<T> {
	public Granny2MemberType Type { get; set; }
	public T NameOffset { get; set; }
	public T ChildrenOffset { get; set; }
	public int ArraySize { get; set; }
	public GrannyCustomTypeInfo CustomTypeInfo { get; set; }
	public T TraversalId { get; set; }
	int IGranny2TypeDefinition.NameOffset => int.CreateChecked(NameOffset);
	int IGranny2TypeDefinition.ChildrenOffset => int.CreateChecked(ChildrenOffset);

	public SpanPointer GetTypeDefinition(Granny2File gr) => gr.Resolve(int.CreateChecked(ChildrenOffset));

	public int GetSize(Granny2File gr) {
		return Type switch {
			       Granny2MemberType.End => 0,
			       Granny2MemberType.Inline => gr.CalculateTypeSize<T>(GetTypeDefinition(gr)),
			       Granny2MemberType.Reference => gr.Header.Is64Bit ? 8 : 4,
			       Granny2MemberType.ReferenceToArray => (gr.Header.Is64Bit ? 8 : 4) + 4,
			       Granny2MemberType.ArrayOfReferences => (gr.Header.Is64Bit ? 8 : 4) + 4,
			       Granny2MemberType.VariantReference => (gr.Header.Is64Bit ? 8 : 4) << 1,
			       Granny2MemberType.SwitchableType => 0,
			       Granny2MemberType.ReferenceToVariantArray => ((gr.Header.Is64Bit ? 8 : 4) << 1) + 4,
			       Granny2MemberType.String => gr.Header.Is64Bit ? 8 : 4,
			       Granny2MemberType.Transform => 68,
			       Granny2MemberType.Real32 => 4,
			       Granny2MemberType.Int8 => 1,
			       Granny2MemberType.UInt8 => 1,
			       Granny2MemberType.BinormalInt8 => 1,
			       Granny2MemberType.NormalUInt8 => 1,
			       Granny2MemberType.Int16 => 2,
			       Granny2MemberType.UInt16 => 2,
			       Granny2MemberType.BinormalInt16 => 2,
			       Granny2MemberType.NormalUInt16 => 2,
			       Granny2MemberType.Int32 => 4,
			       Granny2MemberType.UInt32 => 4,
			       Granny2MemberType.Real16 => 2,
			       Granny2MemberType.EmptyReference => gr.Header.Is64Bit ? 8 : 4,
			       _ => throw new InvalidDataException(),
		       };
	}

	public int GetTotalSize(Granny2File gr) => GetSize(gr) * Math.Max(1, ArraySize);

	public string GetName(Granny2File gr) {
		var offset = int.CreateChecked(NameOffset);
		if (offset == 0) {
			return string.Empty;
		}

		var ptr = gr.Resolve(offset);
		var end = ptr.Span.IndexOf((byte) 0);
		return Encoding.ASCII.GetString(ptr.Span[..end]);
	}

	public IGranny2TypeDefinition Single() => this with {
		ArraySize = 0,
	};
}
