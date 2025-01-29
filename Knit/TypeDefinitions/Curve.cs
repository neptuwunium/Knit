// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Knit.TypeDefinitions;

public class Curve<T> : CurveValue where T : unmanaged, INumberBase<T> {
	[GrannyMember("OneOverKnotScaleTrunc")] public ushort OneOverKnotScale { get; set; }
	public float[] ControlScaleOffsets { get; set; } = [];
	public T[] KnotsControls { get; set; } = [];

	public override CurveDimension Dimension => (CurveDimension) (ControlScaleOffsets.Length / 2);
	public override int Frames => KnotsControls.Length / (int) Dimension + 1;

	public override void Decompress(out List<float> timestamps, out List<Vector3> positions, out List<Quaternion> rotations, out List<Matrix3x3> scales) {
		base.Decompress(out timestamps, out positions, out rotations, out scales);

		var scaleInt = (uint) OneOverKnotScale << 16;
		var scale = Unsafe.As<uint, float>(ref scaleInt);

		var frames = Frames;
		var compressedTime = KnotsControls.AsSpan(0, frames);
		var basis = KnotsControls.AsSpan(frames);

		switch (Dimension) {
			case CurveDimension.Position: {
				var pos = MemoryMarshal.Cast<T, AnyVector3<T>>(basis);
				var control = MemoryMarshal.Cast<float, Vector3>(ControlScaleOffsets);
				for (var i = 0; i < frames; i++) {
					timestamps.Add(float.CreateChecked(compressedTime[i]) / scale);
					positions.Add(pos[i].ToVec() * control[i * 2] + control[i * 2 + 1]);
				}

				break;
			}
			case CurveDimension.Rotation: {
				var quats = MemoryMarshal.Cast<T, AnyVector4<T>>(basis);
				var control = MemoryMarshal.Cast<float, Vector4>(ControlScaleOffsets);
				for (var i = 0; i < frames; i++) {
					timestamps.Add(float.CreateChecked(compressedTime[i]) / scale);
					var vec = quats[i].ToVec() * control[i * 2] + control[i * 2 + 1];
					rotations.Add(new Quaternion(vec.X, vec.Y, vec.Z, vec.W));
				}

				break;
			}
			case CurveDimension.ScaleShear: {
				var matrices = MemoryMarshal.Cast<T, AnyMatrix3x3<T>>(basis);
				var control = MemoryMarshal.Cast<float, Matrix3x3>(ControlScaleOffsets);
				for (var i = 0; i < frames; i++) {
					timestamps.Add(float.CreateChecked(compressedTime[i]) / scale);
					scales.Add(matrices[i].ToMatrix() * control[i * 2] + control[i * 2 + 1]);
				}

				break;
			}
		}
	}
}
