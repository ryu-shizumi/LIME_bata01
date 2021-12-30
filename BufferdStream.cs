using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LIME
{
    public class BufferQueue<T> : IEnumerable<T>
    {
        public int MaxCount { get; private set; }
        private T[] _buffer;
        private int _offset = 0;
        public int Count = 0;
        private T _dummy;
        private bool _useDummy;
        private IEnumerator<T> _e;

        /// <summary>
        /// 元になるデータ列・バッファサイズを設定するコンストラクタ
        /// </summary>
        /// <param name="list">元になるデータ列</param>
        /// <param name="maxCount">バッファサイズ</param>
        /// <param name="dummy">デフォルト値</param>
        public BufferQueue(IEnumerable<T> list, int maxCount)
        {
            if(maxCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            _dummy = default;
            _useDummy = false;

            MaxCount = maxCount;
            _buffer = new T[MaxCount];
            _e = list.GetEnumerator();
            Fill();
        }
        /// <summary>
        /// 元になるデータ列・バッファサイズ・デフォルト値を設定するコンストラクタ
        /// </summary>
        /// <param name="list">元になるデータ列</param>
        /// <param name="maxCount">バッファサイズ</param>
        /// <param name="dummy">デフォルト値</param>
        public BufferQueue(IEnumerable<T> list, int maxCount, T dummy)
        {
            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            _dummy = dummy;
            _useDummy = true;

            MaxCount = maxCount;
            _buffer = new T[MaxCount];
            _e = list.GetEnumerator();
            Fill();
        }

        public void Fill()
        {
            while ((Count < MaxCount) && (_e.MoveNext()))
            {
                this[Count] = _e.Current;
                Count++;
            }
        }

        public T Dequeue()
        {
            if(Count == 0)
            {
                throw new IndexOutOfRangeException();
            }

            var result = this[0];
            _offset = (_offset + 1) % MaxCount; ;
            Count--;

            Fill();

            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for(int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                if(index < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (Count <= index)
                {
                    if (_useDummy)
                    {
                        return _dummy;
                    }
                    throw new IndexOutOfRangeException();
                }
                
                var newIndex = (_offset + index) % MaxCount;
                return _buffer[newIndex];

            }

            private set
            {
                var newIndex = (_offset + index) % MaxCount;
                _buffer[newIndex] = value;
            }
        }


        //public void DebugOut()
        //{
        //    Debug.WriteLine("");
        //    Debug.WriteLine($"Buffer [0]={_buffer[0]} [1]={_buffer[1]} [2]={_buffer[2]}");
        //    Debug.WriteLine($"Index  [0]={this[0]} [1]={this[1]} [2]={this[2]}");
        //}
    }
}
