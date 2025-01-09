using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Knit.Compression;
using Knit.Meta;

namespace Knit;

public sealed class Granny2File : IDisposable {
	public Granny2File(Stream stream, bool softLoad = false) {
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
		Sections = HeaderData.Memory[(Unsafe.SizeOf<Granny2Header>() + FileInfo.Sections.Offset)..].Cast<Granny2Section>()[..FileInfo.Sections.Count];

		if (!FileInfo.IsSupported) {
			HeaderData.Dispose();
			throw new NotSupportedException();
		}

		if (softLoad) {
			FileData = MemoryPool<byte>.Shared.Rent(1);
			return;
		}

		var totalSize = 0;
		foreach (var section in Sections.Span) {
			totalSize += section.UncompressedSize;
		}

		FileData = MemoryPool<byte>.Shared.Rent(totalSize);
		var cursor = 0;
		var fileData = FileData.Memory.Span;
		foreach (var section in Sections.Span) {
			if (section.IsEmpty) {
				continue;
			}

			try {
				if (section.IsSupported) {
					var target = fileData.Slice(cursor, section.UncompressedSize);
					stream.Position = section.Data.Offset;
					if (section.Compression is Granny2CompressionType.None) {
						stream.ReadExactly(target);
					} else {
						using var compressedPool = MemoryPool<byte>.Shared.Rent(section.Data.Count);
						var compressed = compressedPool.Memory.Span[..section.Data.Count];
						stream.ReadExactly(compressed);

						switch (section.Compression) {
							case Granny2CompressionType.Oodle0:
							case Granny2CompressionType.Oodle1: {
								GrannyOodleCompression.Decompress(compressed, target, section.CompressionBits1, section.CompressionBits2, section.UncompressedSize, header.ShouldConvertEndianness, section.Compression == Granny2CompressionType.Oodle1);
								break;
							}
							default: throw new UnreachableException();
						}
					}
				}
			} finally {
				cursor += section.UncompressedSize;
			}
		}
	}

	public Granny2Header Header { get; }
	public Granny2FileInfo FileInfo { get; }
	public Memory<Granny2Section> Sections { get; }

	private IMemoryOwner<byte> HeaderData { get; }
	private IMemoryOwner<byte> FileData { get; }

	public void Dispose() {
		HeaderData.Dispose();
		FileData.Dispose();
	}
}
