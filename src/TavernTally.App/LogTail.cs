using System;
using System.IO;

namespace TavernTally.App
{
    public class LogTail : IDisposable
    {
        private FileSystemWatcher? _fsw;
        private FileStream? _stream;
        private StreamReader? _reader;
        private long _pos;

        public event Action<string>? OnLine;

        public void Start(string file)
        {
            if (!File.Exists(file)) return;
            _stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new StreamReader(_stream);
            _pos = _stream.Length; // start tailing new content only
            _stream.Position = _pos;

            var dir = Path.GetDirectoryName(file)!;
            var name = Path.GetFileName(file);
            _fsw = new FileSystemWatcher(dir, name) { NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite, EnableRaisingEvents = true };
            _fsw.Changed += (_, __) => Pump();
        }

        private void Pump()
        {
            if (_reader == null || _stream == null) return;
            _stream.Position = _pos;
            string? line;
            while ((line = _reader.ReadLine()) != null)
                OnLine?.Invoke(line);
            _pos = _stream.Position;
        }

        public void Dispose()
        {
            _fsw?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
        }
    }
}
