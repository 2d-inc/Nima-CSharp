using System;

namespace Nima.Math2D
{
	public class Mat2D
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

		public Mat2D()
		{
			m_Buffer = new float[6]{1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f};
		}

		public Mat2D(Mat2D copy) : this()
		{
			m_Buffer[0] = copy.m_Buffer[0];
			m_Buffer[1] = copy.m_Buffer[1];
			m_Buffer[2] = copy.m_Buffer[2];
			m_Buffer[3] = copy.m_Buffer[3];
			m_Buffer[4] = copy.m_Buffer[4];
			m_Buffer[5] = copy.m_Buffer[5];
		}

		static public void FromRotation(Mat2D o, float rad)
		{
			float s = (float)Math.Sin(rad);
			float c = (float)Math.Cos(rad);
			o[0] = c;
			o[1] = s;
			o[2] = -s;
			o[3] = c;
			o[4] = 0;
			o[5] = 0;
		}

		static public void Scale(Mat2D o, Mat2D a, Vec2D v)
		{
			float a0 = a[0], a1 = a[1], a2 = a[2], a3 = a[3], a4 = a[4], a5 = a[5], v0 = v[0], v1 = v[1];
			o[0] = a0 * v0;
			o[1] = a1 * v0;
			o[2] = a2 * v1;
			o[3] = a3 * v1;
			o[4] = a4;
			o[5] = a5;
		}

		static public void Multiply(Mat2D o, Mat2D a, Mat2D b) 
		{
			float a0 = a[0], a1 = a[1], a2 = a[2], a3 = a[3], a4 = a[4], a5 = a[5],
					b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3], b4 = b[4], b5 = b[5];
			o[0] = a0 * b0 + a2 * b1;
			o[1] = a1 * b0 + a3 * b1;
			o[2] = a0 * b2 + a2 * b3;
			o[3] = a1 * b2 + a3 * b3;
			o[4] = a0 * b4 + a2 * b5 + a4;
			o[5] = a1 * b4 + a3 * b5 + a5;
		}

		static public void Copy(Mat2D o, Mat2D a)
		{
			o[0] = a[0];
			o[1] = a[1];
			o[2] = a[2];
			o[3] = a[3];
			o[4] = a[4];
			o[5] = a[5];
		}

		static public bool Invert(Mat2D o, Mat2D a)
		{
			float aa = a[0], ab = a[1], ac = a[2], ad = a[3], atx = a[4], aty = a[5];

			float det = aa * ad - ab * ac;
			if(det == 0.0f)
			{
				return false;
			}
			det = 1.0f / det;

			o[0] = ad * det;
			o[1] = -ab * det;
			o[2] = -ac * det;
			o[3] = aa * det;
			o[4] = (ac * aty - ad * atx) * det;
			o[5] = (ab * atx - aa * aty) * det;
			return true;
		}

		static public void GetScale(Mat2D m, Vec2D s)
		{
			float x = m[0];
			float y = m[1];
			s[0] = Math.Sign(x) * (float)Math.Sqrt(x*x + y*y);

			x = m[2];
			y = m[3];
			s[1] = Math.Sign(y) * (float)Math.Sqrt(x*x + y*y);
		}

		static public float Decompose(Mat2D a, Vec2D s)
		{
			float a0 = a[0], a1 = a[1], a2 = a[2], a3 = a[3];
			float det = a0 * a3 - a2 * a1;
			float sx = (float)Math.Sqrt(a0*a0 + a1*a1);
			float sy = (float)Math.Sqrt(a2*a2 + a3*a3);
			if(det < 0)
			{
				sx = -sx;
			}

			s[0] = sx;
			s[1] = sy;

			return (float)Math.Atan2(a1/sx, a0/sx);
		}

		static public void Identity(Mat2D mat)
		{
			mat[0] = 1.0f;
			mat[1] = 0.0f;
			mat[2] = 0.0f;
			mat[3] = 1.0f;
			mat[4] = 0.0f;
			mat[5] = 0.0f;
		}

		static public void Decompose(Mat2D m, TransformComponents result)
		{
			float m0 = m[0], m1 = m[1], m2 = m[2], m3 = m[3];

			float rotation = (float)Math.Atan2(m1, m0);
			float denom = m0*m0 + m1*m1;
			float scaleX = (float)Math.Sqrt(denom);
			float scaleY = (m0 * m3 - m2 * m1) / scaleX;
			float skewX = (float)Math.Atan2(m0 * m2 + m1 * m3, denom);

			result[0] = m[4];
			result[1] = m[5];
			result[2] = scaleX;
			result[3] = scaleY;
			result[4] = rotation;
			result[5] = skewX;
		}

		static public void Compose(Mat2D m, TransformComponents result)
		{
			float r = result[4];

			if(r != 0.0)
			{
				Mat2D.FromRotation(m, r);
			}
			else
			{
				Mat2D.Identity(m);
			}
			m[4] = result[0];
			m[5] = result[1];
			Mat2D.Scale(m, m, result.Scale);

			float sk = result[5];
			if(sk != 0.0)
			{
				m[2] = m[0] * sk + m[2];
				m[3] = m[1] * sk + m[3];
			}
		}

	}
}