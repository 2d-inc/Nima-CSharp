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


		public static Vec2D TransformMat2D(Vec2D o, Vec2D a, Mat2D m) 
		{
			float x = a[0];
	        float y = a[1];
	    	o[0] = m[0] * x + m[2] * y + m[4];
	    	o[1] = m[1] * x + m[3] * y + m[5];
	    	return o;
		}

		public static Vec2D Subtract(Vec2D o, Vec2D a, Vec2D b) 
		{
	    	o[0] = a[0] - b[0];
	    	o[1] = a[1] - b[1];
	    	return o;
		}

		public static float Length(Vec2D a) 
		{
	    	float x = a[0];
	        float y = a[1];
	    	return (float)Math.Sqrt(x*x + y*y);
		}
	}
}