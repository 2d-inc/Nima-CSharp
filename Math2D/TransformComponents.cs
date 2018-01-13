using System;

namespace Nima.Math2D
{
	public class TransformComponents
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

		public TransformComponents()
		{
			m_Buffer = new float[6]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
		}

		public TransformComponents(TransformComponents copy) : this()
		{
			m_Buffer[0] = copy.m_Buffer[0];
			m_Buffer[1] = copy.m_Buffer[1];
			m_Buffer[2] = copy.m_Buffer[2];
			m_Buffer[3] = copy.m_Buffer[3];
			m_Buffer[4] = copy.m_Buffer[4];
			m_Buffer[5] = copy.m_Buffer[5];
		}

        public float X
        {
            get
            {
                return m_Buffer[0];
            }
            set
            {
                m_Buffer[0] = value;
            }
        }

        public float Y
        {
            get
            {
                return m_Buffer[1];
            }
            set
            {
                m_Buffer[1] = value;
            }
        }

        public float ScaleX
        {
            get
            {
                return m_Buffer[2];
            }
            set
            {
                m_Buffer[2] = value;
            }
        }

        public float ScaleY
        {
            get
            {
                return m_Buffer[3];
            }
            set
            {
                m_Buffer[3] = value;
            }
        }

        public float Rotation
        {
            get
            {
                return m_Buffer[4];
            }
            set
            {
                m_Buffer[4] = value;
            }
        }

        public float Skew
        {
            get
            {
                return m_Buffer[5];
            }
            set
            {
                m_Buffer[5] = value;
            }
        }

        public Vec2D Translation
        {
            get
            {
                return new Vec2D(m_Buffer[0], m_Buffer[1]);
            }
        }

        public Vec2D Scale
        {
            get
            {
                return new Vec2D(m_Buffer[2], m_Buffer[3]);
            }
        }
	}
}