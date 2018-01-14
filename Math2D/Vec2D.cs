using System;

namespace Nima.Math2D
{
	public class Vec2D
	{
		private float[] m_Buffer;

		public float this[int index]
		{
			get
			{
				return m_Buffer[index];	
			}
			set
			{
				m_Buffer[index] = value;
			}
		}


		public float[] Values
		{
			get
			{
				return m_Buffer;
			}
		}

		public Vec2D()
		{
			m_Buffer = new float[2]{0.0f, 0.0f};
		}
		
		public Vec2D(float x, float y) : this()
		{
			m_Buffer[0] = x;
			m_Buffer[1] = y;
		}

		public Vec2D(Vec2D copy) : this()
		{
			m_Buffer[0] = copy.m_Buffer[0];
			m_Buffer[1] = copy.m_Buffer[1];
		}

		static public void Copy(Vec2D o, Vec2D a)
		{
			o[0] = a[0];
			o[1] = a[1];
		}

		public static Vec2D TransformMat2D(Vec2D o, Vec2D a, Mat2D m) 
		{
			float x = a[0];
	        float y = a[1];
	    	o[0] = m[0] * x + m[2] * y + m[4];
	    	o[1] = m[1] * x + m[3] * y + m[5];
	    	return o;
		}

		public static Vec2D TransformMat2(Vec2D o, Vec2D a, Mat2D m) 
		{
			float x = a[0];
	        float y = a[1];
	    	o[0] = m[0] * x + m[2] * y;
	    	o[1] = m[1] * x + m[3] * y;
	    	return o;
		}

		public static Vec2D Subtract(Vec2D o, Vec2D a, Vec2D b) 
		{
	    	o[0] = a[0] - b[0];
	    	o[1] = a[1] - b[1];
	    	return o;
		}

		public static Vec2D Add(Vec2D o, Vec2D a, Vec2D b) 
		{
	    	o[0] = a[0] + b[0];
	    	o[1] = a[1] + b[1];
	    	return o;
		}

		public static Vec2D Scale(Vec2D o, Vec2D a, float scale) 
		{
	    	o[0] = a[0] * scale;
	    	o[1] = a[1] * scale;
	    	return o;
		}

		public static Vec2D Lerp(Vec2D o, Vec2D a, Vec2D b, float f)
		{
			float ax = a[0];
			float ay = a[1];
			o[0] = ax + f * (b[0] - ax);
    		o[1] = ay + f * (b[1] - ay);
			return o;
		}

		public static float Length(Vec2D a) 
		{
	    	float x = a[0];
	        float y = a[1];
	    	return (float)Math.Sqrt(x*x + y*y);
		}

		public static float Distance(Vec2D a, Vec2D b) 
		{
	    	float x = b[0] - a[0];
	        float y = b[1] - a[1];
	    	return (float)Math.Sqrt(x*x + y*y);
		}

		public static void Negate(Vec2D a, Vec2D b) 
		{
	    	a[0] = -b[0];
			a[1] = -b[1];
		}

		public static void Normalize(Vec2D result, Vec2D a)
		{
			float x = a[0];
			float y = a[1];
			float len = x*x + y*y;
			if (len > 0.0f) 
			{
				len = (float)(1.0 / Math.Sqrt(len));
				result[0] = a[0] * len;
				result[1] = a[1] * len;
		    }
		}

		public static float Dot(Vec2D a, Vec2D b)
		{
			return a[0] * b[0] + a[1] * b[1];
		}
	}
}