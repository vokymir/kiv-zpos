using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using Visualizer;
using Zcu.Mve.ZMalik.Simplification;

class Program
{
    static void Main(string[] args)
    {
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        MeshData data = importer.Load("Assets/Models/lion.obj");

        Vertex[] origVertices = LaplacianSmoother.CloneVertices(data.Vertices);
        int[] origIndices = data.Indices;

        // Simplify using Quadrics target down to 1000 triangles
        BasicSimplificator simp = new BasicSimplificator(origVertices, origIndices, double.MaxValue, 1000, true);
        (Vertex[] simpVertices, int[] simpIndices) = simp.Simplify();

        NormalsGenerator.Apply(origVertices, origIndices, NormalsMode.Smooth);
        NormalsGenerator.Apply(simpVertices, simpIndices, NormalsMode.Smooth);

        VisualizerSession.Run(session =>
        {
            session.SetAnimation(
                [origVertices, simpVertices],
                [origIndices, simpIndices]
            );
        });
    }
}