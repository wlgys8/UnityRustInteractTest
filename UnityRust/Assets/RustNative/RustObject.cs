using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class RustObject{

    private System.IntPtr _rawPtr;

    public RustObject(){
        _rawPtr = rust_object_new();
    }

    public float val{
        get{
            return rust_object_get_value(_rawPtr);
        }set{
            rust_object_set_value(_rawPtr,value);
        }
    }



    private void Dispose()
    {
        if(_rawPtr != System.IntPtr.Zero){
            rust_object_dispose(_rawPtr);
            _rawPtr = System.IntPtr.Zero;
        }
    }

    ~RustObject(){
        Dispose();
    }

    [DllImport("unity_rust")]
    internal static extern System.IntPtr rust_object_new();

    [DllImport("unity_rust")]
    internal static extern void rust_object_dispose(System.IntPtr rawPtr);

    [DllImport("unity_rust")]
    private static extern void rust_object_set_value(System.IntPtr ptr,float val);

    [DllImport("unity_rust")]
    private static extern float rust_object_get_value(System.IntPtr ptr);

}

public  class RustObjectUnsafe{

    [StructLayout(LayoutKind.Sequential)]
    public struct RustObjectNative{
        public float val;
    }

    private System.IntPtr _rawPtr;

    public RustObjectUnsafe(){
        _rawPtr = RustObject.rust_object_new();
    }

    private unsafe RustObjectNative* pointer{
        get{
            return (RustObjectNative*)_rawPtr;
        }
    }

    public float val{
        get{
            unsafe{
                return pointer->val;
            }
        }set{
            unsafe{
                pointer->val = value;
            }
        }
    }

    private void Dispose()
    {
        if(_rawPtr != System.IntPtr.Zero){
            RustObject.rust_object_dispose(_rawPtr);
            _rawPtr = System.IntPtr.Zero;
        }
    }

    ~RustObjectUnsafe(){
        Dispose();
    }
}

