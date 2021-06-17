using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shenon_Fano_Coding.Model
{
    public delegate Task WriteDelegate(long data, int length);

    public class BufferedWriter
    {
        public long[] Buffer{ get; set; }

        public int BufferOffset{ get; set; }

        public int BitOffset{ get; set; }

        public long[]? OutputBuffer{ get; set; }

        public Stream OutputStream{ get; set; }

        public int BufferLength{ get; set; }

        public int OutputBufferOffset{ get; set; }

        public int OutputBitOffset{ get; set; }

        private readonly SemaphoreSlim _mWriteSemaphore = new SemaphoreSlim(1);

        public BufferedWriter(int bufferLength, Stream output)
        {
            BufferOffset = 0;
            OutputStream = output;

            BufferLength = bufferLength / 8;
            Buffer = new long[BufferLength];
            OutputBuffer = null;
        }

        public async Task WriteCustomLength(long data, int length)
        {
            if (BitOffset >= 64)
            {
                BitOffset = 0;
                BufferOffset++;
            }

            if (BufferOffset >= Buffer.Length)
                await GetFreshBuffer();

            int newBitOffset = BitOffset + length;
            if (newBitOffset <= 64)
            {
                // Данные полностью помещаются в байт
                BitOffset = newBitOffset;
                long bitMask = length == 64 ? -1L : ((1L << length) - 1);
                Buffer[BufferOffset] |= (data & bitMask) << (64 - BitOffset);
            }
            else
            {
                // Сначало записываем часть что помещается в текущий байт
                int fits = 64 - BitOffset;
                int notFits = length - fits;
                long bitMask = fits == 64 ? -1L : ((1L << fits) - 1);
                Buffer[BufferOffset] |= (data >> notFits) & bitMask;

                BufferOffset++;
                BitOffset = 0;
                await WriteCustomLength(data, notFits);
            }
        }

        private async Task GetFreshBuffer()
        {
            await _mWriteSemaphore.WaitAsync();
            try
            {
                OutputBuffer = Buffer;
                Buffer = new long[BufferLength];
                OutputBufferOffset = BufferOffset;
                OutputBitOffset = BitOffset;
                BufferOffset = 0;
            }
            finally
            {
                _mWriteSemaphore.Release();
            }

            WriteOutput();
        }

        private async void WriteOutput()
        {
            await _mWriteSemaphore.WaitAsync();
            try
            {
                var byteCount = OutputBufferOffset * 8;
                if (OutputBitOffset != 0)
                    byteCount += (OutputBitOffset - 1) / 8 + 1;

                if (OutputBuffer != null)
                {
                    byte[] buffer = OutputBuffer.SelectMany(l =>
                            BitConverter.IsLittleEndian ? BitConverter.GetBytes(l).Reverse() : BitConverter.GetBytes(l))
                        .Take(byteCount).ToArray();
                    await OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            finally
            {
                _mWriteSemaphore.Release();
            }
        }

        public async Task FlushBuffer()
        {
            OutputBuffer = Buffer;
            OutputBufferOffset = BufferOffset;
            OutputBitOffset = BitOffset;
            WriteOutput();
            await _mWriteSemaphore.WaitAsync();
            _mWriteSemaphore.Release();
        }
    }
}