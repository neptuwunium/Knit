using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Knit.Meta;

namespace Knit;

public sealed class Granny2File : IDisposable {
	public Granny2File(Stream stream) {
		var header = new Granny2Header();
		var headerSpan = new Span<Granny2Header>(ref header);
		stream.ReadExactly(MemoryMarshal.AsBytes(headerSpan));
		Header = header;

		if (header.ShouldConvertEndianness) {
			MemoryMarshal.AsBytes(headerSpan)[16..].Reverse32();
		}

		if (!header.IsSupported) {
			throw new NotSupportedException();
		}

		HeaderData = MemoryPool<byte>.Shared.Rent(header.HeaderSize);
		var headerData = HeaderData.Memory[..header.HeaderSize];
		var headerDataSpan = headerData.Span;
		stream.Position = 0;
		stream.ReadExactly(headerDataSpan);

		if (header.ShouldConvertEndianness) {
			headerDataSpan.Reverse32();
		}

		FileInfo = MemoryMarshal.Read<Granny2FileInfo>(headerDataSpan[Unsafe.SizeOf<Granny2Header>()..]);
		Sectors = HeaderData.Memory[(Unsafe.SizeOf<Granny2Header>() + FileInfo.Sectors.Offset)..].Cast<Granny2Sector>()[..FileInfo.Sectors.Count];

		if (!FileInfo.IsSupported) {
			HeaderData.Dispose();
			throw new NotSupportedException();
		}

		var totalSize = 0;
		foreach (var sector in Sectors.Span) {
			totalSize += sector.UncompressedSize;
		}

		FileData = MemoryPool<byte>.Shared.Rent(totalSize);
	}

	public Granny2Header Header { get; }
	public Granny2FileInfo FileInfo { get; }
	public Memory<Granny2Sector> Sectors { get; }

	private IMemoryOwner<byte> HeaderData { get; }
	private IMemoryOwner<byte> FileData { get; }

	public void Dispose() {
		HeaderData.Dispose();
		FileData.Dispose();
	}
}
