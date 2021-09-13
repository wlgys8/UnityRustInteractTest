mod bindings;
mod rust_object;
use bindings::gameobject::GameObject;

#[no_mangle]
pub extern fn test_run_method(val:i32)->i32{
    crate::bindings::debug::log("hello, i am from rust");
    let mut go = GameObject::new();
    go.set_active(false);
    return val + 1;
}





