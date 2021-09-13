use crate::bindings;


#[derive(Debug)]
#[repr(C)]
pub struct RustObject{
    pub val:f32
}

impl Drop for RustObject {
    fn drop(&mut self) {
        bindings::debug::log("rust object dropped!");
    }
}

#[no_mangle]
pub extern fn rust_object_new()-> * const RustObject{
    let a = RustObject{
        val :0.
    };
    let b = Box::new(a);
    return Box::into_raw(b);
}

#[no_mangle]
pub extern fn rust_object_dispose(ptr: * mut RustObject){
    unsafe{
        Box::from_raw(ptr);
    }
}

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

