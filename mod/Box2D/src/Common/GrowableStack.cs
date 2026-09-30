using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D
{
    public unsafe ref struct b2GrowableStack<T> where T : unmanaged
    {
        private T* _stack;
        private bool _wasReallocated;
        public int _count;
        private int _capacity;

        public b2GrowableStack(Span<T> stackSpace)
        {
            fixed (T* ap = stackSpace)
            {
                _stack = ap;
            }

            _capacity = stackSpace.Length;
            _wasReallocated = false;
            _count = 0;
        }

        public void Dispose()
        {
            if (_wasReallocated)
            {
                Marshal.FreeHGlobal((IntPtr)_stack);
                _stack = null;
            }
        }

        public void Push(in T element)
        {
            if (_count == _capacity)
            {
                var old = _stack;
                _capacity *= 2;
                var dstSize = _capacity * sizeof(T);
                _stack = (T*)Marshal.AllocHGlobal(dstSize);
                Buffer.MemoryCopy(old, _stack, dstSize, _count * sizeof(T));
                if (_wasReallocated)
                {
                    Marshal.FreeHGlobal((IntPtr)old);
                }

                _wasReallocated = true;
            }

            _stack[_count] = element;
            ++_count;
        }

        public T Pop()
        {
            Debug.Assert(_count > 0);
            --_count;
            return _stack[_count];
        }
    }
}