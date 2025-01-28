// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers;
using System.Collections;
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
				using var marshalledFixups = ReadFixups<Granny2MarshalledFixup>(stream, section.Compression >= Granny2CompressionType.BitKnit1, section.MarshalledFixup.Count);
				var marshalledFixupsSpan = marshalledFixups.Memory.Span[..section.MarshalledFixup.Count];
				foreach (var marshal in marshalledFixupsSpan) {
					var objectLocation = new SpanPointer(fileData, Dereference((Granny2SectionId) index, marshal.ObjectOffset));
					var typeLocation = new SpanPointer(fileData, Dereference(marshal.TypeLocation));
					ApplyMarshal(objectLocation, marshal.Count, typeLocation);
				}
			}

			stream.Position = section.Fixup.Offset;
			using var fixups = ReadFixups<Granny2Fixup>(stream, section.Compression >= Granny2CompressionType.BitKnit1, section.Fixup.Count);
			var fixupsSpan = fixups.Memory.Span[..section.Fixup.Count];
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

	public Func<string, Type?>? TypeResolver { get; set; }
#if DEBUG
	private HashSet<int> DebuggedTypes { get; } = [];
#endif

	public void Dispose() {
		HeaderData.Dispose();
		FileData.Dispose();
		ArrayPool<int>.Shared.Return(SectionBaseAddress);
	}

	private static IMemoryOwner<T> ReadFixups<T>(Stream stream, bool isBitKnit, int count) where T : struct {
		IMemoryOwner<T>? fixups = null;
		try {
			fixups = MemoryPool<T>.Shared.Rent(count);
			var fixupsSpan = fixups.Memory.Span[..count].AsBytes();
			if (isBitKnit) {
				var compressedSize = 0;
				stream.ReadExactly(new Span<int>(ref compressedSize).AsBytes());
				using var compressed = MemoryPool<byte>.Shared.Rent(compressedSize);
				var compressedSpan = compressed.Memory.Span[..compressedSize];
				stream.ReadExactly(compressedSpan);
				GrannyBitKnitCompression.Decompress(compressedSpan, fixupsSpan);
			} else {
				stream.ReadExactly(fixupsSpan);
			}

			return fixups;
		} catch {
			fixups?.Dispose();
			throw;
		}
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

			var size = typeInfo.GetTotalSize(this);
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
			size += typeInfo.GetTotalSize(this);
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

	public T? LoadRoot<T>() where T : class => LoadType<T>(Resolve(FileInfo.Root), Resolve(FileInfo.RootTypeDefinition));

	public object? LoadRoot(Type type) => LoadType(type, Resolve(FileInfo.Root), Resolve(FileInfo.RootTypeDefinition));

	public T? LoadType<T>(SpanPointer objectLocation, SpanPointer typeLocation) where T : class => LoadType(typeof(T), objectLocation, typeLocation) as T;

	public object? LoadType(Type type, SpanPointer objectLocation, SpanPointer typeLocation) => Header.Is64Bit ? LoadType<long>(type, objectLocation, typeLocation, []) : LoadType<int>(type, objectLocation, typeLocation, []);

	public object? LoadType<T>(Type type, SpanPointer objectLocation, SpanPointer typeLocation, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		if (references.TryGetValue(objectLocation, out var cached)) {
			return cached;
		}

		if (type == typeof(string)) {
			var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
			if (offset == 0) {
				return string.Empty;
			}

			var nestedOffset = Resolve(offset);
			var end = nestedOffset.Span.IndexOf((byte) 0);
			return Encoding.ASCII.GetString(nestedOffset.Span[..end]);
		}

		if (type == typeof(object)) {
			type = TryResolveCustomType<T>(objectLocation, typeLocation);
		}

		if (type == typeof(Dictionary<string, object?>)) {
			return LoadTypeData<T>(objectLocation, typeLocation, []);
		}

		if (type.IsValueType || type.IsEnum || type.IsPrimitive) {
			var realSize = Marshal.SizeOf(type);
			if (objectLocation.Span.Length >= realSize) {
				var span = objectLocation.Span;
				unsafe {
					fixed (byte* pin = &span.GetPinnableReference()) {
						return Marshal.PtrToStructure((nint) pin, type);
					}
				}
			}
		}

		if (type.IsAbstract) {
			if (type.GetMethod("SelectVariant", BindingFlags.Public | BindingFlags.Static) is not { } selector) {
				throw new InvalidOperationException();
			}

			var tiny = new List<Granny2TinyType>();
			EnumerateTypeMembers(typeLocation, typeDefinition => {
				tiny.Add(new Granny2TinyType(typeDefinition.GetName(this), typeDefinition.ArraySize, typeDefinition.Type));
				return true;
			});

			type = selector.Invoke(null, [tiny]) as Type ?? throw new InvalidOperationException();
		}

		var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException();
		references[objectLocation] = instance;

		var shouldStub = type.GetCustomAttribute<GrannyStubMembersAttribute>() != null;
		var tracker = new HashSet<string>();

		var members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		                  .Where(x => x.GetCustomAttribute<GrannyIgnoreMemberAttribute>() == null && x.SetMethod != null)
		                  .ToDictionary(x => x.GetCustomAttribute<GrannyMemberAttribute>()?.Name ?? x.Name, x => x, StringComparer.OrdinalIgnoreCase);

		var typeInfos = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		do {
			var typeInfo = typeInfos[0];

			var name = typeInfo.GetName(this);

			var size = typeInfo.GetTotalSize(this);
			var singleSize = typeInfo.GetSize(this);
			try {
				if (shouldStub && !tracker.Add(name)) {
					continue;
				}

				if ((instance as IGrannyType)?.Visit(this, name, typeInfo, objectLocation) == true) {
					continue;
				}

				if (!members.TryGetValue(name, out var member)) {
					continue;
				}

				var propertyType = member.PropertyType;
				var value = ExtractValue(objectLocation, propertyType, typeInfo, singleSize, references);
				if (value != null) {
					member.SetValue(instance, value);
				}
			} finally {
				objectLocation += size;

				typeInfos = typeInfos[1..];
			}
		} while (typeInfos[0].Type != Granny2MemberType.End);

		return instance;
	}

	private Type TryResolveCustomType<T>(SpanPointer objectLocation, SpanPointer typeLocation) where T : struct, ISignedNumber<T> {
		var tmpType = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		var tmpLoc = objectLocation;
		do {
			var typeInfo = tmpType[0];
			var size = typeInfo.GetTotalSize(this);
			try {
				// todo: handle other cases?
				if (typeInfo.Type == Granny2MemberType.String && typeInfo.GetName(this).Equals("TypeName", StringComparison.OrdinalIgnoreCase)) {
					var name = ReadValue<T>(tmpLoc, typeInfo, []) as string;
					if (!string.IsNullOrEmpty(name)) {
						var newType = TypeResolver?.Invoke(name);
						if (newType != null) {
							return newType;
						}

					#if DEBUG
						if (DebuggedTypes.Add(typeLocation.Offset)) {
							Console.WriteLine($"Tried to resolve custom type {name} which was not handled.");
							DebugType(typeLocation, "\t", []);
						}
					#endif

						break;
					}
				}
			} finally {
				tmpLoc += size;

				tmpType = tmpType[1..];
			}
		} while (tmpType[0].Type != Granny2MemberType.End);

		return typeof(Dictionary<string, object?>);
	}

	public Dictionary<string, object?> LoadTypeData(SpanPointer objectLocation, SpanPointer typeLocation) => Header.Is64Bit ? LoadTypeData<long>(objectLocation, typeLocation, []) : LoadTypeData<int>(objectLocation, typeLocation, []);

	public Dictionary<string, object?> LoadTypeData<T>(SpanPointer objectLocation, SpanPointer typeLocation, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		var result = new Dictionary<string, object?>();
		var typeInfos = MemoryMarshal.Cast<byte, Granny2TypeDefinition<T>>(typeLocation);
		do {
			var typeInfo = typeInfos[0];

			var name = typeInfo.GetName(this);
			var size = typeInfo.GetTotalSize(this);
			try {
				result[name] = ReadValue<T>(objectLocation, typeInfo, references);
			} finally {
				objectLocation += size;

				typeInfos = typeInfos[1..];
			}
		} while (typeInfos[0].Type != Granny2MemberType.End);

		return result;
	}

	public object? ReadValue(SpanPointer objectLocation, IGranny2TypeDefinition typeInfo, Dictionary<int, object?> references) => Header.Is64Bit ? ReadValue<long>(objectLocation, typeInfo, references) : ReadValue<int>(objectLocation, typeInfo, references);

	public object? ReadValue<T>(SpanPointer objectLocation, IGranny2TypeDefinition typeInfo, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		if (typeInfo.ArraySize > 1) {
			var result = new object?[typeInfo.ArraySize];
			var size = typeInfo.GetSize(this);
			var nestedTypeInfo = typeInfo.Single();

			for (var index = 0; index < typeInfo.ArraySize; ++index) {
				try {
					result[index] = ReadValue<T>(objectLocation, nestedTypeInfo, references);
				} finally {
					objectLocation += size;
				}
			}

			return result;
		}

		switch (typeInfo.Type) {
			case Granny2MemberType.Inline: {
				return LoadTypeData<T>(objectLocation, typeInfo.GetTypeDefinition(this), references);
			}
			case Granny2MemberType.Reference:
			case Granny2MemberType.VariantReference: {
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));

				SpanPointer nestedTypeAddress;
				if (typeInfo.Type == Granny2MemberType.VariantReference) {
					var typePtr = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + int.CreateChecked(Unsafe.SizeOf<T>())));
					(offset, typePtr) = (typePtr, offset);
					if (typePtr == 0) {
						return null;
					}

					nestedTypeAddress = Resolve(typePtr);
				} else {
					nestedTypeAddress = typeInfo.GetTypeDefinition(this);
				}

				if (offset == 0) {
					return null;
				}

				var nestedObjectLocation = Resolve(offset);
				return LoadTypeData<T>(nestedObjectLocation, nestedTypeAddress, references);
			}
			case Granny2MemberType.ReferenceToArray:
			case Granny2MemberType.ReferenceToVariantArray: {
				var nestedType = typeInfo.GetTypeDefinition(this);
				if (typeInfo.Type == Granny2MemberType.ReferenceToVariantArray) {
					var typePtr = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
					if (typePtr == 0) {
						return null;
					}

					nestedType = Resolve(typePtr);
					objectLocation += Unsafe.SizeOf<T>();
				}

				var count = MemoryMarshal.Read<int>(objectLocation);
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
				if (offset == 0 || count == 0) {
					return null;
				}

				var result = new object?[count];
				var nestedObjectLocation = Resolve(offset);
				var nestedSize = CalculateTypeSize<T>(nestedType);
				for (var index = 0; index < count; ++index) {
					try {
						result[index] = LoadTypeData<T>(nestedObjectLocation, nestedType, references);
					} finally {
						nestedObjectLocation += nestedSize;
					}
				}

				return result;
			}
			case Granny2MemberType.ArrayOfReferences: {
				var count = MemoryMarshal.Read<int>(objectLocation);
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
				if (offset == 0 || count == 0) {
					return null;
				}

				var nestedObjectLocation = Resolve(offset);
				var nestedSize = Unsafe.SizeOf<T>();
				var result = new object?[typeInfo.ArraySize];
				for (var index = 0; index < count; ++index) {
					try {
						var arrayOffset = int.CreateChecked(MemoryMarshal.Read<T>(nestedObjectLocation));
						if (arrayOffset == 0) {
							continue;
						}

						var arrayLocation = Resolve(arrayOffset);
						result[index] = LoadTypeData<T>(arrayLocation, typeInfo.GetTypeDefinition(this), references);
					} finally {
						nestedObjectLocation += nestedSize;
					}
				}

				return result;
			}
			case Granny2MemberType.SwitchableType: throw new NotSupportedException();
			case Granny2MemberType.String: {
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
				if (offset == 0) {
					return string.Empty;
				}

				var nestedOffset = Resolve(offset);
				var end = nestedOffset.Span.IndexOf((byte) 0);
				return Encoding.ASCII.GetString(nestedOffset.Span[..end]);
			}
			case Granny2MemberType.Transform: return MemoryMarshal.Read<Transform>(objectLocation);
			case Granny2MemberType.Real32: return MemoryMarshal.Read<float>(objectLocation);
			case Granny2MemberType.Int8: return MemoryMarshal.Read<sbyte>(objectLocation);
			case Granny2MemberType.UInt8: return MemoryMarshal.Read<byte>(objectLocation);
			case Granny2MemberType.BinormalInt8: return MemoryMarshal.Read<sbyte>(objectLocation) / sbyte.MaxValue;
			case Granny2MemberType.NormalUInt8: return MemoryMarshal.Read<byte>(objectLocation) / uint.MaxValue;
			case Granny2MemberType.Int16: return MemoryMarshal.Read<short>(objectLocation);
			case Granny2MemberType.UInt16: return MemoryMarshal.Read<ushort>(objectLocation);
			case Granny2MemberType.BinormalInt16: return MemoryMarshal.Read<short>(objectLocation) / short.MaxValue;
			case Granny2MemberType.NormalUInt16: return MemoryMarshal.Read<ushort>(objectLocation) / ushort.MaxValue;
			case Granny2MemberType.Int32: return MemoryMarshal.Read<int>(objectLocation);
			case Granny2MemberType.UInt32: return MemoryMarshal.Read<uint>(objectLocation);
			case Granny2MemberType.Real16: return MemoryMarshal.Read<Half>(objectLocation);
			case Granny2MemberType.EmptyReference: return null;
			default: throw new ArgumentOutOfRangeException();
		}
	}

	// NOTE: we're not checking the type info type attribute, the chance of marshalling something as something else is not zero.
	public object? ExtractValue<T>(SpanPointer objectLocation, Type propertyType, Granny2TypeDefinition<T> typeInfo, int size, Dictionary<int, object?> references) where T : struct, ISignedNumber<T> {
		if (propertyType.IsArray || (propertyType.IsConstructedGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))) {
			object? array = null;
			Action<int> init;
			Action<object?, int> add;
			Type type;
			if (propertyType.IsArray) {
				type = propertyType.GetElementType()!;
				init = count => array = Array.CreateInstance(type, count);
				add = (value, index) => ((Array) array!).SetValue(value, index);
			} else {
				type = propertyType.GetGenericArguments()[0];
				init = count => array = Activator.CreateInstance(propertyType, count);
				add = (value, index) => ((IList) array!).Add(value);
			}

			switch (typeInfo.Type) {
				case Granny2MemberType.ReferenceToVariantArray:
				case Granny2MemberType.ReferenceToArray: {
					var nestedType = typeInfo.GetTypeDefinition(this);
					if (typeInfo.Type == Granny2MemberType.ReferenceToVariantArray) {
						var typePtr = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
						if (typePtr == 0) {
							return null;
						}

						nestedType = Resolve(typePtr);
						objectLocation += Unsafe.SizeOf<T>();
					}

					var count = MemoryMarshal.Read<int>(objectLocation);
					var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
					init(count);
					if (offset == 0 || count == 0) {
						return null;
					}

					var nestedObjectLocation = Resolve(offset);
					var nestedSize = CalculateTypeSize<T>(nestedType);
					for (var index = 0; index < count; ++index) {
						try {
							var nested = LoadType<T>(type, nestedObjectLocation, nestedType, references);
							add(nested, index);
						} finally {
							nestedObjectLocation += nestedSize;
						}
					}

					return array;
				}
				case Granny2MemberType.ArrayOfReferences: {
					var count = MemoryMarshal.Read<int>(objectLocation);
					var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + 4));
					init(count);
					if (offset == 0 || count == 0) {
						return null;
					}

					var nestedObjectLocation = Resolve(offset);
					var nestedSize = Unsafe.SizeOf<T>();
					for (var index = 0; index < count; ++index) {
						try {
							var arrayOffset = int.CreateChecked(MemoryMarshal.Read<T>(nestedObjectLocation));
							if (arrayOffset == 0) {
								continue;
							}

							var arrayLocation = Resolve(arrayOffset);
							var nested = LoadType<T>(type, arrayLocation, typeInfo.GetTypeDefinition(this), references);
							add(nested, index);
						} finally {
							nestedObjectLocation += nestedSize;
						}
					}

					return array;
				}
			}

			if (typeInfo.ArraySize >= 1) {
				init(typeInfo.ArraySize);
				for (var index = 0; index < typeInfo.ArraySize; ++index) {
					try {
						var nested = LoadType<T>(type, objectLocation, typeInfo.GetTypeDefinition(this), references);
						add(nested, index);
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

		if (propertyType.IsClass) {
			if (typeInfo.Type == Granny2MemberType.EmptyReference) {
				return null;
			}

			var nestedObjectLocation = objectLocation;
			var nestedTypeAddress = typeInfo.GetTypeDefinition(this);
			var nestedType = propertyType;
			if (typeInfo.Type is Granny2MemberType.Reference or Granny2MemberType.VariantReference) {
				var offset = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation));
				if (typeInfo.Type is Granny2MemberType.VariantReference) {
					var typePtr = int.CreateChecked(MemoryMarshal.Read<T>(objectLocation + int.CreateChecked(Unsafe.SizeOf<T>())));
					(offset, typePtr) = (typePtr, offset);
					if (typePtr == 0) {
						return null;
					}

					nestedTypeAddress = Resolve(typePtr);
				}

				if (offset == 0) {
					return null;
				}

				nestedObjectLocation = Resolve(offset);
			}

			if (nestedTypeAddress != 0) {
				var nested = LoadType<T>(nestedType, nestedObjectLocation, nestedTypeAddress, references);
				return nested;
			}

			return null;
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

		if (propertyType.IsValueType || propertyType.IsPrimitive || propertyType.IsEnum) {
			var realType = propertyType.IsEnum ? propertyType.GetEnumUnderlyingType() : propertyType;
			var span = objectLocation.Span;
			unsafe {
				fixed (byte* pin = &span.GetPinnableReference()) {
					var value = Marshal.PtrToStructure((nint) pin, realType)!;

					if (propertyType.IsEnum) {
						value = Enum.ToObject(propertyType, value);
					}

					return value;
				}
			}
		}

		throw new InvalidOperationException();
	}

	public int Dereference(Granny2Reference ptr) => Dereference(ptr.Section, ptr.Offset);
	public int Dereference(Granny2SectionId section, int offset) => SectionBaseAddress[(int) section] + offset;
	public SpanPointer Resolve(int address) => new(FileData.Memory.Span, address);
	public SpanPointer Resolve(Granny2Reference ptr) => Resolve(Dereference(ptr));
	public SpanPointer Resolve(Granny2SectionId section, int offset) => Resolve(Dereference(section, offset));

	public void DebugType(SpanPointer ptr, string indent, Dictionary<int, int> visited) {
		EnumerateTypeMembers(ptr, typeInfo => {
			Console.Write($"{indent}{typeInfo.Type:G} {typeInfo.GetName(this)}");
			if (typeInfo.ArraySize > 1) {
				Console.Write($"[{typeInfo.ArraySize}]");
			}

			if (typeInfo.ChildrenOffset != 0) {
				if (visited.TryGetValue(typeInfo.ChildrenOffset, out var typeIndex)) {
					Console.WriteLine($" &{typeIndex}");
				} else {
					typeIndex = visited[typeInfo.ChildrenOffset] = visited.Count + 1;
					Console.WriteLine($" &{typeIndex}");
					DebugType(typeInfo.GetTypeDefinition(this), indent + "\t", visited);
				}
			} else {
				Console.WriteLine();
			}

			return true;
		});
	}
}
