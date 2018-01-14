using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public abstract class ActorAxisConstraint : ActorTargetedConstraint
	{
        protected bool m_CopyX;
        protected bool m_CopyY;
        protected float m_ScaleX;
        protected float m_ScaleY;
        protected bool m_EnableMinX;
        protected bool m_EnableMinY;
        protected bool m_EnableMaxX;
        protected bool m_EnableMaxY;
        protected float m_MaxX;
        protected float m_MaxY;
        protected float m_MinX;
        protected float m_MinY;
        protected bool m_Offset;
		protected TransformSpace m_SourceSpace;
        protected TransformSpace m_DestSpace;
        protected TransformSpace m_MinMaxSpace;
        
        public ActorAxisConstraint()
        {
            m_CopyX = false;
            m_CopyY = false;
            m_ScaleX = 1.0f;
            m_ScaleY = 1.0f;
            m_EnableMinX = false;
            m_EnableMinY = false;
            m_EnableMaxX = false;
            m_EnableMaxY = false;
            m_MinX = 0.0f;
            m_MinY = 0.0f;
            m_MaxX = 0.0f;
            m_MaxY = 0.0f;
            m_Offset = false;
            m_SourceSpace = TransformSpace.World;
            m_DestSpace = TransformSpace.World;
            m_MinMaxSpace = TransformSpace.World;
        }

        public override void OnDirty(byte dirt)
        {
            MarkDirty();
        }

        public static ActorAxisConstraint Read(Actor actor, BinaryReader reader, ActorAxisConstraint component)
		{
			ActorTargetedConstraint.Read(actor, reader, component);

			if((component.m_CopyX = reader.ReadByte() == 1))
            {
                component.m_ScaleX = reader.ReadSingle();
            }
            if((component.m_EnableMinX = reader.ReadByte() == 1))
            {
                component.m_MinX = reader.ReadSingle();
            }
            if((component.m_EnableMaxX = reader.ReadByte() == 1))
            {
                component.m_MaxX = reader.ReadSingle();
            }

            // Y Axis
            if((component.m_CopyY = reader.ReadByte() == 1))
            {
                component.m_ScaleY = reader.ReadSingle();
            }
            if((component.m_EnableMinY = reader.ReadByte() == 1))
            {
                component.m_MinY = reader.ReadSingle();
            }
            if((component.m_EnableMaxY = reader.ReadByte() == 1))
            {
                component.m_MaxY = reader.ReadSingle();
            }

            component.m_Offset = reader.ReadByte() == 1;
            component.m_SourceSpace = (TransformSpace)reader.ReadByte();
            component.m_DestSpace = (TransformSpace)reader.ReadByte();
            component.m_MinMaxSpace = (TransformSpace)reader.ReadByte();

			return component;
		}

        public void Copy(ActorAxisConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_CopyX = node.m_CopyX;
            m_CopyY = node.m_CopyY;
            m_ScaleX = node.m_ScaleX;
            m_ScaleY = node.m_ScaleY;
            m_EnableMinX = node.m_EnableMinX;
            m_EnableMinY = node.m_EnableMinY;
            m_EnableMaxX = node.m_EnableMaxX;
            m_EnableMaxY = node.m_EnableMaxY;
            m_MinX = node.m_MinX;
            m_MinY = node.m_MinY;
            m_MaxX = node.m_MaxX;
            m_MaxY = node.m_MaxY;
            m_Offset = node.m_Offset;
            m_SourceSpace = node.m_SourceSpace;
            m_DestSpace = node.m_DestSpace;
            m_MinMaxSpace = node.m_MinMaxSpace;
		}
    }
}