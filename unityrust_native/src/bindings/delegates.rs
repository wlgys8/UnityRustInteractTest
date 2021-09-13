use std::os::raw::c_char;
pub type UnityDVoidString = unsafe extern "C" fn(data: *const c_char);
pub type UnityDU32= unsafe extern "C" fn()->u32;
pub type UnityDVoidU32 = unsafe extern "C" fn(data:u32);
pub type UnityDVoidU32Bool = unsafe extern "C" fn(id:u32,value:bool);