use std::ffi::c_void;
use crate::bindings::nativearray::NativeArray;


#[repr(C)]
struct Vertex{
    position:[f32;3],
    normal:[f32;3]
}

const PI:f32 = 3.1415926;

#[no_mangle]
extern fn rust_dynamic_update_mesh(vertex_buffer_ptr: *mut c_void,vertex_count:u32,time:f32){
    let mut vertex_buffer = NativeArray::new(vertex_buffer_ptr as * mut Vertex, vertex_count as isize);
    for i in  0..vertex_count{
        let pos = &mut vertex_buffer[i as isize].position;
        pos[0] = (time + PI * 0.5 * i as f32) .cos();
        pos[1] = (time + PI * 0.5 * i as f32) .sin();
    }
}

