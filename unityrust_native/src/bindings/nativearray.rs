use std::{marker::PhantomData, ops::{Index, IndexMut}};


pub struct NativeArray<T>{
    _ptr:* mut T,
    _size:isize,
    _type:PhantomData<T>
}


impl <T> NativeArray<T>{

    pub fn new(ptr:* mut T,size:isize)->NativeArray<T>{
        return NativeArray{
            _ptr:ptr,
            _size:size,
            _type:PhantomData{}
        };
    }

    pub fn get(&self,index:isize)->Option<&T>{
        if index >= self._size || index < 0 {
            return None;
        }
        let v = unsafe {
            self._ptr.offset(index).as_ref().unwrap()
        };
        return Some(v);
    }

    pub fn get_mut(&self,index:isize)->Option<& mut T>{
        if index >= self._size || index < 0 {
            return None;
        }
        let mut v = unsafe {
            self._ptr.offset(index).as_mut().unwrap()
        };
        return Some(v);
    }


    pub fn set(&mut self,index:isize,val:T)->bool{
        if index >= self._size || index < 0 {
            return false;
        }
        unsafe{
            * (self._ptr.offset(index)) = val;
            return true;
        }
    }

    pub fn size(&self)->isize{
        self._size
    }
}

impl <T> Index<isize> for NativeArray<T>{
    type Output = T;
    fn index(&self, index: isize) -> &Self::Output {
        return self.get(index).expect("out of bounds");
    }
}

impl <T> IndexMut<isize> for NativeArray<T> {
    fn index_mut(&mut self, index: isize) -> &mut Self::Output {
        return self.get_mut(index).expect("out of bounds");
    }
}



