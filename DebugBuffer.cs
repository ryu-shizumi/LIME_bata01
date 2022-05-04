using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LIME
{
    
    public class TextBuffer
    {
        private StringBuilder _buffer = new StringBuilder();

        public TextBuffer()
        {

        }

        public void Flush()
        {
            _buffer.Clear();
        }
        public void Write(string message)
        {
            _buffer.Append(message);
        }
        public void WriteLine(string message)
        {
            _buffer.AppendLine(message);
        }

        public override string ToString()
        {
            return _buffer.ToString();
        }
    }
}
