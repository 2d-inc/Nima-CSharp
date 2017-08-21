using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public abstract class ActorCollider : ActorNode
	{
		protected bool m_IsCollisionEnabled = true;

		public bool IsCollisionEnabled
		{
			get
			{
				return m_IsCollisionEnabled;
			}
			set
			{
				m_IsCollisionEnabled = value;
			}
		}

		public static ActorCollider Read(Actor actor, BinaryReader reader, ActorCollider property)
		{
			ActorNode.Read(actor, reader, property);
			property.IsCollisionEnabled = reader.ReadByte() == 1;
			return property;
		}

		public void Copy(ActorCollider prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_IsCollisionEnabled = prop.m_IsCollisionEnabled;
		}
	}

	public class ActorColliderRectangle : ActorCollider
	{
		private float m_Width;
		private float m_Height;

		public float Width
		{
			get
			{
				return m_Width;
			}
			set
			{
				m_Width = value;
			}
		}

		public float Height
		{
			get
			{
				return m_Height;
			}
			set
			{
				m_Height = value;
			}
		}

		public static ActorColliderRectangle Read(Actor actor, BinaryReader reader, ActorColliderRectangle property = null)
		{
			if(property == null)
			{
				property = new ActorColliderRectangle();
			}
			ActorCollider.Read(actor, reader, property);
			property.m_Width = reader.ReadSingle();
			property.m_Height = reader.ReadSingle();
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorColliderRectangle instanceCollider = new ActorColliderRectangle();
			instanceCollider.Copy(this, resetActor);
			return instanceCollider;
		}

		public void Copy(ActorColliderRectangle prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Width = prop.m_Width;
			m_Height = prop.m_Height;
		}
	}

	public class ActorColliderTriangle : ActorCollider
	{
		private float m_Width;
		private float m_Height;

		public float Width
		{
			get
			{
				return m_Width;
			}
			set
			{
				m_Width = value;
			}
		}

		public float Height
		{
			get
			{
				return m_Height;
			}
			set
			{
				m_Height = value;
			}
		}

		public static ActorColliderTriangle Read(Actor actor, BinaryReader reader, ActorColliderTriangle property = null)
		{
			if(property == null)
			{
				property = new ActorColliderTriangle();
			}
			ActorCollider.Read(actor, reader, property);
			property.m_Width = reader.ReadSingle();
			property.m_Height = reader.ReadSingle();
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorColliderTriangle instanceCollider = new ActorColliderTriangle();
			instanceCollider.Copy(this, resetActor);
			return instanceCollider;
		}

		public void Copy(ActorColliderTriangle prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Width = prop.m_Width;
			m_Height = prop.m_Height;
		}
	}

	public class ActorColliderCircle : ActorCollider
	{
		private float m_Radius;

		public float Radius
		{
			get
			{
				return m_Radius;
			}
			set
			{
				m_Radius = value;
			}
		}

		public static ActorColliderCircle Read(Actor actor, BinaryReader reader, ActorColliderCircle property = null)
		{
			if(property == null)
			{
				property = new ActorColliderCircle();
			}
			ActorCollider.Read(actor, reader, property);
			property.m_Radius = reader.ReadSingle();
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorColliderCircle instanceCollider = new ActorColliderCircle();
			instanceCollider.Copy(this, resetActor);
			return instanceCollider;
		}

		public void Copy(ActorColliderCircle prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Radius = prop.m_Radius;
		}
	}

	public class ActorColliderPolygon : ActorCollider
	{
		private float[] m_ContourVertices;

		public float[] ContourVertices
		{
			get
			{
				return m_ContourVertices;
			}
		}

		public static ActorColliderPolygon Read(Actor actor, BinaryReader reader, ActorColliderPolygon property = null)
		{
			if(property == null)
			{
				property = new ActorColliderPolygon();
			}
			ActorCollider.Read(actor, reader, property);

			int numVertices = (int)reader.ReadUInt32();
			property.m_ContourVertices = new float[numVertices*2];
			Actor.ReadFloat32Array(reader, property.m_ContourVertices);
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorColliderPolygon instanceCollider = new ActorColliderPolygon();
			instanceCollider.Copy(this, resetActor);
			return instanceCollider;
		}

		public void Copy(ActorColliderPolygon prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_ContourVertices = prop.m_ContourVertices;
		}
	}

	public class ActorColliderLine : ActorCollider
	{
		private float[] m_Vertices;

		public float[] LineVertices
		{
			get
			{
				return m_Vertices;
			}
		}

		public static ActorColliderLine Read(Actor actor, BinaryReader reader, ActorColliderLine property = null)
		{
			if(property == null)
			{
				property = new ActorColliderLine();
			}
			ActorCollider.Read(actor, reader, property);

			int numVertices = (int)reader.ReadUInt32();
			property.m_Vertices = new float[numVertices*2];
			Actor.ReadFloat32Array(reader, property.m_Vertices);
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorColliderLine instanceCollider = new ActorColliderLine();
			instanceCollider.Copy(this, resetActor);
			return instanceCollider;
		}

		public void Copy(ActorColliderLine prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Vertices = prop.m_Vertices;
		}
	}
}