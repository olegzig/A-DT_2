using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

class HashTableDictionary
{
    private const string HashTableFile = "hash_table.json";
    private const string DictionaryFile = "dictionary.json";
    private const int HashTableSize = 10;
    private const int BlockCapacity = 5;

    public HashTableDictionary()
    {
        InitializeFiles();
    }

    private void InitializeFiles()
    {
        if (!File.Exists(HashTableFile))
        {
            var hashTable = new Dictionary<string, List<string>>();
            for (int i = 0; i < HashTableSize; i++)
            {
                hashTable[i.ToString()] = new List<string>();
            }
            SaveToFile(HashTableFile, hashTable);
        }

        if (!File.Exists(DictionaryFile))
        {
            var dictionary = new Dictionary<string, List<Dictionary<string, string>>>();
            SaveToFile(DictionaryFile, dictionary);
        }
    }

    private int HashFunction(string key)
    {
        return Math.Abs(key.GetHashCode()) % HashTableSize;
    }

    private T LoadFromFile<T>(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }

    private void SaveToFile<T>(string filePath, T data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void Insert(string key, string value)
    {
        var hashTable = LoadFromFile<Dictionary<string, List<string>>>(HashTableFile);
        var dictionary = LoadFromFile<Dictionary<string, List<Dictionary<string, string>>>>(DictionaryFile);

        string hashIndex = HashFunction(key).ToString();

        foreach (string blockId in hashTable[hashIndex])
        {
            var block = dictionary[blockId];
            if (block.Count < BlockCapacity)
            {
                block.Add(new Dictionary<string, string> { { "key", key }, { "value", value } });
                dictionary[blockId] = block;
                SaveToFile(DictionaryFile, dictionary);
                return;
            }
        }

        // Создаём новый блок
        string newBlockId = $"block_{dictionary.Count + 1}";
        dictionary[newBlockId] = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string> { { "key", key }, { "value", value } }
        };
        hashTable[hashIndex].Add(newBlockId);

        SaveToFile(DictionaryFile, dictionary);
        SaveToFile(HashTableFile, hashTable);
    }

    public string Search(string key)
    {
        var hashTable = LoadFromFile<Dictionary<string, List<string>>>(HashTableFile);
        var dictionary = LoadFromFile<Dictionary<string, List<Dictionary<string, string>>>>(DictionaryFile);

        string hashIndex = HashFunction(key).ToString();

        foreach (string blockId in hashTable[hashIndex])
        {
            var block = dictionary[blockId];
            foreach (var entry in block)
            {
                if (entry["key"] == key)
                {
                    return entry["value"];
                }
            }
        }

        return null;
    }

    public bool Delete(string key)
    {
        var hashTable = LoadFromFile<Dictionary<string, List<string>>>(HashTableFile);
        var dictionary = LoadFromFile<Dictionary<string, List<Dictionary<string, string>>>>(DictionaryFile);

        string hashIndex = HashFunction(key).ToString();

        foreach (string blockId in hashTable[hashIndex])
        {
            var block = dictionary[blockId];
            for (int i = 0; i < block.Count; i++)
            {
                if (block[i]["key"] == key)
                {
                    block.RemoveAt(i);

                    if (block.Count == 0)
                    {
                        hashTable[hashIndex].Remove(blockId);
                        dictionary.Remove(blockId);
                    }
                    else
                    {
                        dictionary[blockId] = block;
                    }

                    SaveToFile(DictionaryFile, dictionary);
                    SaveToFile(HashTableFile, hashTable);
                    return true;
                }
            }
        }

        return false;
    }
}

class Program
{
    static void Main()
    {
        var hashTableDict = new HashTableDictionary();

        while (true)
        {
            Console.WriteLine("\n--- Меню ---");
            Console.WriteLine("1. Вставить элемент");
            Console.WriteLine("2. Найти элемент");
            Console.WriteLine("3. Удалить элемент");
            Console.WriteLine("4. Выйти");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Введите ключ: ");
                    string keyToInsert = Console.ReadLine();
                    Console.Write("Введите значение: ");
                    string valueToInsert = Console.ReadLine();
                    hashTableDict.Insert(keyToInsert, valueToInsert);
                    Console.WriteLine("Элемент добавлен.");
                    break;

                case "2":
                    Console.Write("Введите ключ для поиска: ");
                    string keyToSearch = Console.ReadLine();
                    string result = hashTableDict.Search(keyToSearch);
                    if (result != null)
                    {
                        Console.WriteLine($"Найдено значение: {result}");
                    }
                    else
                    {
                        Console.WriteLine("Элемент не найден.");
                    }
                    break;

                case "3":
                    Console.Write("Введите ключ для удаления: ");
                    string keyToDelete = Console.ReadLine();
                    bool deleted = hashTableDict.Delete(keyToDelete);
                    if (deleted)
                    {
                        Console.WriteLine("Элемент удалён.");
                    }
                    else
                    {
                        Console.WriteLine("Элемент не найден.");
                    }
                    break;

                case "4":
                    Console.WriteLine("Выход из программы...");
                    return;

                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }
        }
    }
}

