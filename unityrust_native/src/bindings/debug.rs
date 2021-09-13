
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