using System.Runtime.InteropServices;

namespace Knit.Meta;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x20)]
public record struct Granny2Header {
	public static readonly Granny2Magic Granny32_6_LE = new(0xCAB067B8, 0x0FB16DF8, 0x7E8C7284, 0x1E00195E);
	public static readonly Granny2Magic Granny32_6_BE = new(0xB867B0CA, 0xF86DB10F, 0x84728C7E, 0x5E19001E);
	public static readonly Granny2Magic Granny32_7_LE = new(0xC06CDE29, 0x2B53A4BA, 0xA5B7F525, 0xEEE266F6);
	public static readonly Granny2Magic Granny32_7_BE = new(0xB595110E, 0x4BB5A56A, 0x502828EB, 0x04B37825);
	public static readonly Granny2Magic Granny64_7_LE = new(0x5E499BE5, 0x141F636F, 0xA9EB131E, 0xC4EDBE90);
	public static readonly Granny2Magic Granny64_7_BE = new(0xE3D49531, 0x624FDC20, 0x3AD036CC, 0x89FF82B1);
	public const uint LatestVersion = 0;

	public Granny2Magic Magic { get; set; }
	public int HeaderSize { get; set; }
	public uint Version { get; set; }

	public bool Is64Bit => Magic == Granny64_7_LE || Magic == Granny64_7_BE;
	public bool IsV6 => Magic == Granny32_6_LE || Magic == Granny32_6_BE;
	public bool IsV7 => Magic == Granny32_7_LE || Magic == Granny64_7_LE || Magic == Granny32_7_BE || Magic == Granny64_7_BE;
	public bool IsLittleEndian => Magic == Granny32_6_LE || Magic == Granny32_7_LE || Magic == Granny64_7_LE;
	public bool IsValid => IsV6 || IsV7;
	public bool IsSupported => Version is LatestVersion && IsValid;
	public bool ShouldConvertEndianness => BitConverter.IsLittleEndian ^ IsLittleEndian;
}
