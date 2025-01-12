namespace Knit;

public readonly ref struct SpanPointer(Span<byte> span, int offset = 0) {
	public Span<byte> Root { get; } = span;
	public Span<byte> Span { get; } = span[offset..];
	public int Offset { get; } = offset;

	public static SpanPointer operator +(SpanPointer ptr, int amount) => new(ptr.Root, ptr.Offset + amount);
	public static SpanPointer operator ++(SpanPointer ptr) => new(ptr.Root, ptr.Offset + 1);
	public static SpanPointer operator -(SpanPointer ptr, int amount) => new(ptr.Root, ptr.Offset - amount);
	public static SpanPointer operator --(SpanPointer ptr) => new(ptr.Root, ptr.Offset - 1);
	public static implicit operator int(SpanPointer ptr) => ptr.Offset;
	public static implicit operator Span<byte>(SpanPointer ptr) => ptr.Span;
	public static implicit operator ReadOnlySpan<byte>(SpanPointer ptr) => ptr.Span;
}
