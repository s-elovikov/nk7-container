using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace Nk7.Container
{
    public class ListBuffer<T>
    {
        private const int DEFAULT_LIST_CAPACITY = 32;
        private const int MAX_BUFFER_SIZE = 8;

        [ThreadStatic] private static Stack<List<T>> _pool;
        [ThreadStatic] private static List<T> _primaryList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferScope GetScoped(out List<T> buffer)
        {
            buffer = Get();
            return new BufferScope(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<T> Get()
        {
            var primaryList = _primaryList;

            if (_primaryList != null)
            {
                _primaryList = null;
                primaryList.Clear();

                return primaryList;
            }

            var pool = _pool;

            if (pool != null && pool.Count > 0)
            {
                var buffer = pool.Pop();
                buffer.Clear();

                return buffer;
            }

            return new List<T>(DEFAULT_LIST_CAPACITY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Return(List<T> buffer)
        {
            if (buffer == null) return;

            if (_primaryList == null)
            {
                _primaryList = buffer;
                return;
            }

            var pool = _pool;

            if (pool == null)
            {
                _pool = pool = new Stack<List<T>>(MAX_BUFFER_SIZE / 2);
            }

            if (pool.Count < MAX_BUFFER_SIZE)
            {
                buffer.Clear();
                pool.Push(buffer);
            }
        }


        public readonly struct BufferScope : IDisposable
        {
            public readonly List<T> _buffer;

            public BufferScope(List<T> buffer)
            {
                _buffer = buffer;
            }

            public void Dispose()
            {
                Return(_buffer);
            }
        }
    }
}