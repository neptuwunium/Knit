using System.Runtime.CompilerServices;

namespace Knit;

internal static class MemoryExtensions {
	public static Memory<T> Cast<T>(this Memory<byte> bytes) where T : struct {
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
			throw new ArgumentException("Type " + typeof(T).FullName + " is a reference or contains references.");
		}

		return new TypedMemory<T>(bytes).Memory;
	}
}
