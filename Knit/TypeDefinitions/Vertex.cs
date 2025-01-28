// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Knit.Meta;

namespace Knit.TypeDefinitions;

public class Vertex : IGrannyType {
	[GrannyIgnoreMember] public Vector3 Position { get; set; }
	[GrannyIgnoreMember] public Vector3 Normal { get; set; }
	[GrannyIgnoreMember] public Vector4 Tangent { get; set; }
	[GrannyIgnoreMember] public Vector3 Binormal { get; set; }
	[GrannyIgnoreMember] public Vector3 TangentBinormalCross { get; set; }
	[GrannyIgnoreMember] public VertexTextureCoordinates TextureCoordinates { get; set; }
	[GrannyIgnoreMember] public VertexBoneWeights BoneWeights { get; set; }
	[GrannyIgnoreMember] public VertexBoneIndices BoneIndices { get; set; }
	[GrannyIgnoreMember] public Vector4 DiffuseColor { get; set; }
	[GrannyIgnoreMember] public Vector4 SpecularColor { get; set; }
	[GrannyIgnoreMember] public VertexAttributePresence Attributes { get; set; }
	[GrannyIgnoreMember] public int ReferencedBoneCount { get; set; }

	public bool Visit(Granny2File file, string name, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		switch (name) {
			case nameof(Position):
				Position = ReadVector3(file, typeInfo, objectLocation);
				Attributes |= VertexAttributePresence.Position;
				return true;
			case nameof(Normal):
				Normal = ReadVector3(file, typeInfo, objectLocation);
				Attributes |= VertexAttributePresence.Normal;
				return true;
			case nameof(Tangent):
				var tangent = ReadVector4(file, typeInfo, objectLocation);
				if (typeInfo.ArraySize < 4) {
					tangent.W = 1;
				}

				Tangent = tangent;
				Attributes |= VertexAttributePresence.Tangent;

				return true;
			case nameof(Binormal):
				Binormal = ReadVector3(file, typeInfo, objectLocation);
				Attributes |= VertexAttributePresence.Binormal;
				return true;
			case nameof(TangentBinormalCross):
				TangentBinormalCross = ReadVector3(file, typeInfo, objectLocation);
				Attributes |= VertexAttributePresence.TangentBinormalCross;
				return true;
			case nameof(DiffuseColor): {
				var value = ReadVector4(file, typeInfo, objectLocation);
				if (typeInfo.ArraySize == 3) { // set alpha to 1.0 (opaque)
					value.W = 1.0f;
				}

				DiffuseColor = value;
				Attributes |= VertexAttributePresence.DiffuseColor;
				return true;
			}
			case nameof(SpecularColor): {
				var value = ReadVector4(file, typeInfo, objectLocation);
				if (typeInfo.ArraySize == 3) { // set alpha to 1.0 (opaque)
					value.W = 1.0f;
				}

				SpecularColor = value;
				Attributes |= VertexAttributePresence.SpecularColor;
				return true;
			}
			case nameof(BoneIndices): {
				ReferencedBoneCount = typeInfo.ArraySize;
				var indices = new VertexBoneIndices();
				var stack = (Span<short>) indices;
				ReadArray(file, typeInfo, objectLocation, ref stack);
				BoneIndices = indices;
				Attributes |= VertexAttributePresence.BoneIndices;
				return true;
			}
			case nameof(BoneWeights): {
				ReferencedBoneCount = typeInfo.ArraySize;
				var weights = new VertexBoneWeights();
				var stack = (Span<float>) weights;
				ReadArray(file, typeInfo, objectLocation, ref stack);
				BoneWeights = weights;
				Attributes |= VertexAttributePresence.BoneWeights;
				return true;
			}
		}

		if (name.StartsWith(nameof(TextureCoordinates))) {
			var index = name.Length == 18 ? 0 : int.Parse(name[18..], NumberStyles.Integer);
			if (index >= 8) {
				throw new InvalidOperationException();
			}

			var textureCoordinates = TextureCoordinates;
			var value = ReadVector2(file, typeInfo, objectLocation);
			textureCoordinates[index] = value;
			TextureCoordinates = textureCoordinates;
			Attributes |= (VertexAttributePresence) ((ulong) VertexAttributePresence.TextureCoordinates0 << index);
			return true;
		}

		return false;
	}

	private static Vector2 ReadVector2(Granny2File file, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		Span<float> stack = stackalloc float[2];
		ReadArray(file, typeInfo, objectLocation, ref stack);
		return MemoryMarshal.Read<Vector2>(MemoryMarshal.AsBytes(stack));
	}

	private static Vector3 ReadVector3(Granny2File file, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		Span<float> stack = stackalloc float[3];
		ReadArray(file, typeInfo, objectLocation, ref stack);
		return MemoryMarshal.Read<Vector3>(MemoryMarshal.AsBytes(stack));
	}

	private static Vector4 ReadVector4(Granny2File file, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation) {
		Span<float> stack = stackalloc float[4];
		ReadArray(file, typeInfo, objectLocation, ref stack);
		return MemoryMarshal.Read<Vector4>(MemoryMarshal.AsBytes(stack));
	}

	private static void ReadArray<T>(Granny2File file, IGranny2TypeDefinition typeInfo, SpanPointer objectLocation, ref Span<T> stack) where T : unmanaged, INumber<T> {
		if (typeInfo.ArraySize == 0) {
			throw new InvalidOperationException();
		}

		var pos = objectLocation;
		for (var index = 0; index < Math.Min(typeInfo.ArraySize, stack.Length); ++index) {
			switch (typeInfo.Type) {
				case Granny2MemberType.End: return;
				case Granny2MemberType.Real16: {
					var value = MemoryMarshal.Read<Half>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 2;
					break;
				}
				case Granny2MemberType.Real32: {
					var value = MemoryMarshal.Read<float>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 4;
					break;
				}
				case Granny2MemberType.Int8: {
					var value = MemoryMarshal.Read<sbyte>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 1;
					break;
				}
				case Granny2MemberType.UInt8: {
					var value = MemoryMarshal.Read<byte>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 1;
					break;
				}
				case Granny2MemberType.Int16: {
					var value = MemoryMarshal.Read<short>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 2;
					break;
				}
				case Granny2MemberType.UInt16: {
					var value = MemoryMarshal.Read<ushort>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 2;
					break;
				}
				case Granny2MemberType.Int32: {
					var value = MemoryMarshal.Read<int>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 4;
					break;
				}
				case Granny2MemberType.UInt32: {
					var value = MemoryMarshal.Read<uint>(pos);
					stack[index] = T.CreateChecked(value);
					pos += 4;
					break;
				}
				case Granny2MemberType.BinormalInt8: {
					var value = MemoryMarshal.Read<sbyte>(pos) / (float) sbyte.MaxValue;
					stack[index] = T.CreateChecked(value);
					pos += 1;
					break;
				}
				case Granny2MemberType.NormalUInt8: {
					var value = MemoryMarshal.Read<byte>(pos) / (float) byte.MaxValue;
					stack[index] = T.CreateChecked(value);
					pos += 1;
					break;
				}
				case Granny2MemberType.BinormalInt16: {
					var value = MemoryMarshal.Read<short>(pos) / (float) short.MaxValue;
					stack[index] = T.CreateChecked(value);
					pos += 2;
					break;
				}
				case Granny2MemberType.NormalUInt16: {
					var value = MemoryMarshal.Read<ushort>(pos) / (float) ushort.MaxValue;
					stack[index] = T.CreateChecked(value);
					pos += 2;
					break;
				}
				default: throw new InvalidOperationException();
			}
		}
	}
}
