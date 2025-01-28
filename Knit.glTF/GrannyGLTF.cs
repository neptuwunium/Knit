// SPDX-FileCopyrightText: 2025 Legiayayana
//
// SPDX-License-Identifier: EUPL-1.2

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Knit.TypeDefinitions;
using GL = GLTF.Scaffold;

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

public sealed class GrannyGLTF : IDisposable {
	public GrannyGLTF(GrannyFileRoot resource) {
		Resource = resource;
		RootNode = Root.CreateNode().Node;
		if (Resource.Name.Length > 0) {
			RootNode.Name = Path.GetFileNameWithoutExtension(Resource.Name.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[^1]);
		}

		Scale = 1 / Math.Max(1, Resource.ArtToolInfo.UnitsPerMeter);

		Debug.Assert(Resource.Models.Count > 0);

		foreach (var skeleton in Resource.Skeletons) {
			CreateSkeleton(skeleton);
		}

		foreach (var model in Resource.Models) {
			CreateModel(model);
		}
	}


	public bool OneBoned { get; set; } = true;
	public bool GenerateNormals { get; set; } = true;
	public GrannyFileRoot Resource { get; }
	public GL.Root Root { get; } = new();
	public GL.Node RootNode { get; }
	public float Scale { get; }
	public MemoryStream Buffer { get; } = new();
	public Dictionary<string, int> MeshMap { get; } = [];
	public Dictionary<string, int> MaterialMap { get; } = [];
	public Dictionary<VertexData, Dictionary<string, int>> VertexMap { get; } = [];
	public Dictionary<string, int> TextureMap { get; } = [];
	public Dictionary<int, Dictionary<string, int>> BoneMap { get; } = [];
	public Dictionary<string, int> SkeletonMap { get; } = [];
	public Dictionary<string, int> AnimationMap { get; } = [];

	internal static JsonSerializerOptions Options =>
		new() {
			WriteIndented = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			IgnoreReadOnlyFields = true,
			IgnoreReadOnlyProperties = true,
			NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

	public void Dispose() {
		Buffer.Dispose();
	}

	public int CreateSkeleton(Skeleton skeleton) {
		if (SkeletonMap.TryGetValue(skeleton.Name, out var id)) {
			return id;
		}

		var (skin, skinId) = Root.CreateSkin();
		SkeletonMap[skeleton.Name] = skinId;

		skin.Name = skeleton.Name;
		var matrices = new Matrix4x4[skeleton.Bones.Count];
		var hierarchy = new List<GL.Node>();
		var boneMap = new Dictionary<string, int>();

		for (var index = 0; index < skeleton.Bones.Count; index++) {
			var boneData = skeleton.Bones[index];
			GL.Node? bone;
			int boneId;

			if (boneData.ParentIndex == -1) {
				(bone, boneId) = RootNode.CreateNode(Root);
				skin.Skeleton = boneId;
			} else {
				(bone, boneId) = hierarchy[boneData.ParentIndex].CreateNode(Root);
			}

			skin.Joints.Add(boneId);
			boneMap[boneData.Name] = hierarchy.Count;
			hierarchy.Add(bone);
			matrices[index] = boneData.InverseTransform * Matrix4x4.CreateScale(Scale);
			boneData.Transform.ToGLTF(bone, Scale);
			bone.Name = boneData.Name;
		}

		skin.InverseBindMatrices = Root.CreateAccessor(matrices.AsSpan(), Buffer, null, GL.AccessorType.MAT4, GL.AccessorComponentType.Float, 0).Id;
		BoneMap[skinId] = boneMap;

		return skinId;
	}

	public void CreateModel(Model model) {
		var (meshNode, _) = RootNode.CreateNode(Root);

		int? skinId = model.Skeleton != null ? CreateSkeleton(model.Skeleton) : null;
		model.Transform.ToGLTF(meshNode, Scale);
		meshNode.Name = model.Name;

		foreach (var binding in model.MeshBindings) {
			if (binding.Mesh is not { PrimaryTopology: not null, PrimaryVertexData.Vertices.Length: > 0 }) {
				continue;
			}

			CreateMesh(binding.Mesh, skinId, meshNode);
		}
	}

	public int CreateMesh(Mesh mesh, int? skinId, GL.Node parentNode) {
		if (MeshMap.TryGetValue(mesh.Name, out var id)) {
			return id;
		}

		if (mesh.PrimaryTopology is null || mesh.PrimaryVertexData is not { Vertices.Length: > 0 }) {
			throw new InvalidOperationException();
		}

		var (meshNode, meshId) = parentNode.CreateNode(Root);
		MeshMap[mesh.Name] = meshId;
		meshNode.Name = mesh.Name;
		meshNode.Skin = skinId;
		meshNode.Mesh = CreatePrimitive(mesh.PrimaryTopology, mesh.PrimaryVertexData, mesh.MaterialBindings.Select(x => x.Material).ToList(), mesh.BoneBindings, skinId is { } _skinId ? BoneMap[_skinId] : []);

		return meshId;
	}

	public int? CreatePrimitive(TriTopology topology, VertexData vertexData, List<Material?> materials, List<MeshBone> bones, Dictionary<string, int> boneMap) {
		var (mesh, meshId) = Root.CreateMesh();

		Span<byte> bytes;
		GL.AccessorComponentType type;
		int stride;
		if (topology.Indices32.Length > 0) {
			bytes = MemoryMarshal.AsBytes(topology.Indices32.AsSpan());
			type = GL.AccessorComponentType.UnsignedInt;
			stride = 4;
		} else {
			bytes = MemoryMarshal.AsBytes(topology.Indices16.AsSpan());
			type = GL.AccessorComponentType.UnsignedShort;
			stride = 2;
		}

		var vertexAttributes = CreateVertexAttributes(vertexData, bytes, bones, boneMap, type);

		foreach (var group in topology.Groups) {
			if (group.Count == 0) {
				continue;
			}

			var prim = new GL.Primitive {
				Mode = GL.PrimitiveMode.Triangles,
				Attributes = vertexAttributes,
			};

			var view = Root.CreateBufferView(bytes.Slice(group.Start * 3 * stride, group.Count * 3 * stride), Buffer, null, GL.BufferViewTarget.ElementArrayBuffer).Id;

			prim.Indices = Root.CreateAccessor(view, group.Count * 3, 0, GL.AccessorType.SCALAR, type).Id;
			prim.Material = CreateMaterial(materials[group.MaterialIndex]);

			mesh.Primitives.Add(prim);
		}

		return meshId;
	}

	public int? CreateMaterial(Material? material) {
		if (material == null) {
			return null;
		}

		if (MaterialMap.TryGetValue(material.Name, out var materialId)) {
			return materialId;
		}

		var (mat, id) = Root.CreateMaterial();
		MaterialMap[material.Name] = id;

		mat.Name = material.Name;
		// todo.
		return id;
	}

	public Dictionary<string, int> CreateVertexAttributes(VertexData vertexData, Span<byte> faces, List<MeshBone> bones, Dictionary<string, int> boneMap, GL.AccessorComponentType type) {
		if (VertexMap.TryGetValue(vertexData, out var attributes)) {
			return attributes;
		}

		attributes = new Dictionary<string, int>();
		var vertices = vertexData.Vertices;
		var attributePresence = vertices[0].Attributes;
		using var alloc = new VertexAllocation(vertices.Length);
		var minPos = new Vector3(float.MaxValue);
		var maxPos = new Vector3(float.MinValue);
		for (var index = 0; index < vertices.Length; ++index) {
			var indices = vertices[index].BoneIndices;
			var weights = vertices[index].BoneWeights;
			var localIndices = ((Span<short>) indices)[..4];
			var localWeights = ((Span<float>) weights)[..4];

			var pos = vertices[index].Position;
			if (bones.Count == 0) {
				pos *= Scale;
			}

			MemoryMarshal.Write(alloc.Position[(index * 12)..], pos);
			minPos = Vector3.Min(minPos, pos);
			maxPos = Vector3.Max(maxPos, pos);
			var vertNormal = vertices[index].Normal;
			if (vertNormal.X > 0 || vertNormal.Y > 0 || vertNormal.Z > 0) {
				MemoryMarshal.Write(alloc.Normal[(index * 12)..], vertNormal);
			}

			var tangent = vertices[index].Tangent;
			if (tangent.X > 0 || tangent.Y > 0 || tangent.Z > 0) {
				var tangent3 = Vector3.Normalize(Unsafe.As<Vector4, Vector3>(ref tangent));
				tangent = new Vector4(tangent3, tangent.W > 0 ? 1.0f : -1.0f);
			} else {
				tangent = new Vector4(Vector3.Normalize(new Vector3(tangent.W)), 1.0f);
			}

			MemoryMarshal.Write(alloc.Tangent[(index * 16)..], tangent);
			MemoryMarshal.Write(alloc.Color0[(index * 12)..], vertices[index].DiffuseColor);
			MemoryMarshal.Write(alloc.Color1[(index * 12)..], vertices[index].SpecularColor);
			MemoryMarshal.Write(alloc.UV0[(index * 8)..], vertices[index].TextureCoordinates[0]);
			MemoryMarshal.Write(alloc.UV1[(index * 8)..], vertices[index].TextureCoordinates[1]);
			MemoryMarshal.Write(alloc.UV2[(index * 8)..], vertices[index].TextureCoordinates[2]);
			MemoryMarshal.Write(alloc.UV3[(index * 8)..], vertices[index].TextureCoordinates[3]);
			MemoryMarshal.Write(alloc.UV4[(index * 8)..], vertices[index].TextureCoordinates[4]);
			MemoryMarshal.Write(alloc.UV5[(index * 8)..], vertices[index].TextureCoordinates[5]);
			MemoryMarshal.Write(alloc.UV6[(index * 8)..], vertices[index].TextureCoordinates[6]);
			MemoryMarshal.Write(alloc.UV7[(index * 8)..], vertices[index].TextureCoordinates[7]);

			if (bones.Count > 0 && boneMap.Count > 0) {
				var modelIndices = MemoryMarshal.Cast<byte, short>(alloc.Joint[(index * 8)..]);
				var seenIds = new HashSet<int>();
				for (var boneEntry = 0; boneEntry < Math.Min(vertices[index].ReferencedBoneCount, 4); ++boneEntry) {
					var boneId = boneMap[bones[localIndices[boneEntry]].Name];
					if (seenIds.Add(boneId) || (attributePresence & VertexAttributePresence.BoneWeights) != 0) {
						modelIndices[boneEntry] = (short) boneId;
					} else {
						modelIndices[boneEntry] = 0;
					}

					if (OneBoned && (attributePresence & VertexAttributePresence.BoneWeights) == 0) {
						break;
					}
				}

				if ((attributePresence & VertexAttributePresence.BoneWeights) == 0) {
					for (var boneEntry = 0; boneEntry < seenIds.Count; ++boneEntry) {
						localWeights[boneEntry] = 1.0f / seenIds.Count;
					}
				}

				MemoryMarshal.AsBytes(localWeights).CopyTo(alloc.Weight[(index * 16)..]);
			}
		}

		if ((attributePresence & VertexAttributePresence.Position) != 0) {
			var (pos, id) = Root.CreateAccessor(alloc.Position, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC3, GL.AccessorComponentType.Float, 12, vertices.Length);
			attributes["POSITION"] = id;
			pos.Max = GL.Extensions.ToGLTF(maxPos);
			pos.Min = GL.Extensions.ToGLTF(minPos);
		}

		var normal = alloc.Normal;
		var hasNormal = (attributePresence & VertexAttributePresence.Normal) != 0;
		if (!hasNormal && GenerateNormals) {
			if (type == GL.AccessorComponentType.UnsignedInt) {
				CalculateNormals<uint>(ref normal, alloc.Position, faces);
			} else {
				CalculateNormals<ushort>(ref normal, alloc.Position, faces);
			}

			hasNormal = true;
		}

		if (hasNormal) {
			attributes["NORMAL"] = Root.CreateAccessor(alloc.Normal, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC3, GL.AccessorComponentType.Float, 12, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.Tangent) != 0) {
			attributes["TANGENT"] = Root.CreateAccessor(alloc.Tangent, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC4, GL.AccessorComponentType.Float, 16, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.DiffuseColor) != 0) {
			attributes["COLOR_0"] = Root.CreateAccessor(alloc.Color0, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC4, GL.AccessorComponentType.Float, 16, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.SpecularColor) != 0) {
			attributes["COLOR_1"] = Root.CreateAccessor(alloc.Color1, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC4, GL.AccessorComponentType.Float, 16, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates0) != 0) {
			attributes["TEXCOORD_0"] = Root.CreateAccessor(alloc.UV0, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates1) != 0) {
			attributes["TEXCOORD_1"] = Root.CreateAccessor(alloc.UV1, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates2) != 0) {
			attributes["TEXCOORD_2"] = Root.CreateAccessor(alloc.UV2, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates3) != 0) {
			attributes["TEXCOORD_3"] = Root.CreateAccessor(alloc.UV3, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates4) != 0) {
			attributes["TEXCOORD_4"] = Root.CreateAccessor(alloc.UV4, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates5) != 0) {
			attributes["TEXCOORD_5"] = Root.CreateAccessor(alloc.UV5, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates6) != 0) {
			attributes["TEXCOORD_6"] = Root.CreateAccessor(alloc.UV6, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if ((attributePresence & VertexAttributePresence.TextureCoordinates7) != 0) {
			attributes["TEXCOORD_7"] = Root.CreateAccessor(alloc.UV7, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC2, GL.AccessorComponentType.Float, 8, vertices.Length).Id;
		}

		if (bones.Count > 0 && boneMap.Count > 0 && (attributePresence & VertexAttributePresence.BoneIndices) != 0) {
			attributes["JOINTS_0"] = Root.CreateAccessor(alloc.Joint, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC4, GL.AccessorComponentType.UnsignedShort, 8, vertices.Length).Id;
			attributes["WEIGHTS_0"] = Root.CreateAccessor(alloc.Weight, Buffer, GL.BufferViewTarget.ArrayBuffer, GL.AccessorType.VEC4, GL.AccessorComponentType.Float, 16, vertices.Length).Id;
		}

		VertexMap[vertexData] = attributes;

		return attributes;
	}

	public static void CalculateNormals<T>(ref Span<byte> normal, Span<byte> position, Span<byte> faces) where T : struct, INumberBase<T> {
		normal.Clear();
		var normalVec3 = MemoryMarshal.Cast<byte, Vector3>(normal);
		var positionVec3 = MemoryMarshal.Cast<byte, Vector3>(position);
		CalculateNormals<T>(ref normalVec3, positionVec3, faces);
	}

	public static void CalculateNormals<T>(ref Span<Vector3> normal, Span<Vector3> position, Span<byte> faces) where T : struct, INumberBase<T> {
		var cast = MemoryMarshal.Cast<byte, T>(faces);
		for (var i = 0; i < cast.Length; i += 3) {
			var v1 = int.CreateChecked(cast[i]);
			var v2 = int.CreateChecked(cast[i + 1]);
			var v3 = int.CreateChecked(cast[i + 2]);
			var a = position[v1];
			var b = position[v2];
			var c = position[v3];
			var n = Vector3.Cross(b - a, c - a);
			normal[v1] += n;
			normal[v2] += n;
			normal[v3] += n;
		}

		for (var i = 0; i < normal.Length; i++) {
			// should be invert X here?
			normal[i] = Vector3.Normalize(normal[i]);
		}
	}

	public void Write(string path) {
		var bufferPath = Path.ChangeExtension(path, ".bin");
		Root.Buffers ??= [];
		Root.Buffers.Add(new GL.Buffer {
			ByteLength = Buffer.Length,
			Uri = Path.GetFileName(bufferPath),
		});

		using var buffer = new FileStream(bufferPath, FileMode.Create, FileAccess.Write, FileShare.Write);
		Buffer.Position = 0;
		Buffer.CopyTo(buffer);

		using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
		JsonSerializer.Serialize(file, Root, Options);
		file.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
	}
}
