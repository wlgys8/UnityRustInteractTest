using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
public class BindingTest : MonoBehaviour
{
    void Start()
    {
        UnityRustBindings.RegisterUnityToRust();
        var rustObj = new RustObject();
        Debug.Log(rustObj.val);
        rustObj.val = 200;
        Debug.Log(rustObj.val);

        var rustObjUnsafe = new RustObjectUnsafe();
        Debug.Log(rustObjUnsafe.val);
        rustObjUnsafe.val = 3.1415926f;
        Debug.Log(rustObjUnsafe.val);

        var result = test_run_method(100);
        Assert.AreEqual(result,101); //

        this.CreatePlane();
    }

    private RustMeshModifier _meshModifier;

    private void CreatePlane(){
        _meshModifier = new RustMeshModifier();
        var meshFilter = new GameObject("Plane").AddComponent<MeshFilter>();
        var meshRender = meshFilter.gameObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = _meshModifier.mesh;
        meshRender.sharedMaterial = new Material(Shader.Find("Standard"));
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.Space)){
            System.GC.Collect();
        }
        _meshModifier.Update();
    }

    void OnDestroy(){
        _meshModifier.Dispose();
    }

    [DllImport("unity_rust")]
    private extern static int test_run_method(int val);

}
