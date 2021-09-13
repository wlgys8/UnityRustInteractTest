
use super::delegates::*;
use std::marker::PhantomData;

static mut _CONSTRUCTOR: Option<UnityDU32> = None;
static mut _DESTRUCTOR:Option<UnityDVoidU32> = None;
static mut _SET_ACTIVE:Option<UnityDVoidU32Bool> = None;

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
extern fn bind_unityengine_gameobject_set_active(func:UnityDVoidU32Bool){
    unsafe{
        _SET_ACTIVE = Some(func);
    }
}

pub struct GameObject{
    _unity_object_id:u32,
    _not_send_sync:PhantomData<*const ()>
}

//negative trait bounds are not yet fully implemented; use marker types for now
// impl !Send for GameObject {
// }
// impl !Sync for GameObject {
// }

impl Drop for GameObject{
    fn drop(&mut self) {
        unsafe{
            _DESTRUCTOR.expect("GameObject have not binded")(self._unity_object_id);
        }
    }
}

impl GameObject {
    pub fn new()->GameObject{
        unsafe{
            let handle = _CONSTRUCTOR.expect("GameObject have not binded")();
            return GameObject{
                _unity_object_id:handle,
                _not_send_sync:PhantomData{}
            }
        }
    }

    pub fn set_active(&mut self,value:bool){
        unsafe{
            _SET_ACTIVE.expect("GameObject have not binded")(self._unity_object_id,value);
        }
    }
}


