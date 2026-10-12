namespace Core
{
	public static class Mathematics
	{
		private static readonly float _piOver180 = (float)Math.PI / 180f;
		private static readonly float _180OverPi = 180f / (float)Math.PI;

		/// <summary>Linearly interpolates between <paramref name="a"/> and <paramref name="b"/> by factor <paramref name="t"/>.</summary>
		public static float Lerp(float a, float b, float t) => a + (b - a) * t;

		/// <summary>Converts degrees to radians.</summary>
		public static float DegreesToRadians(float deg) => deg * _piOver180;

		/// <summary>Converts radians to degrees.</summary>
		public static float RadiansToDegrees(float rad) => rad * _180OverPi;
	}
}
