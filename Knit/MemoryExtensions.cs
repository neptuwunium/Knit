using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
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

	public static int MaxIndex<T>(this Span<T> values) where T : IUnsignedNumber<T>, IComparisonOperators<T, T, bool> {
		var max = T.Zero;
		var maxIndex = 0;
		for (var index = 0; index < values.Length; index++) {
			var value = values[index];
			if (value > max) {
				max = value;
				maxIndex = index;
			}
		}

		return maxIndex;
	}


	public static int UpperBound<T>(this Span<T> list, T value) where T : IComparable<T> {
		int low = 0, high = list.Length;

		while (low < high) {
			var mid = low + (high - low) / 2;
			if (list[mid].CompareTo(value) <= 0) {
				low = mid + 1;
			} else {
				high = mid;
			}
		}

		return low;
	}

	public static T[] PooledResize<T>(this T[] array, int length, int newLength) where T : struct {
		if (length == newLength) {
			return array;
		}

		if (length > newLength) {
			return array;
		}

		if (array.Length <= newLength) {
			var old = array;
			array = ArrayPool<T>.Shared.Rent(newLength);
			old.AsSpan(0, length).CopyTo(array);
			ArrayPool<T>.Shared.Return(old);
		}

		return array;
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
