using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        int n = 7;
        int lowerBound = 47000;
        int upperBound = 89000;
        int M = 10;

        // Генерация массива из n целых чисел в заданном диапазоне
        int[] array = GenerateRandomArray(n, lowerBound, upperBound);

        List<LinkedList<int>> hashTable = new List<LinkedList<int>>(M);
        for (int i = 0; i < M; i++)
        {
            hashTable.Add(new LinkedList<int>());
        }

        // Заполнение хеш-таблицы
        foreach (var item in array)
        {
            int hashIndex = HashFunction(item, M);
            hashTable[hashIndex].AddLast(item);
        }

        // Вывод исходного массива
        Console.WriteLine("Исходный массив:");
        Console.WriteLine(string.Join(", ", array));

        // Вывод хеш-таблицы
        Console.WriteLine("Хеш-таблица:");
        for (int i = 0; i < M; i++)
        {
            Console.Write($"[{i}] -> ");
            foreach (var item in hashTable[i])
            {
                Console.Write($"{item} -> ");
            }
            Console.WriteLine("null");
        }

        // Поиск элемента в хеш-таблице
        Console.WriteLine("Введите элемент для поиска в хеш-таблице:");
        int searchElement = int.Parse(Console.ReadLine());

        int searchIndex = HashFunction(searchElement, M);
        bool found = hashTable[searchIndex].Contains(searchElement);

        if (found)
        {
            Console.WriteLine($"Элемент {searchElement} найден в хеш-таблице.");
        }
        else
        {
            Console.WriteLine($"Элемент {searchElement} не найден в хеш-таблице.");
        }
    }

    // Функция генерации массива из n случайных чисел в заданном диапазоне
    static int[] GenerateRandomArray(int n, int lowerBound, int upperBound)
    {
        int[] array = new int[n];
        Random random = new Random();
        for (int i = 0; i < n; i++)
        {
            array[i] = random.Next(lowerBound, upperBound + 1);
        }
        return array;
    }

    // Простая хеш-функция для деления по модулю
    static int HashFunction(int key, int M)
    {
        return key % M;
    }
}
