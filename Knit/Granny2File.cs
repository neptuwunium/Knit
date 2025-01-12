// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
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
		SectionBaseAddress = ArrayPool<int>.Shared.Rent(FileInfo.Sections.Count);

		if (!FileInfo.IsSupported) {
			HeaderData.Dispose();
			throw new NotSupportedException();
		}

		if (softLoad) {
			FileData = MemoryPool<byte>.Shared.Rent(1);
			return;
		}

		var totalSize = 0;
		for (var index = 0; index < Sections.Span.Length; index++) {
			SectionBaseAddress[index] = totalSize;
			var section = Sections.Span[index];
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

							case Granny2CompressionType.BitKnit1:
							case Granny2CompressionType.BitKnit2: {
								GrannyBitKnitCompression.Decompress(compressed, target);
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

		for (var index = 0; index < Sections.Span.Length; index++) {
			var section = Sections.Span[index];
			if (section.IsEmpty) {
				continue;
			}

			// for some reason if we apply marshalling after fixups, it dies.
			stream.Position = section.MarshalledFixup.Offset;
			using var marshalledFixups = MemoryPool<Granny2MarshalledFixup>.Shared.Rent(section.MarshalledFixup.Count);
			var marshalledFixupsSpan = marshalledFixups.Memory.Span[..section.MarshalledFixup.Count];
			stream.ReadExactly(marshalledFixupsSpan.AsBytes());
			foreach (var marshal in marshalledFixupsSpan) {
				var objectLocation = new SpanPointer(fileData, Dereference((Granny2SectionId) index, marshal.ObjectOffset));
				var typeLocation = new SpanPointer(fileData, Dereference(marshal.TypeLocation));
				ApplyMarshal(objectLocation, marshal.Count, typeLocation);
			}

			stream.Position = section.Fixup.Offset;
			using var fixups = MemoryPool<Granny2Fixup>.Shared.Rent(section.Fixup.Count);
			var fixupsSpan = fixups.Memory.Span[..section.Fixup.Count];
			stream.ReadExactly(fixupsSpan.AsBytes());
			foreach (var fixup in fixupsSpan) {
				MemoryMarshal.Write(Resolve(Dereference((Granny2SectionId) index, fixup.FromOffset)), Dereference(fixup.To));
			}
		}
	}

	public Granny2Header Header { get; }
	public Granny2FileInfo FileInfo { get; }
	public Memory<Granny2Section> Sections { get; }
	public int[] SectionBaseAddress { get; }

	public IMemoryOwner<byte> HeaderData { get; }
	public IMemoryOwner<byte> FileData { get; }

	public void Dispose() {
		HeaderData.Dispose();
		FileData.Dispose();
		ArrayPool<int>.Shared.Return(SectionBaseAddress);
	}

	public void ApplyMarshal(SpanPointer objectLocation, int typeCount, SpanPointer typeLocation) {
		if (Header.Is64Bit) {
			ApplyMarshal<long>(objectLocation, typeCount, typeLocation);
		} else {
			ApplyMarshal<int>(objectLocation, typeCount, typeLocation);
		}
	}

	public void ApplyMarshal<T>(SpanPointer objectLocation, int typeCount, SpanPointer typeLocation) where T : ISignedNumber<T> {
		var typeInfos = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		for (var index = 0; index < typeInfos.Length && typeCount > 0; index++) {
			var typeInfo = typeInfos[index];
			if (typeInfo.Type == Granny2MemberType.End) {
				typeCount--;
				continue;
			}

			var size = typeInfo.GetArraySize(this);
			if (typeInfo.Type == Granny2MemberType.Inline) {
				ApplyMarshal<T>(objectLocation, 1, typeInfo.GetTypeDefinition(this));
			} else {
				var objBlock = objectLocation.Span[..size];
				switch (typeInfo.Type) {
					case Granny2MemberType.ReferenceToArray:
					case Granny2MemberType.ArrayOfReferences:
					case Granny2MemberType.ReferenceToVariantArray:
					case Granny2MemberType.String:
					case Granny2MemberType.Transform:
					case Granny2MemberType.Real32:
					case Granny2MemberType.Int32:
					case Granny2MemberType.UInt32:
						objBlock.Reverse32();
						break;
					case Granny2MemberType.Int16:
					case Granny2MemberType.UInt16:
					case Granny2MemberType.BinormalInt16:
					case Granny2MemberType.NormalUInt16:
					case Granny2MemberType.Real16:
						objBlock.Reverse16();
						break;
				}
			}

			objectLocation += size;
		}
	}

	public int CalculateTypeSize<T>(SpanPointer typeLocation) where T : ISignedNumber<T> {
		var size = 0;
		EnumerateTypeMembers(typeLocation, typeInfo => {
			size += typeInfo.GetArraySize(this);
			return true;
		});
		return size;
	}

	public void EnumerateTypeMembers(SpanPointer typeLocation, Func<IGranny2TypeDefinition, bool> callback) {
		if (Header.Is64Bit) {
			EnumerateTypeMembers<long>(typeLocation, callback);
		} else {
			EnumerateTypeMembers<int>(typeLocation, callback);
		}
	}

	public static void EnumerateTypeMembers<T>(SpanPointer typeLocation, Func<IGranny2TypeDefinition, bool> callback) where T : ISignedNumber<T> {
		var typeInfos = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		do {
			if (!callback(typeInfos[0])) {
				return;
			}

			typeInfos = typeInfos[1..];
		} while (typeInfos[0].Type != Granny2MemberType.End);
	}

	public int Dereference(Granny2Reference ptr) => Dereference(ptr.Section, ptr.Offset);
	public int Dereference(Granny2SectionId section, int offset) => SectionBaseAddress[(int) section] + offset;
	public SpanPointer Resolve(int address) => new(FileData.Memory.Span, address);
	public SpanPointer Resolve(Granny2Reference ptr) => Resolve(Dereference(ptr));
	public SpanPointer Resolve(Granny2SectionId section, int offset) => Resolve(Dereference(section, offset));
}
