using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public abstract class ActorComponent
	{
		protected string m_Name = "Unnamed Node";
		protected ActorNode m_Parent;
		protected Actor m_Actor;
		private ushort m_ParentIdx = 0;
		private ushort m_Idx = 0;
		private List<CustomProperty> m_CustomProperties;

		public ActorComponent(Actor actor)
		{
			m_Actor = actor;
		}

		public ActorComponent()
		{
		}

		public Actor Actor
		{
			get
			{
				return m_Actor;
			}
		}
		
		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public ushort ParentIdx
		{
			get
			{
				return m_ParentIdx;
			}
		}

		public ActorNode Parent
		{
			get
			{
				return m_Parent;
			}
		}

		public ushort Idx
		{
			get
			{
				return m_Idx;
			}
			set
			{
				m_Idx = value;
			}
		}

		public void AddCustomProperty(CustomProperty property)
		{
			if(m_CustomProperties == null)
			{
				m_CustomProperties = new List<CustomProperty>();
			}
			m_CustomProperties.Add(property);
		}

		public CustomProperty GetCustomProperty(string name)
		{
			if(m_CustomProperties == null)
			{
				return null;
			}
			foreach(CustomProperty prop in m_CustomProperties)
			{
				if(prop.Name == name)
				{
					return prop;
				}
			}
			return null;
		}

		public static ActorComponent Read(Actor actor, BinaryReader reader, ActorComponent component = null)
		{
			component.m_Actor = actor;
			component.m_Name = Actor.ReadString(reader);
			component.m_ParentIdx = reader.ReadUInt16();

			return component;
		}

		public virtual void ResolveComponentIndices(ActorComponent[] components)
		{
			ActorNode node = components[m_ParentIdx] as ActorNode;
			if(node != null && this is ActorNode)
			{
				node.AddChild(this as ActorNode);
			}
		}

		public abstract ActorComponent MakeInstance(Actor resetActor);

		public void Copy(ActorComponent component, Actor resetActor)
		{
			m_Name = component.m_Name;
			m_Actor = resetActor;
			m_ParentIdx = component.m_ParentIdx;
			m_Idx = component.m_Idx;
		}
	}
}