using Core.Structures;

namespace Core.IO
{
	public enum NormalsSource { Generated, Loaded }

	/// <summary>Vertex and index arrays ready to upload to the GPU.</summary>
	public readonly record struct MeshData(Vertex[] Vertices, int[] Indices, NormalsSource NormalsSource);

	/// <summary>Loads mesh data from a file path.</summary>
	public interface IMeshImporter
	{
		MeshData Load(string path);
	}
}
