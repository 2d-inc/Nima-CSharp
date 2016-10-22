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

		public static float[] TransformMat2D(float[] o, float[] a, float[] m) 
		{
			float x = a[0];
	        float y = a[1];
	    	o[0] = m[0] * x + m[2] * y + m[4];
	    	o[1] = m[1] * x + m[3] * y + m[5];
	    	return o;
		}

		public static float[] Subtract(float[] o, float[] a, float[] b) 
		{
	    	o[0] = a[0] - b[0];
	    	o[1] = a[1] - b[1];
	    	return o;
		}

		public static float Length(float[] a) 
		{
	    	float x = a[0];
	        float y = a[1];
	    	return (float)Math.Sqrt(x*x + y*y);
		}
	}
}