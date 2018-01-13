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
		private List<CustomIntProperty> m_CustomIntProperties;
		private List<CustomFloatProperty> m_CustomFloatProperties;
		private List<CustomStringProperty> m_CustomStringProperties;
		private List<CustomBooleanProperty> m_CustomBooleanProperties;
		public IList<ActorComponent>  m_Dependents;
		public uint m_GraphOrder;
		public byte m_DirtMask;

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

		public void AddCustomIntProperty(CustomIntProperty property)
		{
			if(m_CustomIntProperties == null)
			{
				m_CustomIntProperties = new List<CustomIntProperty>();
			}
			m_CustomIntProperties.Add(property);
		}

		public void AddCustomFloatProperty(CustomFloatProperty property)
		{
			if(m_CustomFloatProperties == null)
			{
				m_CustomFloatProperties = new List<CustomFloatProperty>();
			}
			m_CustomFloatProperties.Add(property);
		}

		public void AddCustomStringProperty(CustomStringProperty property)
		{
			if(m_CustomStringProperties == null)
			{
				m_CustomStringProperties = new List<CustomStringProperty>();
			}
			m_CustomStringProperties.Add(property);
		}

		public void AddCustomBooleanProperty(CustomBooleanProperty property)
		{
			if(m_CustomBooleanProperties == null)
			{
				m_CustomBooleanProperties = new List<CustomBooleanProperty>();
			}
			m_CustomBooleanProperties.Add(property);
		}

		public CustomIntProperty GetCustomIntProperty(string name)
		{
			if(m_CustomIntProperties == null)
			{
				return null;
			}
			foreach(CustomIntProperty prop in m_CustomIntProperties)
			{
				if(prop.Name == name)
				{
					return prop;
				}
			}
			return null;
		}

		public CustomFloatProperty GetCustomFloatProperty(string name)
		{
			if(m_CustomFloatProperties == null)
			{
				return null;
			}
			foreach(CustomFloatProperty prop in m_CustomFloatProperties)
			{
				if(prop.Name == name)
				{
					return prop;
				}
			}
			return null;
		}

		public CustomStringProperty GetCustomStringProperty(string name)
		{
			if(m_CustomStringProperties == null)
			{
				return null;
			}
			foreach(CustomStringProperty prop in m_CustomStringProperties)
			{
				if(prop.Name == name)
				{
					return prop;
				}
			}
			return null;
		}

		public CustomBooleanProperty GetCustomBooleanProperty(string name)
		{
			if(m_CustomBooleanProperties == null)
			{
				return null;
			}
			foreach(CustomBooleanProperty prop in m_CustomBooleanProperties)
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
			if(node != null)
			{
				if(this is ActorNode)
				{
					node.AddChild(this as ActorNode);
				}
				m_Actor.AddDependency(this, node);
			}
		}

		public virtual void CompleteResolve()
		{

		}

		public abstract ActorComponent MakeInstance(Actor resetActor);

		public void Copy(ActorComponent component, Actor resetActor)
		{
			m_Name = component.m_Name;
			m_Actor = resetActor;
			m_ParentIdx = component.m_ParentIdx;
			m_Idx = component.m_Idx;
		}

		public virtual void OnDirty(byte dirt){}

		public virtual void Update(byte dirt){}
	}
}