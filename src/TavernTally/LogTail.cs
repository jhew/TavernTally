using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace TavernTally
{
    public class LogTail : IDisposable
    {
        private FileSystemWatcher? _fsw;
        private FileStream? _stream;
        private StreamReader? _reader;
        private long _pos;

        public event Action<string>? OnLine;
        public event Action<string[]>? OnInitialDetectionComplete;

        public void Start(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) 
            {
                return;
            }
            
            try
            {
                _stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _reader = new StreamReader(_stream);
                
                // Process existing content from the end of the file (last 100 lines for context)
                ProcessRecentLines();
                
                _pos = _stream.Length; // start tailing new content from here
                _stream.Position = _pos;

                var dir = Path.GetDirectoryName(file)!;
                var name = Path.GetFileName(file);
                _fsw = new FileSystemWatcher(dir, name) 
                { 
                    NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite, 
                    EnableRaisingEvents = true 
                };
                _fsw.Changed += (_, __) => Pump();
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"LogTail.Start failed for {file}: {ex.Message}");
                Dispose(); // Clean up any partially initialized resources
            }
        }

        private void ProcessRecentLines()
        {
            if (_stream == null || _reader == null) return;
            
            try
            {
                // Read the last portion of the file to catch up on recent game state
                var fileLength = _stream.Length;
                const int maxReadSize = 50000; // Read up to 50KB from the end
                var startPos = Math.Max(0, fileLength - maxReadSize);
                
                _stream.Position = startPos;
                var lines = new List<string>();
                
                string? line;
                while ((line = _reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
                
                // Process MORE lines for initial detection to catch BG cards that might be further back
                var recentLines = lines.TakeLast(Math.Min(lines.Count, 500)); // Process up to 500 recent lines for better detection
                Log.Debug("Processing {Count} recent lines for initial detection", recentLines.Count());
                foreach (var recentLine in recentLines)
                {
                    OnLine?.Invoke(recentLine);
                }
                
                // Complete initial detection after processing recent lines
                // This allows LogParser to check if we detected enough BG cards for initial state
                OnInitialDetectionComplete?.Invoke(recentLines.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogTail.ProcessRecentLines error: {ex.Message}");
            }
        }

        private void Pump()
        {
            if (_reader == null || _stream == null) return;
            
            try
            {
                _stream.Position = _pos;
                string? line;
                while ((line = _reader.ReadLine()) != null)
                    OnLine?.Invoke(line);
                _pos = _stream.Position;
            }
            catch (Exception ex)
            {
                // Log the error but continue operation
                System.Diagnostics.Debug.WriteLine($"LogTail.Pump error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _fsw?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
        }
    }
}
