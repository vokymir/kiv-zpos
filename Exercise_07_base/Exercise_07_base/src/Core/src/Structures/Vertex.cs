using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Core.Structures
{
    /// <summary>
    /// Per-spec normalization applied to a vertex channel's data across all vertices
    /// before upload, while preserving the relative geometry (uniform scale + offset).
    /// </summary>
    public enum ChannelNormalization
    {
        /// <summary>No normalization; data is used as-is.</summary>
        None,
        /// <summary>Rescales the channel's combined min/max range to [-1, 1].</summary>
        SymmetricMinMax,
        /// <summary>Rescales the channel's combined min/max range to [0, 1].</summary>
        UnitMinMax,
    }

    /// <summary>
    /// A single vertex storing named float channels of arbitrary length.
    /// Each channel key (e.g. "position", "normal", "temperature") maps to a float array
    /// whose length determines the component count for that attribute.
    /// </summary>
    public struct Vertex(Dictionary<string, float[]> channels)
    {
        public Dictionary<string, float[]> Channels = channels;

        // -- Semantic accessors for standard channels ------------------------

        public readonly Vector3 Position
        {
            get
            {
                float[] p = Channels["position"];
                return new Vector3(p[0], p[1], p[2]);
            }
            set
            {
                if (Channels.TryGetValue("position", out float[]? p))
                {
                    p[0] = value.X;
                    p[1] = value.Y;
                    p[2] = value.Z;
                }
                else
                {
                    Channels["position"] = [value.X, value.Y, value.Z];
                }
            }
        }

        public readonly Vector3 Normal
        {
            get
            {
                float[] n = Channels["normal"];
                return new Vector3(n[0], n[1], n[2]);
            }
            set
            {
                if (Channels.TryGetValue("normal", out float[]? n))
                {
                    n[0] = value.X;
                    n[1] = value.Y;
                    n[2] = value.Z;
                }
                else
                {
                    Channels["normal"] = [value.X, value.Y, value.Z];
                }
            }
        }

        // -- Convenience factories for the standard position/normal layout ---

        public static Vertex Create(
                float x = 0, float y = 0, float z = 0,
                float xn = 0, float yn = 1, float zn = 0)
            => new(new Dictionary<string, float[]>
            {
                ["position"] = [x, y, z],
                ["normal"] = [xn, yn, zn],
            });

        public static Vertex Create(Vector3 position)
            => Create(position.X, position.Y, position.Z);

        public static Vertex Create(Vector3 position, Vector3 normal)
            => Create(position.X, position.Y, position.Z, normal.X, normal.Y, normal.Z);

        // -- Channel access helpers ------------------------------------------

        /// <summary>Returns the named channel, or an empty array if absent.</summary>
        public readonly float[] Channel(string name)
            => Channels.TryGetValue(name, out float[]? ch) ? ch : [];

        // -- Deep copy -------------------------------------------------------

        /// <summary>Returns a vertex whose channel arrays are independent copies.</summary>
        public readonly Vertex Clone()
            => new(Channels.ToDictionary(kv => kv.Key, kv => (float[])kv.Value.Clone()));

        /// <summary>Returns a deep copy of a vertex array with independent channel arrays.</summary>
        public static Vertex[] Clone(Vertex[] source)
        {
            Vertex[] copy = new Vertex[source.Length];
            for (int i = 0; i < source.Length; i++)
                copy[i] = source[i].Clone();
            return copy;
        }

        // -- GPU upload helpers ----------------------------------------------

        /// <summary>
        /// Flattens per-vertex channel data into a single interleaved float array.
        /// <paramref name="channels"/> holds one entry per output attribute, each
        /// being a per-vertex array of component values (already resolved/normalized).
        /// </summary>
        public static float[] ToFloats(int vertexCount, IReadOnlyList<float[][]> channels)
        {
            if (vertexCount == 0 || channels.Count == 0) return [];

            int stride = channels.Sum(c => c[0].Length);
            float[] result = new float[vertexCount * stride];

            for (int i = 0; i < vertexCount; i++)
            {
                int offset = i * stride;
                foreach (float[][] channel in channels)
                {
                    float[] data = channel[i];
                    data.CopyTo(result, offset);
                    offset += data.Length;
                }
            }

            return result;
        }
    }
}