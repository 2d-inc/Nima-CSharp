using System.IO;
using System;

namespace Nima.Animation
{
	// AE style curve interpolation
	public class ValueTimeCurveInterpolator : KeyFrameInterpolator
	{
		private double m_InFactor;
		private float m_InValue;
		private double m_OutFactor;
		private float m_OutValue;

		public override bool SetNextFrame(KeyFrameWithInterpolation frame, KeyFrame nextFrame)
		{
			// This frame is a hold, return false to remove the interpolator.
			// We store it in the first place as when it gets called as the nextFrame parameter (in a previous call)
			// we still read out the in factor and in value (see below where nextInValue and nextInFactor are defined).
			if(frame.InterpolationType == KeyFrame.InterpolationTypes.Hold)
			{
				return false;
			}
			
			// Just a sanity check really, both keyframes need to be numeric.
			KeyFrameNumeric ourFrame = frame as KeyFrameNumeric;
			KeyFrameNumeric next = nextFrame as KeyFrameNumeric;
			if(ourFrame == null || next == null)
			{
				return false;
			}

			// We are not gauranteed to have a next interpolator (usually when the next keyframe is linear).
			ValueTimeCurveInterpolator nextInterpolator = null;

			float timeRange = next.Time - ourFrame.Time;
			float outTime = (float)(ourFrame.Time + timeRange * m_OutFactor);

			float nextInValue = 0.0f;
			double nextInFactor = 0.0f;

			// Get the invalue and infactor from the next interpolator (this is where hold keyframes get their interpolator values processed too).
			if((nextInterpolator = next.Interpolator as ValueTimeCurveInterpolator) != null)
			{
				nextInValue = nextInterpolator.m_InValue;
				nextInFactor = nextInterpolator.m_InFactor;
				//this._Curve = new BezierAnimationCurve([this._Time, this._Value], [outTime, this._OutValue], [inTime, nxt._InValue], [nxt._Time, nxt._Value]);
			}
			else
			{
				// Happens when next is linear.
				nextInValue = next.Value;
			}

			float inTime = (float)(next.Time - timeRange * nextInFactor);

			// Finally we can generate the curve.
			InitializeCurve(ourFrame.Time, ourFrame.Value, outTime, m_OutValue, inTime, nextInValue, next.Time, next.Value);
			//this._Curve = new BezierAnimationCurve([ourFrame.Time, ourFrame.Value], [outTime, m_OutValue], [inTime, nextInValue], [next.Time, next.Value]);
			return true;
		}

		public static ValueTimeCurveInterpolator Read(BinaryReader reader, KeyFrame.InterpolationTypes type)
		{
			ValueTimeCurveInterpolator vtci = new ValueTimeCurveInterpolator();
			switch(type)
			{
				case KeyFrame.InterpolationTypes.Mirrored:
				case KeyFrame.InterpolationTypes.Asymmetric:
				case KeyFrame.InterpolationTypes.Disconnected:
					vtci.m_InFactor = reader.ReadDouble();
					vtci.m_InValue = reader.ReadSingle();
					vtci.m_OutFactor = reader.ReadDouble();
					vtci.m_OutValue = reader.ReadSingle();
					return vtci;

				case KeyFrame.InterpolationTypes.Hold:
					vtci.m_InFactor = reader.ReadDouble();
					vtci.m_InValue = reader.ReadSingle();
					vtci.m_OutFactor = 0.0f;
					vtci.m_OutValue = 0.0f;
					return vtci;
			}

			return null;
		}

		const double m_Epsilon = Double.Epsilon;
		double m_X0;
		double m_Y0;

		double m_X1;
		double m_Y1;

		double m_X2;
		double m_Y2;

		double m_X3;
		double m_Y3;

		double m_E;
		double m_F;
		double m_G;
		double m_H;

		private void InitializeCurve(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
		{
			m_X0 = x0;
			m_Y0 = y0;

			m_X1 = x1;
			m_Y1 = y1;

			m_X2 = x2;
			m_Y2 = y2;

			m_X3 = x3;
			m_Y3 = y3;

			m_E = y3 - 3.0 * y2 + 3.0 * y1 - y0;
			m_F = 3.0 * y2 - 6.0 * y1 + 3.0 * y0;
			m_G = 3.0 * y1 - 3.0 * y0;
			m_H = y0;
		}

		private double CubicRoot(double v)
		{
			if (v < 0.0)
			{
				return -Math.Pow(-v, 1.0 / 3.0);
			}
			return Math.Pow(v, 1.0 / 3.0);
		}

		// http://stackoverflow.com/questions/27176423/function-to-solve-cubic-equation-analytically
		private int SolveCubic(double a, double b, double c, double d, double[] roots)
		{
			if (Math.Abs(a) < m_Epsilon)
			{
				// Quadratic case, ax^2+bx+c=0
				a = b; b = c; c = d;
				if (Math.Abs(a) < m_Epsilon)
				{
					// Linear case, ax+b=0
					a = b;
					b = c;
					// Degenerate case
					if (Math.Abs(a) < m_Epsilon)
					{
						return 0;
					}
					else
					{
						roots[0] = -b / a;
						return 1;
					}
				}
				else
				{
					double D = b * b - 4.0 * a * c;
					if (Math.Abs(D) < m_Epsilon)
					{
						roots[0] = -b / (2.0 * a);
						return 1;
					}
					else if (D > 0.0)
					{
						roots[0] = (-b + Math.Sqrt(D)) / (2.0 * a);
						roots[1] = (-b - Math.Sqrt(D)) / (2.0 * a);
						return 2;
					}
				}
				return 0;
			}
			else
			{
				int numRoots = 0;
				// Convert to depressed cubic t^3+pt+q = 0 (subst x = t - b/3a)
				double p = (3.0 * a * c - b * b) / (3.0 * a * a);
				double q = (2.0 * b * b * b - 9.0 * a * b * c + 27.0 * a * a * d) / (27.0 * a * a * a);

				if (Math.Abs(p) < m_Epsilon)
				{
					// p = 0 -> t^3 = -q -> t = -q^1/3
					roots[0] = CubicRoot(-q);
					numRoots = 0;
				}
				else if (Math.Abs(q) < m_Epsilon)
				{
					// q = 0 -> t^3 + pt = 0 -> t(t^2+p)=0
					roots[0] = 0.0;
					if (p < 0.0)
					{
						roots[1] = Math.Sqrt(-p);
						roots[2] = -Math.Sqrt(-p);
						numRoots = 3;
					}
					else
					{
						numRoots = 1;
					}
				}
				else
				{
					double D = q * q / 4.0 + p * p * p / 27.0;
					if (Math.Abs(D) < m_Epsilon)
					{
						// D = 0 -> two roots
						roots[0] = -1.5 * q / p;
						roots[1] = 3.0 * q / p;
						numRoots = 2;
					}
					else if (D > 0.0)
					{
						// Only one real root
						double u = CubicRoot(-q / 2.0 - Math.Sqrt(D));
						roots[0] = u - p / (3.0 * u);
						numRoots = 1;
					}
					else
					{
						// D < 0, three roots, but needs to use complex numbers/trigonometric solution
						double u = 2.0 * Math.Sqrt(-p / 3.0);
						double t = Math.Acos(3.0 * q / p / u) / 3.0; // D < 0 implies p < 0 and acos argument in [-1..1]
						double k = 2.0 * Math.PI / 3.0;
						roots[0] = u * Math.Cos(t);
						roots[1] = u * Math.Cos(t - k);
						roots[2] = u * Math.Cos(t - 2.0 * k);
						numRoots = 3;
					}
				}

				// Convert back from depressed cubic
				for (int i = 0; i < numRoots; i++)
				{
					roots[i] -= b / (3.0 * a);
				}

				return numRoots;
			}
		}

		public double Get(double x)
		{
			double p0 = m_X0-x;
			double p1 = m_X1-x;
			double p2 = m_X2-x;
			double p3 = m_X3-x;

			double a = p3 - 3.0 * p2 + 3.0 * p1 - p0;
			double b = 3.0 * p2 - 6.0 * p1 + 3.0 * p0;
			double c = 3.0 * p1 - 3.0 * p0;
			double d = p0;

			double[] roots = new double[3]{0.0, 0.0, 0.0};
			double numRoots = SolveCubic(a, b, c, d, roots);
			double t = 0.0;
			// Find first valid root.
			for(int i = 0; i < numRoots; i++)
			{
				double r = roots[i];
				if(r >= 0.0 && r <= 1.0)
				{
					t = r;
					break;
				}
			}
			return m_E*(t*t*t) + m_F*(t*t) + m_G*t + m_H;
		}
	}
}