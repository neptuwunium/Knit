using Knit.TypeDefinitions;
using GL = GLTF.Scaffold;

namespace Knit.glTF;

public sealed class GrannyGLTF : IDisposable {
	public GrannyGLTF(string path) : this(new FileInfo(path)) { }
	public GrannyGLTF(FileInfo info) : this(info.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
	public GrannyGLTF(Stream stream) : this(new Granny2File(stream)) { }

	public GrannyGLTF(Granny2File stream) {
		File = stream;
		Resource = stream.LoadRoot() ?? throw new InvalidOperationException();
	}

	public Granny2File File { get; }
	public GrannyFileRoot Resource { get; }

	public void Dispose() {
		File.Dispose();
	}

	public void Write(string targetFile) {
		var bufferData = new MemoryStream();
		var root = new GL.Root();

		var meshMap = new Dictionary<Mesh, (GL.Mesh Mesh, int Id)>();
		var vertMap = new Dictionary<VertexData, Dictionary<string, int>>();

		foreach (var vertData in Resource.Vertices) {
			if (vertData.Vertices.Length == 0) { }
		}

		foreach (var meshData in Resource.Meshes) {
			var tri = meshData.PrimaryTopology;
			var vert = meshData.PrimaryVertexData;
			if (tri == null || vert == null || !vertMap.TryGetValue(vert, out var attributes)) {
				continue;
			}

			var (mesh, _) = meshMap[meshData] = root.CreateMesh();
			mesh.Name = meshData.Name;

			foreach (var group in tri.Groups) {
				var prim = new GL.Primitive {
					Mode = GL.PrimitiveMode.Triangles,
					Attributes = attributes,
				};
			}
		}

		foreach (var model in Resource.Models) { }
	}
}
