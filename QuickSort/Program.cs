using System;

namespace QuickSort
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.Write("Введите длинну массива: N = ");
            var len = Convert.ToInt32(Console.ReadLine());
            var a = new int[len];

            var rnd = new Random(len);

            Console.Write("Неупорядаченный массив: ");

            for (var i = 0; i < a.Length - 1; i++)
            {
                a[i] = rnd.Next(0, len);
            }
            Console.WriteLine($"Неупорядаченный массив: {string.Join(", ", a)} ");

            Console.WriteLine($"Упорядоченный массив: {string.Join(", ", QuickSort(a))}");

            Console.ReadLine();
        }


        //метод для обмена элементов массива
        static void Swap(ref int x, ref int y)
        {
            var b = x;
            x = y;
            y = b;
        }

        //метод возвращающий индекс опорного элемента
        static int Partition(int[] array, int minIndex, int maxIndex)
        {
            var pivot = minIndex - 1;
            for (var i = minIndex; i < maxIndex; i++)
            {
                if (array[i] < array[maxIndex])
                {
                    pivot++;
                    Swap(ref array[pivot], ref array[i]);
                }
            }

            pivot++;
            Swap(ref array[pivot], ref array[maxIndex]);
            return pivot;
        }

        //быстрая сортировка
        static int[] QuickSort(int[] array, int minIndex, int maxIndex)
        {
            if (minIndex >= maxIndex)
            {
                return array;
            }

            var pivotIndex = Partition(array, minIndex, maxIndex);
            QuickSort(array, minIndex, pivotIndex - 1);
            QuickSort(array, pivotIndex + 1, maxIndex);

            return array;
        }

        static int[] QuickSort(int[] array)
        {
            return QuickSort(array, 0, array.Length - 1);
        }


    }
}