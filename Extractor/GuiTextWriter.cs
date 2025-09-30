using System;
using System.IO;
using System.Text;

namespace Extractor
{
    internal sealed class GuiTextWriter : TextWriter
    {
        private readonly Action<string, bool> _log;
        private readonly bool _isError;
        private readonly StringBuilder _buffer = new();
        private readonly object _sync = new();

        public GuiTextWriter(Action<string, bool> log, bool isError)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _isError = isError;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_sync)
            {
                if (value == '\n')
                {
                    FlushBuffer();
                }
                else if (value != '\r')
                {
                    _buffer.Append(value);
                }
            }
        }

        public override void Write(string value)
        {
            if (value is null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var ch in value)
                {
                    if (ch == '\n')
                    {
                        FlushBuffer();
                    }
                    else if (ch != '\r')
                    {
                        _buffer.Append(ch);
                    }
                }
            }
        }

        public override void WriteLine(string value)
        {
            Write(value);
            Write('\n');
        }

        public override void Flush()
        {
            lock (_sync)
            {
                if (_buffer.Length > 0)
                {
                    FlushBuffer();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
            }
            base.Dispose(disposing);
        }

        private void FlushBuffer()
        {
            var line = _buffer.ToString();
            _buffer.Clear();

            if (_isError)
            {
                _log(line, true);
            }
            else
            {
                _log(line, false);
            }
        }
    }
}

