using System.Buffers;
using System.Runtime.InteropServices;

namespace Knit;

internal class TypedMemory<T>(Memory<byte> buffer) : MemoryManager<T> where T : struct {
	private Memory<byte> Buffer { get; } = buffer;

	public override Span<T> GetSpan() => MemoryMarshal.Cast<byte, T>(Buffer.Span);

	public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

	public override void Unpin() {
		throw new NotSupportedException();
	}

	protected override bool TryGetArray(out ArraySegment<T> segment) => MemoryMarshal.TryGetArray(Memory, out segment);

	protected override void Dispose(bool disposing) { }
}
