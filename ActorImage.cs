using Nima.Math2D;
using System;
using System.IO;
using System.Collections.Generic;

namespace Nima
{
	public enum BlendModes
	{
		Normal = 0,
		Multiply = 1,
		Screen = 2,
		Additive = 3
	};

	public class ActorImage : ActorNode
	{
		// Editor set draw index.
		private int m_DrawOrder;
		// Computed draw index in the image list.
		private int m_DrawIndex;
		BlendModes m_BlendMode;
		private int m_TextureIndex;
		private float[] m_Vertices;
		private ushort[] m_Triangles;
		private int m_VertexCount;
		private int m_TriangleCount;
		private float[] m_AnimationDeformedVertices;
		private bool m_IsVertexDeformDirty;

		public class BoneConnection
		{
			public ushort m_BoneIdx;
			public ActorNode m_Node;
			public Mat2D m_Bind = new Mat2D();
			public Mat2D m_InverseBind = new Mat2D();

			public ActorNode Node
			{
				get
				{
					return m_Node;
				}
			}

			public Mat2D Bind
			{
				get
				{
					return m_Bind;
				}
			}

			public Mat2D InverseBind
			{
				get
				{
					return m_InverseBind;
				}
			}
		}

		private BoneConnection[] m_BoneConnections;
		public float[] m_BoneMatrices;

		public int ConnectedBoneCount
		{
			get
			{
				return m_BoneConnections == null ? 0 : m_BoneConnections.Length;
			}
		}

		public IEnumerable<BoneConnection> BoneConnections
		{
			get
			{
				return m_BoneConnections;
			}
		}

		public int TextureIndex
		{
			get
			{
				return m_TextureIndex;
			}
		}

		public BlendModes BlendMode
		{
			get
			{
				return m_BlendMode;
			}
		}

		public int DrawOrder
		{
			get
			{
				return m_DrawOrder;
			}
			set
			{
				if(m_DrawOrder == value)
				{
					return;
				}
				m_DrawOrder = value;
			}
		}

		public int DrawIndex
		{
			get
			{
				return m_DrawIndex;
			}
			set
			{
				if(m_DrawIndex == value)
				{
					return;
				}
				m_DrawIndex = value;
			}
		}

		public int VertexCount
		{
			get
			{
				return m_VertexCount;
			}
		}

		public int TriangleCount
		{
			get
			{
				return m_TriangleCount;
			}
		}

		public ushort[] Triangles
		{
			get
			{
				return m_Triangles;
			}
		}
		
		public float[] Vertices
		{
			get
			{
				return m_Vertices;
			}
		}

		public int VertexPositionOffset
		{
			get
			{
				return 0;
			}
		}

		public int VertexUVOffset
		{
			get
			{
				return 2;
			}
		}

		public int VertexBoneIndexOffset
		{
			get
			{
				return 4;
			}
		}

		public int VertexBoneWeightOffset
		{
			get
			{
				return 8;
			}
		}

		public int VertexStride
		{
			get
			{
				return m_BoneConnections != null ? 12 : 4;
			}
		}

		public bool IsSkinned
		{
			get
			{
				return m_BoneConnections != null;
			}
		}

		public bool DoesAnimationVertexDeform
		{
			get
			{
				return m_AnimationDeformedVertices != null;
			}
			set
			{
				if(value)
				{
					if(m_AnimationDeformedVertices == null || m_AnimationDeformedVertices.Length != m_VertexCount * 2)
					{
						m_AnimationDeformedVertices = new float[m_VertexCount * 2];
						// Copy the deform verts from the rig verts.
						int writeIdx = 0;
						int readIdx = 0;
						int readStride = VertexStride;
						for(int i = 0; i < m_VertexCount; i++)
						{
							m_AnimationDeformedVertices[writeIdx++] = m_Vertices[readIdx];
							m_AnimationDeformedVertices[writeIdx++] = m_Vertices[readIdx+1];
							readIdx += readStride;
						}
					}
				}
				else
				{
					m_AnimationDeformedVertices = null;
				}
			}
		}

		public float[] AnimationDeformedVertices
		{
			get
			{
				return m_AnimationDeformedVertices;
			}
		}

		public bool IsVertexDeformDirty
		{
			get
			{
				return m_IsVertexDeformDirty;
			}
			set
			{
				m_IsVertexDeformDirty = value;
			}
		}

		public ActorImage()
		{
			m_TextureIndex = -1;
		}

		public void DisposeGeometry()
		{
			// Delete vertices only if we do not vertex deform at runtime.
			if(m_AnimationDeformedVertices == null)
			{
				m_Vertices = null;
			}
			m_Triangles = null;
		}

		// We don't do this at initialization as some engines (like Unity)
		// don't need us to handle the bone matrix transforms ourselves.
		// This helps keep memory a little lower when this code runs in those engines.
		private void InstanceBoneMatrices()
		{
			if(m_BoneMatrices == null)
			{
				int numConnectedBones = m_BoneConnections.Length;
				m_BoneMatrices = new float[(numConnectedBones+1)*6];
				// First bone transform is always identity.
				m_BoneMatrices[0] = 1.0f;
				m_BoneMatrices[1] = 0.0f;
				m_BoneMatrices[2] = 0.0f;
				m_BoneMatrices[3] = 1.0f;
				m_BoneMatrices[4] = 0.0f;
				m_BoneMatrices[5] = 0.0f;
			}
		}

		public float[] BoneInfluenceMatrices
		{
			get
			{
				InstanceBoneMatrices();

				Mat2D mat = new Mat2D();
				int bidx = 6;
				foreach(BoneConnection bc in m_BoneConnections)
				{
					bc.m_Node.UpdateTransforms();
					Mat2D.Multiply(mat, bc.m_Node.WorldTransform, bc.m_InverseBind);

					m_BoneMatrices[bidx++] = mat[0];
					m_BoneMatrices[bidx++] = mat[1];
					m_BoneMatrices[bidx++] = mat[2];
					m_BoneMatrices[bidx++] = mat[3];
					m_BoneMatrices[bidx++] = mat[4];
					m_BoneMatrices[bidx++] = mat[5];
				}

				return m_BoneMatrices;
			}
		}

		public float[] BoneTransformMatrices
		{
			get
			{
				InstanceBoneMatrices();

				int bidx = 6;
				foreach(BoneConnection bc in m_BoneConnections)
				{
					bc.m_Node.UpdateTransforms();
					Mat2D mat = bc.m_Node.WorldTransform;

					m_BoneMatrices[bidx++] = mat[0];
					m_BoneMatrices[bidx++] = mat[1];
					m_BoneMatrices[bidx++] = mat[2];
					m_BoneMatrices[bidx++] = mat[3];
					m_BoneMatrices[bidx++] = mat[4];
					m_BoneMatrices[bidx++] = mat[5];
				}

				return m_BoneMatrices;
			}
		}

		public static ActorImage Read(Actor actor, BinaryReader reader, ActorImage node = null)
		{
			if(node == null)
			{
				node = new ActorImage();
			}
			
			ActorNode.Read(actor, reader, node);

			bool isVisible = reader.ReadByte() != 0;
			if(isVisible)
			{
				node.m_BlendMode = (BlendModes)reader.ReadByte();
				node.m_DrawOrder = (int)reader.ReadUInt16();
				node.m_TextureIndex = (int)reader.ReadByte();

				int numConnectedBones = (int)reader.ReadByte();
				if(numConnectedBones != 0)
				{
					node.m_BoneConnections = new BoneConnection[numConnectedBones];

					for(int i = 0; i < numConnectedBones; i++)
					{
						BoneConnection bc = new BoneConnection();
						bc.m_BoneIdx = reader.ReadUInt16();
						Actor.ReadFloat32Array(reader, bc.m_Bind.Values);
						Mat2D.Invert(bc.m_InverseBind, bc.m_Bind);

						node.m_BoneConnections[i] = bc;
					}

					Mat2D worldOverride = new Mat2D();
					Actor.ReadFloat32Array(reader, worldOverride.Values);
					node.WorldTransformOverride = worldOverride;
				}

				uint numVertices = reader.ReadUInt32();
				int vertexStride = numConnectedBones > 0 ? 12 : 4;
				node.m_VertexCount = (int)numVertices;
				node.m_Vertices = new float[numVertices * vertexStride];
				Actor.ReadFloat32Array(reader, node.m_Vertices);
				
				uint numTris = reader.ReadUInt32();
				node.m_Triangles = new ushort[numTris * 3];
				node.m_TriangleCount = (int)numTris;
				Actor.ReadUInt16Array(reader, node.m_Triangles);
			}

			return node;
		}

		public override void ResolveComponentIndices(ActorComponent[] components)
		{
			base.ResolveComponentIndices(components);
			if(m_BoneConnections != null)
			{
				for(int i = 0; i < m_BoneConnections.Length; i++)
				{
					BoneConnection bc = m_BoneConnections[i];
					bc.m_Node = components[bc.m_BoneIdx] as ActorNode;
					ActorBone bone = bc.m_Node as ActorBone;
					bone.IsConnectedToImage = true;
				}	
			}
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorImage instanceNode = new ActorImage();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public void Copy(ActorImage node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_DrawOrder = node.m_DrawOrder;
			m_BlendMode = node.m_BlendMode;
			m_TextureIndex = node.m_TextureIndex;
			m_VertexCount = node.m_VertexCount;
			m_TriangleCount = node.m_TriangleCount;
			m_Vertices = node.m_Vertices;
			m_Triangles = node.m_Triangles;
			if(node.m_AnimationDeformedVertices != null)
			{
				m_AnimationDeformedVertices = (float[])node.m_AnimationDeformedVertices.Clone();
			}

			if(node.m_BoneConnections != null)
			{
				m_BoneConnections = new BoneConnection[node.m_BoneConnections.Length];
				for(int i = 0; i < node.m_BoneConnections.Length; i++)
				{
					BoneConnection bc = new BoneConnection();
					bc.m_BoneIdx = node.m_BoneConnections[i].m_BoneIdx;
					Mat2D.Copy(bc.m_Bind, node.m_BoneConnections[i].m_Bind);
					Mat2D.Copy(bc.m_InverseBind, node.m_BoneConnections[i].m_InverseBind);
					m_BoneConnections[i] = bc;
				} 
			}
		}

		public void TransformBind(Mat2D xform)
		{
			if(m_BoneConnections != null)
			{
				foreach(BoneConnection bc in m_BoneConnections)
				{
					Mat2D.Multiply(bc.m_Bind, xform, bc.m_Bind);
					Mat2D.Invert(bc.m_InverseBind, bc.m_Bind);
				}
			}
		}

		public float[] MakeVertexPositionBuffer()
		{
			return new float[m_VertexCount * 2];
		}

		public void TransformDeformVertices(Nima.Math2D.Mat2D wt)
		{
			if(m_AnimationDeformedVertices == null)
			{
				return;
			}
			
			float[] fv = m_AnimationDeformedVertices;

			int vidx = 0;
			for(int j = 0; j < m_VertexCount; j++)
			{
				float x = fv[vidx];
				float y = fv[vidx+1];

				fv[vidx] = wt[0] * x + wt[2] * y + wt[4];
				fv[vidx+1] = wt[1] * x + wt[3] * y + wt[5];

				vidx += 2;
			}
		}

		public void UpdateVertexPositionBuffer(float[] buffer, bool isSkinnedDeformInWorld = true)
		{
			Mat2D worldTransform = this.WorldTransform;
			int readIdx = 0;
			int writeIdx = 0;

			float[] v = m_AnimationDeformedVertices != null ? m_AnimationDeformedVertices : m_Vertices;
			int stride = m_AnimationDeformedVertices != null ? 2 : VertexStride;
			
			if(IsSkinned)
			{
				float[] boneTransforms = this.BoneInfluenceMatrices;

				//Mat2D inverseWorldTransform = Mat2D.Invert(new Mat2D(), worldTransform);
				float[] influenceMatrix = new float[6]{0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f};
				int boneIndexOffset = VertexBoneIndexOffset;
				int weightOffset = VertexBoneWeightOffset;
				for(int i = 0; i < m_VertexCount; i++)
				{
					float x = v[readIdx];
					float y = v[readIdx+1];

					float px, py;

					if(m_AnimationDeformedVertices != null && isSkinnedDeformInWorld)
					{
						px = x;
						py = y;
					}
					else
					{
						px = worldTransform[0] * x + worldTransform[2] * y + worldTransform[4];
						py = worldTransform[1] * x + worldTransform[3] * y + worldTransform[5];
					}

					influenceMatrix[0] = influenceMatrix[1] = influenceMatrix[2] = influenceMatrix[3] = influenceMatrix[4] = influenceMatrix[5] = 0.0f;

					for(int wi = 0; wi < 4; wi++)
					{
						int boneIndex = (int)m_Vertices[boneIndexOffset+wi];
						float weight = m_Vertices[weightOffset+wi];

						int boneTransformIndex = boneIndex*6;
						for(int j = 0; j < 6; j++)
						{
							influenceMatrix[j] += boneTransforms[boneTransformIndex+j] * weight;
						}
					}

					x = influenceMatrix[0] * px + influenceMatrix[2] * py + influenceMatrix[4];
					y = influenceMatrix[1] * px + influenceMatrix[3] * py + influenceMatrix[5];

					readIdx += stride;
					boneIndexOffset += VertexStride;
					weightOffset += VertexStride;

					buffer[writeIdx++] = x;
					buffer[writeIdx++] = y;
				}
			}
			else
			{
				Vec2D tempVec = new Vec2D();
				for(int i = 0; i < m_VertexCount; i++)
				{
					tempVec[0] = v[readIdx];
					tempVec[1] = v[readIdx+1];
					Vec2D.TransformMat2D(tempVec, tempVec, worldTransform);
					readIdx += stride;

					buffer[writeIdx++] = tempVec[0];
					buffer[writeIdx++] = tempVec[1];
				}
			}
		}
	}
}