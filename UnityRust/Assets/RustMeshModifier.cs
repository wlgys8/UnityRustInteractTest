using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

public class RustMeshModifier
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex{
        public float3 position;
        public float3 normal;
    }

    private Mesh _mesh;
    private NativeArray<Vertex> _vertexBuffer;

    public RustMeshModifier(){
        _mesh = new Mesh();
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        };
        var vertexCount = 4;
        _mesh.SetVertexBufferParams(vertexCount, layout);
        _vertexBuffer = new NativeArray<Vertex>(vertexCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
        _vertexBuffer[0] = new Vertex(){
            position = new float3(0,0,0),
            normal = new float3(0,0,-1),
        };
        _vertexBuffer[1] = new Vertex(){
            position = new float3(1,0,0),
            normal = new float3(0,0,-1),
        };
        _vertexBuffer[3] = new Vertex(){
            position = new float3(1,1,0),
            normal = new float3(0,0,-1),
        };
        _vertexBuffer[2] = new Vertex(){
            position = new float3(0,1,0),
            normal = new float3(0,0,-1),
        };
        _mesh.SetVertexBufferData(_vertexBuffer,0,0,vertexCount);
        _mesh.SetIndices(new int[]{
            0,3,1,1,3,2
        },MeshTopology.Triangles, 0);
        _mesh.MarkDynamic();
    }


    public Mesh mesh{
        get{
            return _mesh;
        }
    }

    public void Dispose(){
        _vertexBuffer.Dispose();
    }

    public void Update(){
        unsafe{
            var ptr = (System.IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(_vertexBuffer);
            var vertexCount = _vertexBuffer.Length;
            //在rust中更新vertexbuffer
            rust_dynamic_update_mesh(ptr,(uint)vertexCount,Time.time);
            _mesh.SetVertexBufferData(_vertexBuffer,0,0,vertexCount);
        }
    }

    [DllImport("unity_rust")]
    private extern static void rust_dynamic_update_mesh(System.IntPtr nativeArrayPtr,uint arraySize,float time);

}
