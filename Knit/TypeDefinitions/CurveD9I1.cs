// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Knit.TypeDefinitions;

public class CurveD9I1<T> : CurveValue where T : unmanaged, INumberBase<T> {
	[GrannyMember("OneOverKnotScaleTrunc")] public ushort OneOverKnotScale { get; set; }
	public float ControlScale { get; set; }
	public float ControlOffset { get; set; }
	public T[] KnotsControls { get; set; } = [];

	public override CurveDimension Dimension => CurveDimension.ScaleShear;
	public override int Frames => KnotsControls.Length / 2;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		var scaleInt = (uint) OneOverKnotScale << 16;
		var scale = Unsafe.As<uint, float>(ref scaleInt);

		var frames = Frames;
		var compressedTime = KnotsControls.AsSpan(0, frames);
		var basis = KnotsControls.AsSpan(frames);

		for (var i = 0; i < frames; ++i) {
			timestamps.Add(float.CreateChecked(compressedTime[i]) / scale);
			var n = float.CreateChecked(basis[i]) * ControlScale + ControlOffset;
			scales.Add(new Matrix3x3 {
				M11 = n,
				M22 = n,
				M33 = n,
			});
		}
	}
}
