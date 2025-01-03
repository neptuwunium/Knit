using System.Runtime.InteropServices;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x2C)]
public record struct Granny2Sector {
	public Granny2CompressionType Compression { get; set; }
	public Granny2SectorPointer Data { get; set; }
	public int UncompressedSize { get; set; }
	public uint Alignment { get; set; }
	public uint CompressionBits1 { get; set; }
	public uint CompressionBits2 { get; set; }
	public Granny2SectorPointer Fixup { get; set; }
	public Granny2SectorPointer MarshalledFixup { get; set; }

	public bool IsEmpty => UncompressedSize == 0;
	public bool IsSupported => Compression is not Granny2CompressionType.None; // need to implement Oodle1 and BitKnit1
}
