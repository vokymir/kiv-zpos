namespace Core.Structures
{
	/// <summary>
	/// Resolves a vertex channel's per-vertex data, optionally rescaling it so that the
	/// combined min/max across every vertex and component maps onto a target range.
	/// A single global scale + offset is used so relative geometry (proportions) is preserved.
	/// </summary>
	public static class VertexNormalizer
	{
		/// <summary>
		/// Returns one array per vertex holding the channel's component values.
		/// When <paramref name="mode"/> is <see cref="ChannelNormalization.None"/>, the
		/// original arrays are returned by reference (no copy). Otherwise new arrays are
		/// allocated, leaving the source <paramref name="vertices"/> untouched.
		/// </summary>
		public static float[][] Resolve(Vertex[] vertices, string channelName, ChannelNormalization mode)
		{
			float[][] data = new float[vertices.Length][];
			for (int i = 0; i < vertices.Length; i++)
				data[i] = vertices[i].Channel(channelName);

			if (mode == ChannelNormalization.None || data.Length == 0)
				return data;

			float min = float.MaxValue, max = float.MinValue;
			foreach (float[] arr in data)
				foreach (float v in arr)
				{
					if (v < min) min = v;
					if (v > max) max = v;
				}

			float range = max - min;
			if (range <= 1e-12f)
				return data;

			(float scale, float offset) = mode switch
			{
				ChannelNormalization.SymmetricMinMax => (2f / range, (max + min) * 0.5f),
				ChannelNormalization.UnitMinMax       => (1f / range, min),
				_ => (1f, 0f),
			};

			var result = new float[data.Length][];
			for (int i = 0; i < data.Length; i++)
			{
				result[i] = new float[data[i].Length];
				for (int j = 0; j < data[i].Length; j++)
					result[i][j] = (data[i][j] - offset) * scale;
			}

			return result;
		}
	}
}
