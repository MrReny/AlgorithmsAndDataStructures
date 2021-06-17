using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shenon_Fano_Coding.Model
{
    public class Encoder
    {
        public event EventHandler FrequenciesCounted = delegate{};
        private BufferedReader _reader;
        private BufferedReader _keyReader;
        private BufferedWriter _writer;
        private BufferedWriter _keyWriter;
        private int _wordLength;
        private string FileName { get; set; }

        public List<KeyValuePair<char, long>> FrequencyList { get; private set; }

        public Dictionary<char, (long? code, int codeLength)>  EncodingTable { get; private set; }

        public Encoder(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Кодируем с переданной длинной слова
        /// </summary>
        /// <param name="encodingLength"></param>
        public async Task Encode(int encodingLength)
        {
            _reader = new BufferedReader(4096,
                new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
            _writer = new BufferedWriter(4096,
                new FileStream(FileName + ".fano", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite));

            _keyWriter = new BufferedWriter(4096,
                new FileStream(FileName + ".key", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite));

            _wordLength = encodingLength;

            // Считаем частоту
            Dictionary<char, long> frequencies = await CalculateFrequencyAsync();

            if(frequencies.Count > 0)
            {
                //Создаем сортированный список
                FrequencyList = frequencies.ToList();
                FrequencyList.Sort((val1, val2) => (val1.Value.CompareTo(val2.Value)));

                // Райзим ивент что частоты посчитаны
                FrequenciesCounted.Invoke(this, EventArgs.Empty);

                //Создаем дерево
                TreeNode root = await CreateEncodeTree(FrequencyList);

                //Создаем словарь кодировок
                EncodingTable = await CreateEncodingsAsync(root);

                // Шифруем файл

                long originalFileLength = _reader.GetFileSize();



                //Записываем длинну слова чтобы расшифорвать
                await _keyWriter.WriteCustomLength(_wordLength, sizeof(byte) * 8);

                //Записываем оригинальную длинну файла
                await _keyWriter.WriteCustomLength(originalFileLength, sizeof(long) * 8);

                //Записываем дерево кодов
                await WriteEncodingTreeAsync(root);

                long? currentWord;

                while ((currentWord = await _reader.ReadCustomLength(_wordLength)) != null)
                {
                    await _writer.WriteCustomLength(
                        (long)EncodingTable[(char)currentWord!].code!,
                        EncodingTable[(char)currentWord].codeLength);
                }
                for (int i = _wordLength - 1; i > 0; i--)
                {
                    currentWord = await _reader.ReadCustomLength(i);

                    if (currentWord != null)
                    {
                        currentWord <<= _wordLength - i;

                        await _writer.WriteCustomLength((long)EncodingTable[(char)currentWord!].code!,
                            EncodingTable[(char)currentWord].codeLength);

                    }
                }


            }
            else
            {
                //Если файл пустой

                await _writer.WriteCustomLength(_wordLength, sizeof(byte) * 8);
                await _writer.WriteCustomLength(0, sizeof(long) * 8);

            }

            await _writer.FlushBuffer();
            await _keyWriter.FlushBuffer();
            _writer.OutputStream.Close();
            _keyWriter.OutputStream.Close();
            _reader.InputStream.Close();
        }

        public async Task Decode()
        {
            _reader = new BufferedReader(4096,
                new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
            _writer = new BufferedWriter(4096,
                new FileStream(FileName + ".decoded", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite));

            _keyReader = new BufferedReader(4096,
                new FileStream(FileName.Replace(".fano",".key" ), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));

            //Читаем длинну слова, и оригинальный размер файла
            _wordLength = (int) await _keyReader.ReadCustomLength(8);
            long originalFileLength = (long) await _keyReader.ReadCustomLength(64);

            if (originalFileLength > 0)
            {
                //Парсим дерево из битов
                TreeNode root = await ParseDecodeTreeAsync();


                //Таблица декодирования
                EncodingTable = await CreateEncodingsAsync(root);

                decimal bytesWritten = 0;
                decimal wordToBytes = (decimal)_wordLength / 8;

                while(bytesWritten + wordToBytes <= originalFileLength)
                {

                    TreeNode branch = root;
                    while(branch.Leaf == null)
                    {
                        long? currentBit = await _reader.ReadCustomLength(1);
                        branch = currentBit == 1 ? branch.Right : branch.Left;

                    }
                    bytesWritten += wordToBytes;
                    await _writer.WriteCustomLength((long)branch.Leaf, _wordLength);

                }

                TreeNode branch2 = root;
                var da = (originalFileLength - bytesWritten);
                var remainder = (8 * (originalFileLength - bytesWritten));
                if (remainder > 0)
                {
                    while (true)
                    {
                        long? currentBit = await _reader.ReadCustomLength(1);

                        branch2 = currentBit == 1 ? branch2.Right : branch2.Left;
                        if (branch2.Leaf != null)
                        {
                            bytesWritten += wordToBytes;
                            var d = (int)(remainder * _wordLength);
                            await _writer.WriteCustomLength((long)(branch2.Leaf >> (_wordLength - (int)remainder)), (int) remainder);
                            break;
                        }
                    }
                }


            }

            await _writer.FlushBuffer();
            _writer.OutputStream.Close();
            _reader.InputStream.Close();
        }

        private async Task<TreeNode> ParseDecodeTreeAsync()
        {
            TreeNode parent = new TreeNode();
            await ParseTreeBitAsync(parent);
            return parent;
        }

        private async Task<TreeNode> ParseTreeBitAsync(TreeNode parent)
        {
            long? bit = await _keyReader.ReadCustomLength(1);

            //Leaf
            if (bit == 1)
            {
                long? word = await _keyReader.ReadCustomLength(_wordLength);
                parent.Leaf = (char?)word;
                return parent;
            }


            //Branch
            parent.Left = await ParseTreeBitAsync(new TreeNode());
            parent.Right = await ParseTreeBitAsync(new TreeNode());
            return parent;
        }

        /// <summary>
        /// Считаем частоту встречаемых символов
        /// </summary>
        /// <returns></returns>
        async Task<Dictionary<char, long>> CalculateFrequencyAsync()
        {
            var frequencies = new Dictionary<char, long>();

            char[] buff = new char[1];

            long? currentWord;

            while((currentWord = await _reader.ReadCustomLength(_wordLength)) != null)
            {
                if (frequencies.ContainsKey((char)currentWord))
                {
                    frequencies[(char)currentWord] += 1;
                }
                else
                {
                    frequencies.Add((char)currentWord, 1);
                }
            }

            //Check for remaining bits
            for(int i = _wordLength - 1; i > 0; i--)
            {
                currentWord = await _reader.ReadCustomLength(i);

                if(currentWord != null)
                {
                    currentWord <<= _wordLength - i;
                    if (frequencies.ContainsKey((char)currentWord))
                    {
                        frequencies[(char)currentWord] += 1;
                    }
                    else
                    {
                        frequencies.Add((char)currentWord, 1);
                    }
                    break;
                }
            }
            await _reader.ResetBufferedReader();
            return frequencies;
        }

        /// <summary>
        /// Создание дерева
        /// </summary>
        /// <param name="frequencies"></param>
        /// <returns></returns>
        private async Task<TreeNode> CreateEncodeTree(List<KeyValuePair<char, long>> frequencies)
        {
            if (frequencies.Count == 1)
            {
                char w = frequencies[0].Key;
                return new TreeNode(frequencies[0].Key);
            }

            TreeNode branch = new TreeNode();

            // Найти индекс для деления последовательности пополам
            int splitIndex = FindSplitIndex(frequencies);

            //Создаем поддеревья
            branch.Left = await CreateEncodeTree(frequencies.GetRange(0, splitIndex));
            branch.Right = await CreateEncodeTree(frequencies.GetRange(splitIndex, frequencies.Count - splitIndex));

            return branch;

        }




        /// <summary>
        /// Найти индекс для деления последовательности пополам
        /// </summary>
        /// <param name="frequencies"></param>
        /// <returns></returns>
        private int FindSplitIndex(List<KeyValuePair<char, long>> frequencies)
        {
            //Последний элемент
            long rightSum = frequencies[^1].Value;

            long leftSum = 0;

            int leftIndex = 0;
            int rightIndex = 2;

            // перебираем элементы и уравниваем суммы
            for (int i = 1; i < frequencies.Count; i++)
            {
                if (rightSum > leftSum)
                {
                    leftSum += frequencies[leftIndex++].Value;
                }
                else
                {
                    rightSum += frequencies[^rightIndex++].Value;
                }
            }
            return leftIndex;
        }

        /// <summary>
        /// Рассчет кодировки
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private async Task<Dictionary<char, (long? code, int codeLength)>> CreateEncodingsAsync(TreeNode node)
        {
            var encodings = new Dictionary<char, (long?, int)>();
            await FindEncodingsAsync(node, encodings, 0, 0);
            return encodings;
        }

        /// <summary>
        /// Для каждого символа в дереве формируется кодировка
        /// </summary>
        /// <param name="node"></param>
        /// <param name="encodings"></param>
        /// <param name="code"></param>
        /// <param name="length"></param>
        private async Task FindEncodingsAsync(TreeNode node, Dictionary<char, (long?, int)> encodings, long code, int length)
        {

            if (node.Leaf != null)
            {
                char? w = node.Leaf;
                encodings.Add((char)node.Leaf, (code, length));
            }
            else
            {
                //Увиличиваем длинну кода на 1

                //Код левого корня сдвигается в лево
                await FindEncodingsAsync(node.Left, encodings, code << 1, length + 1);

                //Код правого корня сдвигается в лево и устанавливается нулевой бит в еденицу
                await FindEncodingsAsync(node.Right, encodings, code << 1 | 1, length + 1);
            }
        }

        /// <summary>
        /// Запись древа кодировок рекурсивно
        /// </summary>
        /// <param name="node"></param>
        private async Task WriteEncodingTreeAsync(TreeNode node)
        {
            if (node.Leaf != null)
            {
                // Записываем сигнализирующий байт что это не тупиковая ветвь
                await _keyWriter.WriteCustomLength(1, 1);
                await _keyWriter.WriteCustomLength((long)node.Leaf, _wordLength);
            }
            else
            {
                // Записываем сигнализирующий байт что это тупиковая ветвь
                await _keyWriter.WriteCustomLength(0, 1);
                await WriteEncodingTreeAsync(node.Left);
                await WriteEncodingTreeAsync(node.Right);
            }
        }

    }
}