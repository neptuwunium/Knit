// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

public class CurveD3I1<T> : CurveValue where T : unmanaged, INumberBase<T> {
	[GrannyMember("OneOverKnotScaleTrunc")] public ushort OneOverKnotScale { get; set; }
	public Vector3 ControlScale { get; set; }
	public Vector3 ControlOffset { get; set; }
	public T[] KnotsControls { get; set; } = [];

	public override CurveDimension Dimension => CurveDimension.Position;
	public override int Frames => KnotsControls.Length / 4;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		var scaleInt = (uint) OneOverKnotScale << 16;
		var scale = Unsafe.As<uint, float>(ref scaleInt);

		var frames = Frames;
		var compressedTime = KnotsControls.AsSpan(0, frames);
		var basis = MemoryMarshal.Cast<T, AnyVector3<T>>(KnotsControls.AsSpan(frames));

		for (var i = 0; i < frames; i++) {
			timestamps.Add(float.CreateChecked(compressedTime[i]) / scale);
			positions.Add(basis[i].ToVec() * ControlScale + ControlOffset);
		}
	}
}
