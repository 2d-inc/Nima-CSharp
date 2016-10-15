using System.IO;
using System;

namespace Nima.Animation
{
	// AE style curve interpolation
	public class ValueTimeCurveInterpolator : KeyFrameInterpolator
	{
		private float m_Value;
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

			m_E = y3 - 3.0*y2 + 3.0*y1 - y0;    
			m_F = 3.0*y2 - 6.0*y1 + 3.0*y0;             
			m_G = 3.0*y1 - 3.0*y0;             
			m_H = y0;
		}

		private double CubicRoot(double v) 
		{ 
			if(v<0.0) 
			{
				return -Math.Pow(-v,1.0/3.0); 
			}
			return Math.Pow(v,1.0/3.0); 
		}

		// http://www.trans4mind.com/personal_development/mathematics/polynomials/cubicAlgebra.htm
		private double SolveCubic(double a, double b, double c, double d)
		{
			b /= a;
			c /= a;
			d /= a;


			double p = (3.0 * c - b * b) / 3.0;
			double q = (2.0 * b * b * b - 9.0 * b * c + 27.0 * d) / 27.0;

			if (p == 0.0) 
			{
			    return CubicRoot(-q);
			} 
			else if (q == 0) 
			{
				var r = Math.Round(Math.Sqrt(-p), 15);
				if(r >= 0 && r <= 1.0)
				{
					return r;
				}
				r = Math.Round(-Math.Sqrt(-p), 15);
				if(r >= 0 && r <= 1.0)
				{
					return r;
				}
				return 0;
			} 
			else 
			{
			    double discriminant = Math.Pow(q / 2.0, 2.0) + Math.Pow(p / 3.0, 3.0);
			    if (discriminant == 0) 
			    {
					return Math.Round(CubicRoot(q / 2.0) - b / 3.0, 15);
			    } 
			    else if (discriminant > 0) 
			    {
			        return Math.Round(CubicRoot(-(q / 2.0) + Math.Sqrt(discriminant)) - CubicRoot((q / 2.0) + Math.Sqrt(discriminant)) - b / 3, 15);
			    } 
			    else 
			    {
			        double r = Math.Sqrt( Math.Pow(-(p/3.0), 3.0) );
			        double phi = Math.Acos(-(q / (2.0 * Math.Sqrt(Math.Pow(-(p / 3.0), 3.0)))));

			        double s = 2.0 * CubicRoot(r);

			        // favor phi + 4 * PI first as it seams to generally be right given our system.
					r = Math.Round(s * Math.Cos((phi + 4.0 * Math.PI) / 3.0) - b / 3.0, 15);
					if(r >= 0.0 && r <= 1.0)
					{
						return r;
					}
					r = Math.Round(s * Math.Cos(phi / 3.0) - b / 3.0, 15);
					if(r >= 0.0 && r <= 1.0)
					{
						return r;
					}
					r = Math.Round(s * Math.Cos((phi + 2 * Math.PI) / 3.0) - b / 3.0, 15);
					if(r >= 0.0 && r <= 1.0)
					{
						return r;
					}
					return 0.0;
			    }
			}
		}

		public double Get(double x)
		{
			//console.log("GET FOR X", x);
			// First solve for t given x.
			double p0 = m_X0-x;
			double p1 = m_X1-x;
			double p2 = m_X2-x;
			double p3 = m_X3-x;

			double a = p3 - 3.0 * p2 + 3.0 * p1 - p0;
			double b = 3.0 * p2 - 6.0 * p1 + 3.0 * p0;
			double c = 3.0 * p1 - 3.0 * p0;
			double d = p0;

			double t = SolveCubic(a, b, c, d);

			return m_E*(t*t*t) + m_F*(t*t) + m_G*t + m_H;
		}
	}
}