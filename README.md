# Unity中C#与Rust交互

最近在学习Rust，顺带就研究了一下如何在Unity中使用Rust。写了一个小项目来测试Rust与Unity的交互，此文仅作马克之用。

Rust是一门对标C/C++的高性能语言。其独特之处在于既没有自动GC机制，也无需手动释放内存。在Unity中大约是可以用来写一些高计算量、性能敏感型的代码。 Unity目前在推的Burst Compiler / Job System也是为高性能计算服务的，两者自然是竞争关系。如果希望写一套库能同时给多个引擎使用，那么就不能使用Unity自带的那套技术方案了。 这时候C++/Rust就是一个好选项。具体用哪个呢，萝卜青菜各有所爱，反正主都不在乎。

首先，关于Rust的FFI可以参考官方文档 - [Foreign Function Interface](https://doc.rust-lang.org/nomicon/ffi.html)

这里假设我们已经知道如何使用cargo命令构建一个rust library项目，并将其编译成相应平台的动态库。在本测试项目中输出的动态库为`libunity_rust.dylib`，将其丢入Unity项目中。

下面将依次说一下如何在Unity中实现:

- 在C#里调用Rust函数/方法
- Rust返回对象到C#
- 在C#里访问Rust Struct的数据
- 在Rust里调用C#的静态函数
- 在Rust里调用C#的成员函数


# 1. C#调用Rust函数/方法

这其实很简单。。首先在Rust中函数实现如下:

```rust
#[no_mangle]
pub extern fn test_run_method(val:i32)->i32{
    return val + 1;
}
```

- `extern`表示这个函数可以被外部语言调用
- `#[no_mangle]`这个attribute告诉rust编译器不要修改这个函数名字

这个函数会对输入值val进行 `+1` 并返回

c#端定义如下:

```c#
[DllImport("unity_rust")]
private extern static int test_run_method(int val);
```

- DllImport中的"unity_rust"是Rust编译出的动态库名字

测试调用:

```c#
var result = test_run_method(100);
Assert.AreEqual(result,101); //
```

# 2. Rust返回对象到C#

现在假设我们在rust端有一个如下的struct对象:

```rust
pub struct RustObject{
    pub val:f32
}
```

我们希望在c#端能够创建RustObject对象，并对其进行维护。 

在rust端定义函数如下:
```rust
#[no_mangle]
pub extern fn rust_object_new()-> * const RustObject{
    let obj = RustObject{
        val :0.
    };
    let b = Box::new(obj);
    return Box::into_raw(b);
}
```

这里首先会构造一个RustObject实例`obj`。在rust中对象默认都是分配在栈上的，为了将obj这个对象返回给c#进行管理，我们需要将其先转移到堆上。Box是Rust中的一个智能指针，它可以将数据转移到堆上存储，并在自身内部维护一个指向堆上数据的指针。当box超出作用域时会自动回收堆上内存。

所以这里通过

```rust
let b = Box::new(obj);
```

就成功将obj转移到了堆上。

为了不让rust在函数结束时自动回收堆上内存，我们需要拿到这块内存的手动管理权限，可以通过以下方式做到:

```rust
Box::into_raw(b)
```

`Box::into_raw`可以返回Box内部维护的指针，并将box对象consume掉。
这样rust将不再负责回收其指向的堆上内存，使用者需要再恰当的时机手动对其进行回收。

c#端对应的定义:

```c#
[DllImport("unity_rust")]
internal static extern System.IntPtr rust_object_new();
```
c#中使用`System.IntPtr`来代表一个指针对象。

这样我们就把一个RustObject对象返回给了C#，由C#负责其生命周期管理。


那么如何回收这份内存呢？

在rust端我们可以实现函数如下:

```rust
#[no_mangle]
pub extern fn rust_object_dispose(ptr: * mut RustObject){
    unsafe{
        Box::from_raw(ptr);
    }
}
```

`Box::from_raw`是将指针对象重新由Box封装起来，然后函数结束时，box由于超出作用域自动对指针指向的内存进行回收。

c#端相应声明如下:

```c#
[DllImport("unity_rust")]
internal static extern void rust_object_dispose(System.IntPtr rawPtr);
```

通常而言，我们可以将`rust_object_dispose`与c#中的析构函数结合起来，这样就实现了由GC来自动回收rust端分配的内存。例如我们在c#端实现一个RustObject的绑定类如下:

```c#

public class RustObject{

    private System.IntPtr _rawPtr;

    public RustObject(){
        _rawPtr = rust_object_new();
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
}
```

# 3. C#访问Rust Struct数据

在前面我们已经成功让c#拥有了一个Rust Struct对象的所有权。接下来要访问和修改这个对象的数据。这里有两种实现方式，下面依次介绍:

## 3.1 C# Safe的方式

首先在Rust端定义set/get函数如下:

```rust
#[no_mangle]
pub extern fn rust_object_set_value(ptr: * mut RustObject,val:f32){
    let obj = unsafe{
        ptr.as_mut().expect("invalid ptr")
    };
    obj.val = val;
}

#[no_mangle]
extern fn rust_object_get_value(ptr: * const RustObject)->f32{
    let obj = unsafe {
        ptr.as_ref().expect("invalid ptr")
    };
    return obj.val;
}
```

这两个函数均接受`RustObject*`指针作为首个参数，然后对其字段进行赋值或者读取。

c#端声明如下:

```c#
[DllImport("unity_rust")]
private static extern void rust_object_set_value(System.IntPtr ptr,float val);

[DllImport("unity_rust")]
private static extern float rust_object_get_value(System.IntPtr ptr);
```

然后封装一个属性访问:

```c#
public float val{
    get{
        return rust_object_get_value(_rawPtr);
    }set{
        rust_object_set_value(_rawPtr,value);
    }
}
```

## 3.2 C# Unsafe 方式

在c#我们定义一个struct如下:

```c#
[StructLayout(LayoutKind.Sequential)]
public struct RustObjectNative{
    public float val;
}
```
注意这里的内存布局使用`LayoutKind.Sequential`.

同时rust端的struct也要加上attribute - `#[repr(C)]`:

```rust
#[repr(C)]
pub struct RustObject{
    pub val:f32
}
```

这样我们就保证了两者的内存布局一致。

然后在c#端，可以直接将rust返回的指针`System.IntPtr`转为`RustObjectNative*`指针:

```c#
private unsafe RustObjectNative* pointer{
    get{
        return (RustObjectNative*)_rawPtr;
    }
}
```

因为在c#中使用指针是unsafe的行为，所以需要unsafe标记。

然后直接对`RustObjectNative*`指针进行数据读写即可:

```c#
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
```

这种方式读写上应该更高效，但必须保证两端的struct内存对其。 对于比较复杂的对象，或者非自己可以完全掌控的对象可能难以做到这一点。

# 4. 在Rust里调用C#静态函数

某些情况下我们希望在rust中能够执行一些c#端的函数。 例如我们将库集成到unity时，希望能够在rust里调用unity的`Debug.Log`来输出一些调试日志。 

首先在rust项目里创建mod目录如下:

- src
  - bindings
    - `debug.rs`
    - `mod.rs`

`debug.rs`实现如下:

```rust

use std::ffi::CString;
use super::delegates::*;

static mut _LOG: Option<UnityDVoidString> = None;

pub fn log(data:&str){
    let c_str = CString::new(data).unwrap();
    unsafe{
        _LOG.expect("have not binded")(c_str.as_ptr());
    }
}

///在外部语言调用进行绑定
#[no_mangle]
extern fn bind_unityengine_debug_log(func:UnityDVoidString){
    unsafe{
        _LOG = Some(func);
    }
}
```

这里定义了一个静态的变量_LOG，类型为`Option<UnityDVoidString>`，其中`UnityDVoidString`是一个函数类型，定义如下:

```rust
pub type UnityDVoidString = unsafe extern "C" fn(data: *const c_char);
```

我们将在c#端，通过调用`bind_unityengine_debug_log`，将c#侧的函数指针传入到rust中，并赋给_LOG变量。

```c#
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
```

然后在rust端我们就可以通过如下代码调用debug.log:

```rust
crate::bindings::debug::log("hello, i am from rust");
```
    
# 5. 在Rust里调用c#成员函数

这里将在rust中实现一个简单的GameObject为例。在rust项目中创建:

- src
  - bindings
    - `gameobject.rs`

定义三个静态变量和相应的绑定函数，分别对应构造、析构、以及`gameObject.SetActive`函数

```rust
static mut _CONSTRUCTOR: Option<UnityDU32> = None;
static mut _DESTRUCTOR:Option<UnityDVoidU32> = None;
static mut _SET_ACTIVE:Option<UnityDVoidBool> = None;

#[no_mangle]
extern fn bind_unityengine_gameobject_constructor(func:UnityDU32){
    unsafe{
        _CONSTRUCTOR = Some(func);
    }
}
#[no_mangle]
extern fn bind_unityengine_gameobject_destructor(func:UnityDVoidU32){
    unsafe{
        _DESTRUCTOR = Some(func);
    }
}
#[no_mangle]
extern fn bind_unityengine_gameobject_set_active(func:UnityDVoidBool){
    unsafe{
        _SET_ACTIVE = Some(func);
    }
}

```

c#端声明如下:

```c#
[DllImport("unity_rust")]
private static unsafe extern void bind_unityengine_gameobject_constructor(System.Func<uint> func);
[DllImport("unity_rust")]
private static unsafe extern void bind_unityengine_gameobject_destructor(System.Action<uint> func);
[DllImport("unity_rust")]
private static unsafe extern void bind_unityengine_gameobject_set_active(System.Action<uint,bool> func);

```

constructor函数会在c#端创建一个gameObject，将其加入到一个objectCache中，并返回一个uint类型的objectId。

```c#
private static uint Constructor(){
    var go = new GameObject();
    var id = ObjectCache.Add(go);
    return id;
}
public static void Register(){
    bind_unityengine_gameobject_constructor(Constructor);
    //....
}
```

我们将这个id返回到rust中，作为c#对象在rust中的一个handle。

rust中实现GameObject如下:

```rust
pub struct GameObject{
    _unity_object_id:u32, //c# object handle
}
impl GameObject {
    pub fn new()->GameObject{
        unsafe{
            let handle = _CONSTRUCTOR.expect("GameObject have not binded")();
            return GameObject{
                _unity_object_id:handle,
            }
        }
    }
}
```

我们通过调用`_CONSTRUCTOR()`，在c#端创建一个gameObject，并在rust里拿到这个对象的id。然后在rust中用一个同名的`struct GameObject`对象包裹住这个`object_id`.

我们可以通过如下代码在rust中创建gameObject:

```rust
let go = GameObject::new();
```

那么当rust中这个go对象被回收时，我们自然需要在c#端的ObjectCache中做同步移除。否则就会有内存泄露。

因此可以在rust中为GameObject这个struct实现Drop Trait:

```rust

impl Drop for GameObject{
    fn drop(&mut self) {
        unsafe{
            _DESTRUCTOR.expect("GameObject have not binded")(self._unity_object_id);
        }
    }
}
```

当rust中go对象因为超出作用域而被回收时，会自动触发drop。在drop函数中通过调用c#侧的_DESTRUCTOR函数，实现将对象从ObjectCache中移除。_DESTRUCTOR在c#端实现如下:

```c#
private static void Destructor(uint objectId){
    ObjectCache.Remove(objectId);
}
public static void Register(){
    //....
    bind_unityengine_gameobject_destructor(Destructor);
}

```

这样我们就成功通过rust中的生命周期机制，实现了对c#对象的引用管理。

接下来以实现gameObject.SetActive这个函数为例,rust中实现如下

```rust
impl GameObject {
//.....
pub fn set_active(&mut self,value:bool){
    unsafe{
        _SET_ACTIVE.expect("GameObject have not binded")(self._unity_object_id,value);
    }
}
//....
}
```

c#端对应的_SET_ACTIVE函数:

```c#
private static void SetActive(uint objectId,bool val){
    ObjectCache.Get<GameObject>(objectId).SetActive(val);
}
public static void Register(){
    //.....
    bind_unityengine_gameobject_set_active(SetActive);
}
```

然后在rust中就可以通过如下方式进行调用:

```rust
let mut go = GameObject::new();
go.set_active(false);
```

# 6. 在Rust中更新Mesh

To be Written

# 7. 总结

以上就是在Unity中使用Rust与C#交互的过程。在绝大多数用例里，应该都是通过c#调用rust来完成高性能计算，rust调用c#多数是用在callback的时候。剩下把c#对象绑定到rust中这种行为，似乎没有什么有用的场景。








