// SPDX-FileCopyrightText: 2025 Legiayayana, 2017-2025 Norbyte
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

public class CurveD4<T> : CurveValue where T : unmanaged, IBinaryNumber<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T> {
	public ushort ScaleOffsetTableEntries { get; set; }
	public float OneOverKnotScale { get; set; }
	public T[] KnotsControls { get; set; } = [];

	public override CurveDimension Dimension => CurveDimension.Rotation;
	public override int Frames => KnotsControls.Length / 4;

	private static Quaternion QuatFromControl(AnyVector3<T> abc, float[] scales, float[] offsets) {
		var (a, b, c) = abc;
		var numBits = Unsafe.SizeOf<T>() << 3;
		var bitMask = T.CreateChecked(1 << (numBits - 1));
		var bitMaskUnder = bitMask - T.One;
		var swizzle1 = int.CreateChecked(((b & bitMask) >> 14) | (c >> 15));
		var swizzle2 = int.CreateChecked(swizzle1 + 1) & 3;
		var swizzle3 = int.CreateChecked(swizzle2 + 1) & 3;
		var swizzle4 = int.CreateChecked(swizzle3 + 1) & 3;

		var dataA = int.CreateChecked(a & bitMaskUnder) * scales[swizzle2] + offsets[swizzle2];
		var dataB = int.CreateChecked(b & bitMaskUnder) * scales[swizzle3] + offsets[swizzle3];
		var dataC = int.CreateChecked(c & bitMaskUnder) * scales[swizzle4] + offsets[swizzle4];

		var dataD = (float) Math.Sqrt(1 - int.CreateChecked(dataA * dataA + dataB * dataB + dataC * dataC));
		if ((a & bitMask) != T.Zero) {
			dataD = -dataD;
		}

		var f = new float[4];
		f[swizzle2] = float.CreateChecked(dataA);
		f[swizzle3] = float.CreateChecked(dataB);
		f[swizzle4] = float.CreateChecked(dataC);
		f[swizzle1] = dataD;

		return new Quaternion(f[0], f[1], f[2], f[3]);
	}

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		var selector = ScaleOffsetTableEntries;
		var numBits = Unsafe.SizeOf<T>() << 3;
		var scale = 1.0f / float.CreateChecked(T.CreateChecked(1 << (numBits - 1)) - T.One);
		var scaleTable = new[] { CurveD4Tables.S[(selector >> 0) & 0x0F] * scale, CurveD4Tables.S[(selector >> 4) & 0x0F] * scale, CurveD4Tables.S[(selector >> 8) & 0x0F] * scale, CurveD4Tables.S[(selector >> 12) & 0x0F] * scale };
		var offsetTable = new[] { CurveD4Tables.O[(selector >> 0) & 0x0F], CurveD4Tables.O[(selector >> 4) & 0x0F], CurveD4Tables.O[(selector >> 8) & 0x0F], CurveD4Tables.O[(selector >> 12) & 0x0F] };

		var frames = Frames;
		var compressedTime = KnotsControls.AsSpan(0, frames);
		var basis = MemoryMarshal.Cast<T, AnyVector3<T>>(KnotsControls.AsSpan(frames));

		for (var i = 0; i < frames; i++) {
			timestamps.Add(float.CreateChecked(compressedTime[i]) / OneOverKnotScale);
			rotations.Add(QuatFromControl(basis[i], scaleTable, offsetTable));
		}
	}
}
