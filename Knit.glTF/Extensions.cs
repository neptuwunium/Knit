// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Numerics;
using GLTF.Scaffold;
using Knit.TypeDefinitions;

namespace Knit.glTF;

public static class Extensions {
	public static void ToGLTF(this Transform transform, Node node, float scale = 1.0f) {
		if ((transform.Flags & XFormFlags.HasPosition) != 0) {
			node.Translation = (transform.Position * scale).ToGLTF();
		}

		if ((transform.Flags & XFormFlags.HasRotation) != 0) {
			node.Rotation = transform.Rotation.ToGLTF();
		}

		if ((transform.Flags & XFormFlags.HasScaleMatrix) != 0) {
			node.Scale = (transform.Scale * scale).ToGLTF();
		} else if (Math.Abs(1.0f - scale) > 0.0001f) {
			node.Scale = new Vector3(scale).ToGLTF();
		}
	}
}
