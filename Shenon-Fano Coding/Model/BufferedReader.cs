using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shenon_Fano_Coding.Model
{
    public delegate Task<long?> ReadDelegate(int length);

    public class BufferedReader
    {
        public long[]? Buffer{ get; set; }

        public long[]? BackupBuffer{ get; set; }

        public int BufferOffset{ get; set; }

        public int BitOffset{ get; set; }

        public Stream InputStream{ get; set; }

        public int BufferLength { get; set; } // in BYTES!

        public int MaxBufferLength{ get; set; }

        public int BackupBufferLength{ get; set; }

        public bool StreamEmpty { get; set; }

        private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(1);

        public BufferedReader(int bufferLength, Stream input)
        {
            BufferOffset = 0;
            InputStream = input;
            BufferLength = bufferLength;
            MaxBufferLength = bufferLength;
            Buffer = new long[(BufferLength - 1) / 8 + 1];
            ReadBackup();
            GetNextBuffer().Wait();
        }

        public async Task<long?> ReadCustomLength(int length)
        {
            var bufferWordLength = GetBufferWordLength();

            if (BitOffset == bufferWordLength)
            {
                BitOffset = 0;
                BufferOffset++;
                bufferWordLength = GetBufferWordLength();
            }

            if (BufferOffset == (BufferLength - 1) / 8 + 1)
            {
                await GetNextBuffer();
            }

            if (Buffer == null)
                return null;

            if (length > BufferLength * 8)
                throw new ArgumentOutOfRangeException();

            int newBitOffset = BitOffset + length;

            if (newBitOffset <= bufferWordLength)
            {
                // the entire requested word fits in current byte
                BitOffset = newBitOffset;
                long bitMask = length == 64 ? -1L : ((1L << length) - 1);
                return (Buffer[BufferOffset] >> (64 - BitOffset)) & bitMask;
            }
            else
            {
                // first, store the part that fits in current byte
                int fits = bufferWordLength - BitOffset;
                int notFits = length - fits;
                long bitMask = fits == 64 ? -1L : ((1L << fits) - 1);

                BitOffset = 0;

                var firstPart = ((Buffer[BufferOffset++] >> (64 - bufferWordLength)) & bitMask) << notFits;
                var secondPart = await ReadCustomLength(notFits);
                if (secondPart == null)
                {
                    Buffer = new[] {firstPart >> notFits};
                    BufferOffset = 0;
                    BitOffset = 64 - fits;
                    BufferLength = 8;
                    return null;
                }


                return firstPart | secondPart;
            }
        }

        public long GetFileSize()
        {
            return InputStream.Length;
        }

        public async Task ResetBufferedReader()
        {
            this.InputStream.Seek(0, SeekOrigin.Begin);
            Buffer = new long[(BufferLength - 1) / 8 + 1];
            BitOffset = 0;
            BufferLength = MaxBufferLength;
            StreamEmpty = false;
            ReadBackup();
            await GetNextBuffer();
        }

        private int GetBufferWordLength()
        {
            var bufferWordLength = (BufferOffset == (BufferLength - 1) / 8) ? (BufferLength % 8) * 8 : 64;
            if (bufferWordLength == 0)
                bufferWordLength = 64;
            return bufferWordLength;
        }

        public async Task<byte?> ReadByte()
        {
            return (byte?) (await ReadCustomLength(8));
        }

        private async Task GetNextBuffer()
        {
            await _readSemaphore.WaitAsync();
            Buffer = BackupBuffer;
            BufferLength = BackupBufferLength;
            BackupBuffer = null;
            BufferOffset = 0;
            BitOffset = 0;
            _readSemaphore.Release();

            if (!StreamEmpty)
                ReadBackup();
        }

        private async void ReadBackup()
        {
            await _readSemaphore.WaitAsync();
            try
            {
                byte[] tempBuffer = new byte[BufferLength + 8];
                int bytesRead = InputStream.Read(tempBuffer, 0, BufferLength);
                BackupBuffer = bytesRead == 0
                    ? null
                    : Enumerable.Range(0, (BufferLength - 1) / 8 + 1).Select(x =>
                        BitConverter.IsLittleEndian
                            ? BitConverter.ToInt64(tempBuffer.Skip(x * 8).Take(8).Reverse().ToArray(), 0)
                            : BitConverter.ToInt64(tempBuffer, x * 8)).ToArray();
                if (bytesRead != BufferLength)
                    StreamEmpty = true;
                BackupBufferLength = bytesRead;
            }
            finally
            {
                _readSemaphore.Release();
            }
        }
    }
}