using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIME
{
    

    public class CharCodeStream : IEnumerable<int>
    {
        private IEnumerable<char> _text;

        public CharCodeStream(IEnumerable<char> text)
        {
            _text = text;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var queue = new BufferQueue<char>(_text, 2);

            while (queue.Count > 0)
            {
                if ((queue.Count == 2) &&
                    (char.IsHighSurrogate(queue[0])) &&
                    (char.IsLowSurrogate(queue[1])))
                {
                    yield return char.ConvertToUtf32(queue[0], queue[1]);
                    queue.Dequeue();
                    queue.Dequeue();
                }
                else
                {
                    yield return queue.Dequeue();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
