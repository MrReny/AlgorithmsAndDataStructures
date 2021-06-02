using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

namespace OnePhaseMergeSort
{
    /// <summary>
    /// Однофазная сортировка естественным слиянием c 4 добполнительными файлами
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите длинну внешнего носителя <= 1000000: N = ");
            var len = Convert.ToInt32(Console.ReadLine());
            var a = new int[len];
            var rnd = new Random(len);

            Console.Write("Неупорядаченный массив: ");

            for (var i = 0; i < a.Length - 1; i++)
            {
                a[i] = rnd.Next(0, len);
            }

            Console.WriteLine($"Неупорядаченный массив: {string.Join(", ", a)} ");

            if (a.Length <= 1)
            {
                Console.WriteLine($"Упорядоченный массив: {string.Join(", ", a)}");
                Console.ReadLine();
                return;
            }

            Queue<int> lst = new Queue<int>(a);

            var (b, c) = SplitInitialData(lst);

            Console.WriteLine($"Разделенный массив 1: {string.Join(", ", b)}");
            Console.WriteLine($"Разделенный массив 2: {string.Join(", ", c)}");

            Console.WriteLine($"Упорядоченный массив: {string.Join(", ", OnePhaseMergeSort(b,c))}");
        }

        /// <summary>
        /// Первое разделение
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static (Queue<int> b, Queue<int> c) SplitInitialData(Queue<int> a)
        {
            var b = new Queue<int>();
            var c = new Queue<int>();

            var refer = c;

            var tmp = 0;

            while (a.Count > 0)
            {
                tmp = a.Peek();

                refer = refer == b ? c : b;

                refer.Enqueue(a.Dequeue());

                while ( a.Count > 0 && tmp <= a.Peek())
                {
                    refer.Enqueue(a.Dequeue());
                }
            }
            return (b, c);
        }

        /// <summary>
        /// Сортировка
        /// </summary>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        static Queue<int> OnePhaseMergeSort(Queue<int> b, Queue<int> c)
        {
            var d = new Stack<int>();
            var e = new Stack<int>();

            var sourcePare = (first:b, second:c);
            var destPare = (first:d, second:e);

            var tmp = 0;

            do
            {
                d = new Stack<int>();
                e = new Stack<int>();

                sourcePare = (first: b, second: c);
                destPare = (first: d, second: e);

                while (sourcePare.first.Count > 0 || sourcePare.second.Count > 0)
                {
                    destPare = Swap(destPare);
                    var fb = sourcePare.first.TryPeek(out var f);
                    var sb = sourcePare.second.TryPeek(out var s);

                    if (fb && sb)
                    {
                        destPare.first.Push(f < s ? sourcePare.first.Dequeue() : sourcePare.second.Dequeue());
                    }
                    else if (fb)
                    {
                        destPare.first.Push(sourcePare.first.Dequeue());
                    }
                    else if (sb)
                    {
                        destPare.first.Push(sourcePare.second.Dequeue());
                    }

                    destPare.first.TryPeek(out var dest);
                    tmp = dest;

                    while (sourcePare.first.Count > 0 || sourcePare.second.Count > 0 )
                    {
                        fb = sourcePare.first.TryPeek(out f);
                        sb = sourcePare.second.TryPeek(out s);
                        if (fb && sb && f >= tmp && s >= tmp )
                        {
                            destPare.first.Push(f < s ? sourcePare.first.Dequeue() : sourcePare.second.Dequeue());
                        }
                        else if (fb && f >= tmp)
                        {
                            destPare.first.Push(sourcePare.first.Dequeue());
                        }
                        else if (sb && s >= tmp)
                        {
                            destPare.first.Push(sourcePare.second.Dequeue());
                        }
                        else
                        {
                            break;
                        }

                        tmp = destPare.first.Peek();
                    }
                }

                b = new Queue<int>(d.Reverse());
                c = new Queue<int>(e.Reverse());

                Console.WriteLine($"Промежуточный массив 1: {string.Join(", ", b)}");
                Console.WriteLine($"Промежуточный массив 2: {string.Join(", ", c)}");

            } while (destPare.first.Count != 0 && destPare.second.Count != 0);

            return b.Count > c.Count ? b : c;
        }

        private static ValueTuple<T,T> Swap<T>(ValueTuple<T,T> t)
        {
            var tmp = t.Item1;
            t.Item1= t.Item2;
            t.Item2 = tmp;
            return t;
        }
    }
}