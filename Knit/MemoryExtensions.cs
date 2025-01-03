using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit;

internal static class MemoryExtensions {
	public static Memory<T> Cast<T>(this Memory<byte> bytes) where T : struct {
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
			throw new ArgumentException("Type " + typeof(T).FullName + " is a reference or contains references.");
		}

		return new TypedMemory<T>(bytes).Memory;
	}

	public static void Reverse32(this Memory<byte> bytes) => bytes.Span.Reverse32();

	public static void Reverse32(this Span<byte> bytes) {
		var ints = MemoryMarshal.Cast<byte, uint>(bytes);
		for (var i = 0; i < ints.Length; i++) {
			ints[i] = BinaryPrimitives.ReverseEndianness(ints[i]);
		}
	}

	public static void Reverse16(this Memory<byte> bytes) => bytes.Span.Reverse16();

	public static void Reverse16(this Span<byte> bytes) {
		var ints = MemoryMarshal.Cast<byte, ushort>(bytes);
		for (var i = 0; i < ints.Length; i++) {
			ints[i] = BinaryPrimitives.ReverseEndianness(ints[i]);
		}
	}
}
