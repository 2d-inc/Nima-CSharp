using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorNode : ActorComponent
	{
		protected List<ActorNode> m_Children;
		protected List<ActorNode> m_Dependents;
		protected Mat2D m_Transform = new Mat2D();
		protected Mat2D m_WorldTransform = new Mat2D();

		protected Vec2D m_Translation = new Vec2D();
		protected float m_Rotation = 0.0f;
		protected Vec2D m_Scale = new Vec2D(1.0f, 1.0f);
		protected float m_Opacity = 1.0f;
		protected float m_RenderOpacity = 1.0f;

		private bool m_IsDirty = true;
		private bool m_IsWorldDirty = true;
		private bool m_SuppressMarkDirty = false;

		private bool m_OverrideWorldTransform = false;
		private bool m_OverrideRotation = false;
		private float m_OverrideRotationValue = 0.0f;

		public ActorNode()
		{

		}

		public ActorNode(Actor actor) : base(actor)
		{
		}
		
		public bool SuppressMarkDirty
		{
			get
			{
				return m_SuppressMarkDirty;
			}
			set
			{
				m_SuppressMarkDirty = value;
			}
		}

		public bool IsWorldDirty
		{
			get
			{
				return m_IsWorldDirty;
			}
		}
		
		public bool IsDirty
		{
			get
			{
				return m_IsDirty;
			}
		}

		public Mat2D Transform
		{
			get
			{
				if(m_IsDirty)
				{
					UpdateTransform();
				}
				return m_Transform;
			}
		}

		public Mat2D WorldTransformOverride
		{
			get
			{
				return m_OverrideWorldTransform ? m_WorldTransform : null;
			}
			set
			{
				if(value == null)
				{
					m_OverrideWorldTransform = false;
				}
				else
				{
					m_OverrideWorldTransform = true;
					Mat2D.Copy(m_WorldTransform, value);
				}
				MarkWorldDirty();
			}
		}

		public Mat2D WorldTransform
		{
			get
			{
				if(m_IsWorldDirty)
				{
					UpdateWorldTransform();
				}
				return m_WorldTransform;
			}
		}

		public float X
		{
			get
			{
				return m_Translation[0];
			}
			set
			{
				if(m_Translation[0] == value)
				{
					return;
				}
				m_Translation[0] = value;
				MarkDirty();
				MarkWorldDirty();
			}
		}
		
		public float Y
		{
			get
			{
				return m_Translation[1];
			}
			set
			{
				if(m_Translation[1] == value)
				{
					return;
				}
				m_Translation[1] = value;
				MarkDirty();
				MarkWorldDirty();
			}
		}
		
		public float Rotation
		{
			get
			{
				return m_Rotation;
			}
			set
			{
				if(m_Rotation == value)
				{
					return;
				}
				m_Rotation = value;
				MarkDirty();
				MarkWorldDirty();
			}
		}
		
		public float ScaleX
		{
			get
			{
				return m_Scale[0];
			}
			set
			{
				if(m_Scale[0] == value)
				{
					return;
				}
				m_Scale[0] = value;
				MarkDirty();
				MarkWorldDirty();
			}
		}
		
		public float ScaleY
		{
			get
			{
				return m_Scale[1];
			}
			set
			{
				if(m_Scale[1] == value)
				{
					return;
				}
				m_Scale[1] = value;
				MarkDirty();
				MarkWorldDirty();
			}
		}
		
		public float Opacity
		{
			get
			{
				return m_Opacity;
			}
			set
			{
				if(m_Opacity == value)
				{
					return;
				}
				m_Opacity = value;
				MarkWorldDirty();
			}
		}

		public float RenderOpacity
		{
			get
			{
				return m_RenderOpacity;
			}
		}

		public void MarkDirty()
		{
			if(m_IsDirty)
			{
				return;
			}
			m_IsDirty = true;
		}

		public void MarkWorldDirty()
		{
			if(m_IsWorldDirty || m_SuppressMarkDirty)
			{
				return;
			}
			m_IsWorldDirty = true;
			if(m_Children != null)
			{
				foreach(ActorNode node in m_Children)
				{
					node.MarkWorldDirty();
				}
			}
			if(m_Dependents != null)
			{
				foreach(ActorNode node in m_Dependents)
				{
					node.MarkWorldDirty();
				}
			}
		}

		public void AddDependent(ActorNode node)
		{
			if(m_Dependents == null)
			{
				m_Dependents = new List<ActorNode>();
			}
			m_Dependents.Add(node);
		}

		private void UpdateTransform()
		{
			m_IsDirty = false;

			Mat2D.FromRotation(m_Transform, m_OverrideRotation ? m_OverrideRotationValue : m_Rotation);
			m_Transform[4] = m_Translation[0];
			m_Transform[5] = m_Translation[1];
			Mat2D.Scale(m_Transform, m_Transform, m_Scale);
		}

		public Vec2D GetWorldTranslation(Vec2D vec)
		{
			if(m_IsWorldDirty)
			{
				UpdateWorldTransform();
			}
			vec[0] = m_WorldTransform[4];
			vec[1] = m_WorldTransform[5];
			return vec;
		}

		public void SetRotationOverride(float v)
		{
			if(!m_OverrideRotation || m_OverrideRotationValue != v)
			{
				m_OverrideRotation = true;
				m_OverrideRotationValue = v;
				MarkDirty();
				MarkWorldDirty();
			}
		}

		public void ClearRotationOverride()
		{
			if(m_OverrideRotation)
			{
				m_OverrideRotation = false;
				MarkDirty();
				MarkWorldDirty();
			}
		}

		public float OverrideRotationValue
		{
			get
			{
				return m_OverrideRotationValue;
			}
		}

		public void UpdateTransforms()
		{
			if(m_IsDirty)
			{
				UpdateTransform();
			}
			if(m_IsWorldDirty)
			{
				UpdateWorldTransform();
			}
		}

		private void UpdateWorldTransform()
		{
			m_IsWorldDirty = false;

			if(m_IsDirty)
			{
				UpdateTransform();
			}
		
			m_RenderOpacity = m_Opacity;
		
			if(m_Parent != null)
			{
				m_Parent.UpdateTransforms();
				m_RenderOpacity *= m_Parent.RenderOpacity;
				if(!m_OverrideWorldTransform)
				{
					Mat2D.Multiply(m_WorldTransform, m_Parent.m_WorldTransform, m_Transform);
				}
			}
			else if(!m_OverrideWorldTransform)
			{
				Mat2D.Copy(m_WorldTransform, m_Transform);
			}
		}

		public static ActorNode Read(Actor actor, BinaryReader reader, ActorNode node = null)
		{
			if(node == null)
			{
				node = new ActorNode();
			}
			ActorComponent.Read(actor, reader, node);
			Actor.ReadFloat32Array(reader, node.m_Translation.Values);
			node.m_Rotation = reader.ReadSingle();
			Actor.ReadFloat32Array(reader, node.m_Scale.Values);
			node.m_Opacity = reader.ReadSingle();

			return node;
		}

		public void AddChild(ActorNode node)
		{
			if(node.m_Parent != null)
			{
				node.m_Parent.m_Children.Remove(node);
			}
			node.m_Parent = this;
			if(m_Children == null)
			{
				m_Children = new List<ActorNode>();
			}
			m_Children.Add(node);
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorNode instanceNode = new ActorNode();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public void Copy(ActorNode node, Actor resetActor)
		{
			base.Copy(node, resetActor);
			m_IsDirty = true;
			m_IsWorldDirty = true;
			m_Transform = new Mat2D(node.m_Transform);
			m_WorldTransform = new Mat2D(node.m_WorldTransform);
			m_Translation = new Vec2D(node.m_Translation);
			m_Scale = new Vec2D(node.m_Scale);
			m_Rotation = node.m_Rotation;
			m_Opacity = node.m_Opacity;
			m_RenderOpacity = node.m_RenderOpacity;
			m_OverrideWorldTransform = node.m_OverrideWorldTransform;
			m_OverrideRotation = node.m_OverrideRotation;
			m_OverrideRotationValue = node.m_OverrideRotationValue;
		}
	}
}