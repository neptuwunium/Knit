// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Runtime.InteropServices;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x2C)]
public record struct Granny2Section {
	public Granny2CompressionType Compression { get; set; }
	public Granny2SectionPointer Data { get; set; }
	public int UncompressedSize { get; set; }
	public uint Alignment { get; set; }
	public int CompressionBits1 { get; set; }
	public int CompressionBits2 { get; set; }
	public Granny2SectionPointer Fixup { get; set; }
	public Granny2SectionPointer MarshalledFixup { get; set; }

	public bool IsEmpty => UncompressedSize == 0;

	public bool IsSupported => Compression is Granny2CompressionType.None or
	                                          Granny2CompressionType.Oodle0 or Granny2CompressionType.Oodle1 or
	                                          Granny2CompressionType.BitKnit1 or Granny2CompressionType.BitKnit2;
}
