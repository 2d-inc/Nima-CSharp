using System;

namespace Nima.Math2D
{
	public static class	Vec2D
	{
		public static float[] Create()
		{
			return new float[2];
		}

		public static float[] Clone(float[] o)
		{
			return new float[2] { o[0], o[1] };
		}

		public static float[] Create(float x, float y)
		{
			float[] r = new float[2];
			r[0] = x;
			r[1] = y;
			return r;
		}
	}
}