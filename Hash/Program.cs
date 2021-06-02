using System;
using System.Linq;

namespace Hash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Введите длинну последовательности: N=");
            var len = Convert.ToInt32(Console.ReadLine());
            var a = new CustomHashTable(len);

            Console.WriteLine("Введите длинну рандомных слов: strLen=");
            var slen = Convert.ToInt32(Console.ReadLine());

            for (int i = 0; i < len; i++)
            {
                var st = RandomString(slen);
                Console.Write(st +", ");
                a.Add(st);
            }

            Console.WriteLine("Введите строку которую хотите найти: ");
            var find = Console.ReadLine();

            Console.WriteLine(a.Find(find));
        }

        public static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}