// ReSharper disable Unity.PreferAddressByIdToGraphicsParams

using System;
using System.Collections.Generic;
using System.Linq;
using PQS.Mods;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PQS
{
    public class PQSChunk
    {
        public struct LocalData
        {
            public readonly Vector3 Up;
            public readonly Vector2 Position;
            public readonly float Scale;
            
            public readonly Vector3 Right;
            public readonly Vector3 Back;

            public LocalData(Vector3 up, Vector2 position = default, float scale = 1f)
            {
                Up = up;
                Position = position;
                Scale = scale;
                
                Right = new Vector3(Up.y, Up.z, Up.x);
                Back = Vector3.Cross(Up, Right);
            }
        }
        
        public bool IsActive { get; private set; }
        public int ChildrenCount
        {
            get { return _children.Count; }
        }
        
        public readonly GameObject GameObject;
        public readonly MeshRenderer MeshRenderer;
        public readonly MeshFilter MeshFilter;
        public readonly PQSChunk Parent;
        public readonly int DetailLevel;

        private readonly PQSTerrain _terrain;
        private readonly LocalData _localData;
        
        private readonly Mesh _mesh;
        private readonly Action _meshGenerated;
        
        private readonly List<PQSChunk> _children = new List<PQSChunk>();
        private readonly List<PQSChunk> _siblings = new List<PQSChunk>();
        
        public PQSChunk(PQSTerrain terrain, LocalData localData, PQSChunk parent = null, int detailLevel = 0, Action meshGenerated = null)
        {
            _terrain = terrain;
            _localData = localData;
            Parent = parent;
            DetailLevel = detailLevel;
            _meshGenerated = meshGenerated;
            
            IsActive = true;
            _terrain.Chunks.Add(this);
            Parent?._children.Add(this);

            GameObject = new GameObject
            {
                name = $"Chunk_{DetailLevel}_{_localData}",
                transform = { parent = _terrain.transform }
            };
            MeshRenderer = GameObject.AddComponent<MeshRenderer>();
            MeshFilter = GameObject.AddComponent<MeshFilter>();
            
            _mesh = new Mesh
            {
                name = "PQSChunk mesh"
            };
            MeshFilter.mesh = _mesh;
            MeshRenderer.material = _terrain.CelestialBodySO.Material;
            Generate();
            
            if (DetailLevel < PQSManager.MIN_DETAIL_LEVEL)
            {
                Split();
            }
        }

        public void Generate()
        {
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            NativeArray<VertexAttributeDescriptor> vertexAttributes =
                new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertexAttributes[0] =
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
            vertexAttributes[1] =
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);

            const int quadCountPerAxis = PQSManager.GLOBAL_RESOLUTION - 1;
            const int centerQuadCountPerAxis = quadCountPerAxis - 2;
            const int quadCount = quadCountPerAxis * quadCountPerAxis;
            const int vertexCount = PQSManager.GLOBAL_RESOLUTION * PQSManager.GLOBAL_RESOLUTION;
            const int triangleCount = quadCount * 6;

            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            vertexAttributes.Dispose();
            meshData.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);

            NativeArray<float3> positions = meshData.GetVertexData<float3>(0);
            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);
            NativeArray<uint> triangles = meshData.GetIndexData<uint>();

            NativeArray<int3> quantizedNormals =
                new NativeArray<int3>(vertexCount, Allocator.Temp, NativeArrayOptions.ClearMemory);

            const int floatSize = sizeof(float);
            const int float3Size = floatSize * 3;
            const int intSize = sizeof(int);
            const int int3Size = intSize * 3;
            const int noiseModSize = intSize * 6 + floatSize * 7;

            ComputeBuffer positionBuffer = new ComputeBuffer(vertexCount, float3Size);
            positionBuffer.SetData(positions);
            ComputeBuffer normalBuffer = new ComputeBuffer(vertexCount, float3Size);
            normalBuffer.SetData(normals);
            ComputeBuffer triangleBuffer = new ComputeBuffer(triangleCount, intSize);
            triangleBuffer.SetData(triangles);

            ComputeBuffer quantizedNormalBuffer = new ComputeBuffer(vertexCount, int3Size);
            quantizedNormalBuffer.SetData(quantizedNormals);
            quantizedNormals.Dispose();

            int noiseModCount = 9999;
            if (_terrain.CelestialBodySO.NoiseMods.Length > 0)
            {
                noiseModCount = _terrain.CelestialBodySO.NoiseMods.Length;
            }
            ComputeBuffer noiseModBuffer = new ComputeBuffer(noiseModCount, noiseModSize);
            noiseModBuffer.SetData(_terrain.CelestialBodySO.NoiseMods);

            ComputeShader shader = PQSManager.Instance.ChunkCS;

            int main = shader.FindKernel("Main");
            int normal = shader.FindKernel("Normal");
            int brim = shader.FindKernel("Brim");

            shader.SetBuffer(main, "PositionBuffer", positionBuffer);
            shader.SetBuffer(main, "TriangleBuffer", triangleBuffer);
            shader.SetBuffer(main, "NoiseModBuffer", noiseModBuffer);
            
            shader.SetBuffer(normal, "PositionBuffer", positionBuffer);
            shader.SetBuffer(normal, "TriangleBuffer", triangleBuffer);
            shader.SetBuffer(normal, "NormalBuffer", normalBuffer);
            shader.SetBuffer(normal, "QuantizedNormalBuffer", quantizedNormalBuffer);

            shader.SetBuffer(brim, "PositionBuffer", positionBuffer);
            shader.SetBuffer(brim, "NormalBuffer", normalBuffer);
            shader.SetBuffer(brim, "QuantizedNormalBuffer", quantizedNormalBuffer);
            
            shader.SetVector("LocalUp", _localData.Up);
            shader.SetVector("LocalRight", _localData.Right);
            shader.SetVector("LocalBack", _localData.Back);
            shader.SetVector("LocalPosition", _localData.Position);
            shader.SetFloat("LocalScale", _localData.Scale);

            shader.SetFloat("Radius", _terrain.CelestialBodySO.Radius);

            shader.SetInt("Resolution", PQSManager.GLOBAL_RESOLUTION);
            shader.SetInt("QuadCountPerAxis", quadCountPerAxis);
            shader.SetInt("CenterQuadCountPerAxis", centerQuadCountPerAxis);
            shader.SetInt("VertexCount", vertexCount);
            shader.SetInt("TriangleCount", triangleCount);

            int mainGroup = Mathf.CeilToInt(PQSManager.GLOBAL_RESOLUTION / 16f);
            int triangleGroup = Mathf.CeilToInt(triangleCount / 64f);
            int brimGroup = Mathf.CeilToInt(vertexCount / 16f);
            
            shader.Dispatch(main, mainGroup, mainGroup, 1);
            shader.Dispatch(normal, triangleGroup, 1, 1);
            shader.Dispatch(brim, brimGroup, 1, 1);
            
            quantizedNormalBuffer.Dispose();
            noiseModBuffer.Dispose();
            
        AsyncGPUReadback.Request(positionBuffer, positionRequest =>
            {
                if (positionRequest.hasError || !IsActive)
                {
                    meshDataArray.Dispose();
                    positionBuffer.Dispose();
                    normalBuffer.Dispose();
                    triangleBuffer.Dispose();
                    
                    return;
                }
                positionRequest.GetData<float3>().CopyTo(positions);
                
                AsyncGPUReadback.Request(normalBuffer, normalRequest =>
                {
                    if (normalRequest.hasError || !IsActive)
                    {
                        meshDataArray.Dispose();
                        positionBuffer.Dispose();
                        normalBuffer.Dispose();
                        triangleBuffer.Dispose();
                        
                        return;
                    }
                    normalRequest.GetData<float3>().CopyTo(normals);
                
                    AsyncGPUReadback.Request(triangleBuffer, triangleRequest =>
                    {
                        if (triangleRequest.hasError || !IsActive)
                        {
                            meshDataArray.Dispose();
                            positionBuffer.Dispose();
                            normalBuffer.Dispose();
                            triangleBuffer.Dispose();
                            
                            return;
                        }
                        triangleRequest.GetData<uint>().CopyTo(triangles);
            
                        if (_mesh != null)
                        {
                            meshData.subMeshCount = 1;
                            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount), MeshUpdateFlags.DontRecalculateBounds);
                            
                            _mesh.Clear();
                            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);
                            _mesh.RecalculateBounds();
                        }
                        else
                        {
                            meshDataArray.Dispose();
                        }
                        positionBuffer.Dispose();
                        normalBuffer.Dispose();
                        triangleBuffer.Dispose();

                        _meshGenerated?.Invoke();
                    }); 
                });
            });
        }
        
        public void Split()
        {
            if (DetailLevel >= PQSManager.MAX_DETAIL_LEVEL || !IsActive)
            {
                return;
            }
            
            int left = 4;
            
            Vector2[] offsets = {new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)};
            foreach (Vector2 offset in offsets)
            {
                float scale = _localData.Scale / 2f;
                Vector2 position = _localData.Position + offset * scale;
                
                LocalData localData = new LocalData(_localData.Up, position, scale);
                PQSChunk parent = this;
                int detailLevel = DetailLevel + 1;
                
                PQSChunk chunk = new PQSChunk(_terrain, localData, parent, detailLevel, () =>
                {
                    left--;

                    if (left == 0)
                    {
                        Disable();
                    }
                });
            }

            foreach (PQSChunk child in _children)
            {
                child._siblings.AddRange(_children);
                child._siblings.Remove(child);
            }
        }

        public void Merge()
        {
            if (DetailLevel <= PQSManager.MIN_DETAIL_LEVEL || !IsActive)
            {
                return;
            }   
            if (_siblings.Any(sibling => sibling._children.Count > 0))
            {
                return;
            }
            
            foreach (PQSChunk sibling in _siblings)
            {
                sibling.Destroy();
            }
            
            Parent.Enable();
            Destroy();
        }

        private void Destroy()
        {
            IsActive = false;
            Object.Destroy(GameObject);
            Object.Destroy(_mesh);
            _terrain.Chunks.Remove(this);
            Parent._children.Remove(this);
        }

        private void Disable()
        {
            if (!IsActive)
            {
                return;
            }
            
            IsActive = false;
            GameObject.SetActive(false);
        }

        private void Enable()
        {
            if (IsActive)
            {
                return;
            }
            
            IsActive = true;
            GameObject.SetActive(true);
        }
    }
}