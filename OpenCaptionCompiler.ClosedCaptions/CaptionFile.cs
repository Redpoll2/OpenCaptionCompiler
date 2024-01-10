using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;

namespace OpenCaptionCompiler.ClosedCaptions
{
    public class CaptionFile : IDictionary<uint, string>
    {
        private readonly Dictionary<uint, string> _captions;

        private const int Alignment = 512;
        private const int DefaultBlockSize = 8192;

        public CaptionFile() => _captions = [];

        public CaptionFile(IDictionary<uint, string> captions) => _captions = new Dictionary<uint, string>(captions);

        public CaptionFile(IEnumerable<string> captions)
        {
            _captions = new Dictionary<uint, string>(capacity: captions.Count());

            foreach (string caption in captions)
                Add(caption);
        }

        public unsafe void Add(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"The '{nameof(value)}' cannot be null or empty.", nameof(value));

            int captionByteCount = value.Length * 2;   // UTF16: two bytes for single char
            Span<byte> captionBytes = stackalloc byte[captionByteCount];

            fixed (char* chars = value)
            fixed (byte* bytes = captionBytes)
                captionByteCount = Encoding.Unicode.GetBytes(chars, value.Length, bytes, captionBytes.Length);

            captionBytes = captionBytes.Slice(0, captionByteCount); // we got the actual number of bytes, so correct the span

            uint hash = Crc32.HashToUInt32(captionBytes);

            _captions.Add(hash, value);
        }

        public static CaptionFile ParseFile(string filePath)
        {
            return ParseStream(stream: File.OpenRead(filePath));
        }

        public static CaptionFile ParseStream(Stream stream, bool leaveOpen = false)
        {
            using var reader = new BinaryReader(stream, Encoding.Unicode, leaveOpen);

            var header = CaptionHeader.Parse(reader);
            var directory = new CaptionDirectoryEntry[header.DirectorySize];

            for (int i = 0; i < header.DirectorySize; i++)
                directory[i] = CaptionDirectoryEntry.Parse(reader);

            var texts = new Dictionary<uint, string>(capacity: header.DirectorySize);

            long dataStart = (stream.Position + Alignment - 1) & ~(Alignment - 1);

            foreach (CaptionDirectoryEntry entry in directory)
            {
                int textByteCount = entry.DataSize;
                long textBytesStart = dataStart + (entry.BlockId * DefaultBlockSize) + entry.DataStart;

                byte[] textBytes = ArrayPool<byte>.Shared.Rent(textByteCount);

                try
                {
                    stream.Seek(textBytesStart, SeekOrigin.Begin);
                    reader.Read(textBytes, 0, textByteCount);

                    string text = Encoding.Unicode.GetString(textBytes, 0, textByteCount - 2);  // deleting null-terminator

                    texts.Add(entry.Hash, text);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(textBytes);
                }
            }

            return new CaptionFile(texts);
        }

        #region Inherited methods and properties

        public ICollection<uint> Keys => ((IDictionary<uint, string>)_captions).Keys;
        public ICollection<string> Values => ((IDictionary<uint, string>)_captions).Values;
        public int Count => ((ICollection<KeyValuePair<uint, string>>)_captions).Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<uint, string>>)_captions).IsReadOnly;

        public string this[uint key] { get => ((IDictionary<uint, string>)_captions)[key]; set => ((IDictionary<uint, string>)_captions)[key] = value; }

        public void Add(uint hash, string value) => ((IDictionary<uint, string>)_captions).Add(hash, value);
        public void Add(KeyValuePair<uint, string> item) => ((ICollection<KeyValuePair<uint, string>>)_captions).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<uint, string>>)_captions).Clear();
        public bool Contains(KeyValuePair<uint, string> item) => ((ICollection<KeyValuePair<uint, string>>)_captions).Contains(item);
        public bool ContainsKey(uint hash) => ((IDictionary<uint, string>)_captions).ContainsKey(hash);
        public void CopyTo(KeyValuePair<uint, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<uint, string>>)_captions).CopyTo(array, arrayIndex);
        public bool Remove(uint hash) => ((IDictionary<uint, string>)_captions).Remove(hash);
        public bool Remove(KeyValuePair<uint, string> item) => ((ICollection<KeyValuePair<uint, string>>)_captions).Remove(item);
        public bool TryGetValue(uint hash, out string value) => ((IDictionary<uint, string>)_captions).TryGetValue(hash, out value);
        public IEnumerator<KeyValuePair<uint, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<uint, string>>)_captions).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_captions).GetEnumerator();

        #endregion
    }
}
