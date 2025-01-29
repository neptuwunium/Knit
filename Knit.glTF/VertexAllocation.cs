// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers;
using System.Runtime.InteropServices;

namespace Knit.glTF;

public readonly ref struct VertexAllocation : IDisposable {
	public VertexAllocation(int count) {
		PositionArray = ArrayPool<float>.Shared.Rent(count * 3);
		NormalArray = ArrayPool<float>.Shared.Rent(count * 3);
		TangentArray = ArrayPool<float>.Shared.Rent(count * 4);
		JointArray = ArrayPool<ushort>.Shared.Rent(count * 4);
		WeightArray = ArrayPool<float>.Shared.Rent(count * 4);
		ColorArray = ArrayPool<float>.Shared.Rent(count * 4 * 2);
		TexCoordArray = ArrayPool<float>.Shared.Rent(count * 2 * 8);

		Position = MemoryMarshal.AsBytes(PositionArray.AsSpan(0, count * 3));
		Normal = MemoryMarshal.AsBytes(NormalArray.AsSpan(0, count * 3));
		Tangent = MemoryMarshal.AsBytes(TangentArray.AsSpan(0, count * 4));
		Joint = MemoryMarshal.AsBytes(JointArray.AsSpan(0, count * 4));
		Weight = MemoryMarshal.AsBytes(WeightArray.AsSpan(0, count * 4));
		Color0 = MemoryMarshal.AsBytes(ColorArray.AsSpan(0, count * 4));
		Color1 = MemoryMarshal.AsBytes(ColorArray.AsSpan(count * 4, count * 4));
		UV0 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(0, count * 2));
		UV1 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 1, count * 2));
		UV2 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 2, count * 2));
		UV3 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 3, count * 2));
		UV4 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 4, count * 2));
		UV5 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 5, count * 2));
		UV6 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 6, count * 2));
		UV7 = MemoryMarshal.AsBytes(TexCoordArray.AsSpan(count * 2 * 7, count * 2));

		Weight.Clear();
		Joint.Clear();
	}

	public float[] PositionArray { get; }
	public float[] NormalArray { get; }
	public float[] TangentArray { get; }
	public float[] ColorArray { get; }
	public float[] TexCoordArray { get; }
	public ushort[] JointArray { get; }
	public float[] WeightArray { get; }
	public Span<byte> Position { get; }
	public Span<byte> Normal { get; }
	public Span<byte> Tangent { get; }
	public Span<byte> Color0 { get; }
	public Span<byte> Color1 { get; }
	public Span<byte> UV0 { get; }
	public Span<byte> UV1 { get; }
	public Span<byte> UV2 { get; }
	public Span<byte> UV3 { get; }
	public Span<byte> UV4 { get; }
	public Span<byte> UV5 { get; }
	public Span<byte> UV6 { get; }
	public Span<byte> UV7 { get; }
	public Span<byte> Joint { get; }
	public Span<byte> Weight { get; }

	public void Dispose() {
		ArrayPool<float>.Shared.Return(PositionArray);
		ArrayPool<float>.Shared.Return(NormalArray);
		ArrayPool<float>.Shared.Return(TangentArray);
		ArrayPool<float>.Shared.Return(ColorArray);
		ArrayPool<float>.Shared.Return(TexCoordArray);
		ArrayPool<ushort>.Shared.Return(JointArray);
		ArrayPool<float>.Shared.Return(WeightArray);
	}
}
