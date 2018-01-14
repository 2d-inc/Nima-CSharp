using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorNode : ActorComponent
	{
		protected List<ActorNode> m_Children;
		//protected List<ActorNode> m_Dependents;
		protected Mat2D m_Transform = new Mat2D();
		protected Mat2D m_WorldTransform = new Mat2D();

		protected Vec2D m_Translation = new Vec2D();
		protected float m_Rotation = 0.0f;
		protected Vec2D m_Scale = new Vec2D(1.0f, 1.0f);
		protected float m_Opacity = 1.0f;
		protected float m_RenderOpacity = 1.0f;

		private bool m_OverrideWorldTransform = false;
		protected bool m_IsCollapsedVisibility = false;

		private bool m_RenderCollapsed = false;

		private IList<ActorConstraint> m_Constraints;

		const byte TransformDirty = 1<<0;
		const byte WorldTransformDirty = 1<<1;

		public ActorNode()
		{

		}

		public ActorNode(Actor actor) : base(actor)
		{
		}
		
		public Mat2D Transform
		{
			get
			{
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
				MarkTransformDirty();
			}
		}

		public Mat2D WorldTransform
		{
			get
			{
				return m_WorldTransform;
			}
			// N.B. this should only be done if you really know what you're doing. Generally you want to manipulate the local translation, rotation, and scale of a Node.
			set
			{
				Mat2D.Copy(m_WorldTransform, value);
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
				MarkTransformDirty();
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
				MarkTransformDirty();
			}
		}

		public Vec2D Translation
		{
			get 
			{
				return new Vec2D(m_Translation);
			}
			set
			{
				Vec2D.Copy(m_Translation, value);
				MarkTransformDirty();
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
				MarkTransformDirty();
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
				MarkTransformDirty();
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
				MarkTransformDirty();
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
				MarkTransformDirty();
			}
		}

		public float RenderOpacity
		{
			get
			{
				return m_RenderOpacity;
			}
		}

		public bool RenderCollapsed
		{
			get
			{
				return m_RenderCollapsed;
			}
		}

		public bool CollapsedVisibility
		{
			get
			{
				return m_IsCollapsedVisibility;
			}
			set
			{
				if(m_IsCollapsedVisibility != value)
				{
					m_IsCollapsedVisibility = value;
					MarkTransformDirty();
				}
			}
		}

		public void MarkTransformDirty()
		{
			if(m_Actor == null)
			{
				// Still loading?
				return;
			}
			if(!m_Actor.AddDirt(this, TransformDirty))
			{
				return;
			}
			m_Actor.AddDirt(this, WorldTransformDirty, true);
		}

		private void UpdateTransform()
		{
			Mat2D.FromRotation(m_Transform, m_Rotation);
			m_Transform[4] = m_Translation[0];
			m_Transform[5] = m_Translation[1];
			Mat2D.Scale(m_Transform, m_Transform, m_Scale);
		}

		public Vec2D GetWorldTranslation(Vec2D vec)
		{
			vec[0] = m_WorldTransform[4];
			vec[1] = m_WorldTransform[5];
			return vec;
		}

		private void UpdateWorldTransform()
		{
			m_RenderOpacity = m_Opacity;
		
			if(m_Parent != null)
			{
				m_RenderCollapsed = m_IsCollapsedVisibility || m_Parent.m_RenderCollapsed;
				m_RenderOpacity *= m_Parent.m_RenderOpacity;
				if(!m_OverrideWorldTransform)
				{
					Mat2D.Multiply(m_WorldTransform, m_Parent.m_WorldTransform, m_Transform);
				}
			}
			else
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

			if(actor.Version >= 13)
			{
				node.m_IsCollapsedVisibility = reader.ReadByte() == 1;
			}

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

		public IList<ActorNode> Children
		{
			get
			{
				return m_Children;
			}
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
			m_Transform = new Mat2D(node.m_Transform);
			m_WorldTransform = new Mat2D(node.m_WorldTransform);
			m_Translation = new Vec2D(node.m_Translation);
			m_Scale = new Vec2D(node.m_Scale);
			m_Rotation = node.m_Rotation;
			m_Opacity = node.m_Opacity;
			m_RenderOpacity = node.m_RenderOpacity;
			m_OverrideWorldTransform = node.m_OverrideWorldTransform;
		}

		public override void OnDirty(byte dirt)
		{

		}

		public bool AddConstraint(ActorConstraint constraint)
		{
			if(m_Constraints == null)
			{
				m_Constraints = new List<ActorConstraint>();
			}
			if(m_Constraints.Contains(constraint))
			{
				return false;
			}
			m_Constraints.Add(constraint);
			return true;
		}

		public override void Update(byte dirt)
		{
			if((dirt & TransformDirty) == TransformDirty)
			{
				UpdateTransform();
			}
			if((dirt & WorldTransformDirty) == WorldTransformDirty)
			{
				UpdateWorldTransform();
				if(m_Constraints != null)
				{
					foreach(ActorConstraint constraint in m_Constraints)
					{
						if(constraint.IsEnabled)
						{
							constraint.Constrain(this);
						}
					}
				}
			}
		}
	}
}