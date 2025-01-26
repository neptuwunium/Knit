// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Knit.Compression;
using Knit.Meta;
using Knit.TypeDefinitions;

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

			if (header.ShouldConvertEndianness) {
				stream.Position = section.MarshalledFixup.Offset;
				using var marshalledFixups = MemoryPool<Granny2MarshalledFixup>.Shared.Rent(section.MarshalledFixup.Count);
				var marshalledFixupsSpan = marshalledFixups.Memory.Span[..section.MarshalledFixup.Count];
				stream.ReadExactly(marshalledFixupsSpan.AsBytes());
				foreach (var marshal in marshalledFixupsSpan) {
					var objectLocation = new SpanPointer(fileData, Dereference((Granny2SectionId) index, marshal.ObjectOffset));
					var typeLocation = new SpanPointer(fileData, Dereference(marshal.TypeLocation));
					ApplyMarshal(objectLocation, marshal.Count, typeLocation);
				}
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

	public GrannyFileRoot? LoadRoot() => LoadRoot<GrannyFileRoot>();

	// todo: create method to load into a hashmap.
	public T? LoadRoot<T>() where T : class => LoadType<T>(Resolve(FileInfo.Root), Resolve(FileInfo.RootTypeDefinition));

	public object? LoadRoot(Type type) => LoadType(type, Resolve(FileInfo.Root), Resolve(FileInfo.RootTypeDefinition));

	public T? LoadType<T>(SpanPointer objectLocation, SpanPointer typeLocation) where T : class => LoadType(typeof(T), objectLocation, typeLocation) as T;

	public object? LoadType(Type type, SpanPointer objectLocation, SpanPointer typeLocation) => Header.Is64Bit ? LoadType<long>(type, objectLocation, typeLocation, []) : LoadType<int>(type, objectLocation, typeLocation, []);

	public object? LoadType<T>(Type type, SpanPointer objectLocation, SpanPointer typeLocation, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		if (references.TryGetValue(objectLocation, out var cached)) {
			return cached;
		}

		var instance = Activator.CreateInstance(type) as MarshalledGrannyType ?? throw new InvalidOperationException();
		references[objectLocation] = instance;

		var members = type.GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
		                  .Where(x => x.GetCustomAttribute<GrannyMemberAttribute>() != null)
		                  .ToDictionary(x => {
			                   var attr = x.GetCustomAttribute<GrannyMemberAttribute>()!;
			                   return attr.Name ?? x.Name;
		                   }, x => x);

		var typeInfos = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		do {
			var typeInfo = typeInfos[0];

			var name = typeInfo.GetName(this);
			var size = typeInfo.GetArraySize(this);
			try {
				if (instance.Visit(this, name, typeInfo, objectLocation)) {
					continue;
				}

				if (!members.TryGetValue(name, out var member)) {
					continue;
				}

				var propertyType = member.PropertyType;
				member.SetValue(instance, ExtractValue(objectLocation, propertyType, typeInfo, size, references));
			} finally {
				objectLocation += size;

				typeInfos = typeInfos[1..];
			}
		} while (typeInfos[0].Type != Granny2MemberType.End);

		return instance;
	}

	// NOTE: we're not checking the type info type attribute, the chance of marshalling something as something else is not zero.
	private object? ExtractValue<T>(SpanPointer objectLocation, Type propertyType, Granny2TypeDefinition<T> typeInfo, int size, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		if (propertyType.IsAssignableTo(typeof(MarshalledGrannyType))) {
			if (typeInfo.Type == Granny2MemberType.EmptyReference) {
				return Activator.CreateInstance(propertyType);
			}

			var nestedObjectLocation = objectLocation;
			if (typeInfo.Type is Granny2MemberType.Reference or Granny2MemberType.VariantReference) {
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
				if (typeInfo.Type is Granny2MemberType.VariantReference) {
					var typePtr = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + int.CreateChecked(Unsafe.SizeOf<T>())));
					if (typePtr == 0) {
						return null;
					}

					throw new NotImplementedException(); // todo: find a gr2 file with variant types
				}

				if (offset == 0) {
					return Activator.CreateInstance(propertyType);
				}

				nestedObjectLocation = Resolve(offset);
			}

			if (typeInfo.ChildrenOffset != T.Zero) {
				var nested = LoadType<T>(propertyType, nestedObjectLocation, typeInfo.GetTypeDefinition(this), references);
				return nested;
			}

			throw new InvalidOperationException();
		}

		if (propertyType.IsArray) {
			Array? array;

			switch (typeInfo.Type) {
				case Granny2MemberType.ReferenceToVariantArray:
					throw new NotImplementedException(); // todo: find a gr2 file with variant arrays
				case Granny2MemberType.ReferenceToArray: {
					var count = MemoryMarshal.Read<int>(objectLocation);
					var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
					array = Array.CreateInstanceFromArrayType(propertyType, count);
					if (offset == 0 || count == 0) {
						return array;
					}

					var nestedObjectLocation = Resolve(offset);

					if (references.TryGetValue(nestedObjectLocation, out var value)) {
						return value;
					}

					references[nestedObjectLocation] = array;

					var nestedSize = CalculateTypeSize<T>(typeInfo.GetTypeDefinition(this));
					for (var index = 0; index < count; ++index) {
						try {
							var nested = LoadType<T>(propertyType, nestedObjectLocation, typeInfo.GetTypeDefinition(this), references);
							array.SetValue(nested, index);
						} finally {
							nestedObjectLocation += nestedSize;
						}
					}

					return array;
				}
				case Granny2MemberType.ArrayOfReferences: {
					var count = MemoryMarshal.Read<int>(objectLocation);
					var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
					array = Array.CreateInstanceFromArrayType(propertyType, count);
					if (offset == 0 || count == 0) {
						return array;
					}

					var nestedObjectLocation = Resolve(offset);

					if (references.TryGetValue(nestedObjectLocation, out var value)) {
						return value;
					}

					references[nestedObjectLocation] = array;

					var nestedSize = Unsafe.SizeOf<T>();
					for (var index = 0; index < count; ++index) {
						try {
							var arrayOffset = int.CreateChecked(MemoryMarshal.Read<T>(nestedObjectLocation));
							if (arrayOffset == 0) {
								continue;
							}

							var arrayLocation = Resolve(arrayOffset);
							var nested = LoadType<T>(propertyType, arrayLocation, typeInfo.GetTypeDefinition(this), references);
							array.SetValue(nested, index);
						} finally {
							nestedObjectLocation += nestedSize;
						}
					}

					return array;
				}
			}

			if (typeInfo.ArraySize >= 1) {
				array = Array.CreateInstanceFromArrayType(propertyType, typeInfo.ArraySize);
				for (var index = 0; index < typeInfo.ArraySize; ++index) {
					try {
						var nested = LoadType<T>(propertyType, objectLocation, typeInfo.GetTypeDefinition(this), references);
						array.SetValue(nested, index);
					} finally {
						objectLocation += size;
					}
				}

				return array;
			}

			throw new InvalidOperationException();
		}

		if (propertyType == typeof(string)) {
			var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation.Span));
			if (offset == 0) {
				return string.Empty;
			}

			var nestedOffset = Resolve(offset);
			var end = nestedOffset.Span.IndexOf((byte) 0);
			return Encoding.ASCII.GetString(nestedOffset.Span[..end]);
		}

		if (propertyType.IsPrimitive) {
			if (propertyType == typeof(float)) {
				switch (typeInfo.Type) {
					case Granny2MemberType.NormalUInt8: {
						var value = MemoryMarshal.Read<byte>(objectLocation);
						return value / (float) byte.MaxValue;
					}
					case Granny2MemberType.NormalUInt16: {
						var value = MemoryMarshal.Read<ushort>(objectLocation);
						return value / (float) ushort.MaxValue;
					}
					case Granny2MemberType.BinormalInt8: {
						var value = MemoryMarshal.Read<sbyte>(objectLocation);
						return value / (float) sbyte.MaxValue;
					}
					case Granny2MemberType.BinormalInt16: {
						var value = MemoryMarshal.Read<short>(objectLocation);
						return value / (float) short.MaxValue;
					}
				}
			} else if (propertyType == typeof(bool)) {
				return MemoryMarshal.Read<byte>(objectLocation) == 1;
			} else if (propertyType == typeof(float) && typeInfo.Type == Granny2MemberType.Real16) {
				return (float) MemoryMarshal.Read<Half>(objectLocation);
			}
		}

		// note: should we convert between integer types?

		if (propertyType.IsValueType || propertyType.IsPrimitive) {
			var realSize = Marshal.SizeOf(propertyType);
			if (realSize <= size && objectLocation.Span.Length >= realSize) {
				var span = objectLocation.Span;
				unsafe {
					fixed (byte* pin = &span.GetPinnableReference()) {
						return Marshal.PtrToStructure((nint) pin, propertyType);
					}
				}
			}

			throw new InvalidOperationException();
		}

		throw new InvalidOperationException();
	}

	public int Dereference(Granny2Reference ptr) => Dereference(ptr.Section, ptr.Offset);
	public int Dereference(Granny2SectionId section, int offset) => SectionBaseAddress[(int) section] + offset;
	public SpanPointer Resolve(int address) => new(FileData.Memory.Span, address);
	public SpanPointer Resolve(Granny2Reference ptr) => Resolve(Dereference(ptr));
	public SpanPointer Resolve(Granny2SectionId section, int offset) => Resolve(Dereference(section, offset));
}
