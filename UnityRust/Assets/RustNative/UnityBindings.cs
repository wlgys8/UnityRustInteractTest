using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class ObjectCache{
    private static Dictionary<uint,object> _objectCache = new Dictionary<uint, object>();
    private static uint _globalId = 0;

    public static uint Add(object obj){
        var id = _globalId ++;
        _objectCache.Add(id,obj);
        return id;
    }

    public static bool Remove(uint id){
        return _objectCache.Remove(id);
    }
    public static T Get<T>(uint id){
        return (T)_objectCache[id];
    }
}

public class GameObjectBinding{


    [DllImport("unity_rust")]
    private static unsafe extern void bind_unityengine_gameobject_constructor(System.Func<uint> func);
    [DllImport("unity_rust")]
    private static unsafe extern void bind_unityengine_gameobject_destructor(System.Action<uint> func);
    [DllImport("unity_rust")]
    private static unsafe extern void bind_unityengine_gameobject_set_active(System.Action<uint,bool> func);

    private static uint Constructor(){
        var go = new GameObject();
        var id = ObjectCache.Add(go);
        return id;
    }

    private static void Destructor(uint objectId){
        ObjectCache.Remove(objectId);
    }

    private static void SetActive(uint objectId,bool val){
        ObjectCache.Get<GameObject>(objectId).SetActive(val);
    }

    public static void Register(){
        bind_unityengine_gameobject_constructor(Constructor);
        bind_unityengine_gameobject_destructor(Destructor);
        bind_unityengine_gameobject_set_active(SetActive);
    }
}

public class DebugBinding{

    /// <summary>
    /// 调用Register，注册相关函数到rust中
    /// </summary>
    public static void Register(){
        bind_unityengine_debug_log(unity_log);
    }
    private static void unity_log(string msg){
        UnityEngine.Debug.Log(msg);
    }
    [DllImport("unity_rust")]
    private static unsafe extern void bind_unityengine_debug_log(System.Action<string> func);
}

public static class UnityRustBindings{
    public static void RegisterUnityToRust(){
        DebugBinding.Register();
        GameObjectBinding.Register();
    }

}
