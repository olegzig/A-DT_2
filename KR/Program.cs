using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class DictionaryEntry
{
    public string Key { get; set; }
    public string Value { get; set; }
    public bool IsDeleted { get; set; } // Указывает, удалена ли запись

    public override string ToString() => $"{Key}:{Value}:{(IsDeleted ? "1" : "0")}";
}

class HashSegment
{
    public List<DictionaryEntry> Entries { get; private set; } = new List<DictionaryEntry>();

    public void AddEntry(DictionaryEntry entry)
    {
        Entries.Add(entry);
    }

    public DictionaryEntry FindEntry(string key)
    {
        return Entries.Find(e => e.Key == key && !e.IsDeleted);
    }

    public bool RemoveEntry(string key)
    {
        var entry = FindEntry(key);
        if (entry != null)
        {
            entry.IsDeleted = true;
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Entries);
    }
}

class FileHashTable
{
    private readonly string _filePath;
    private readonly int _segmentCount;
    private List<HashSegment> _segments;

    public FileHashTable(string filePath, int segmentCount)
    {
        _filePath = filePath;
        _segmentCount = segmentCount;
        _segments = new List<HashSegment>(segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            _segments.Add(new HashSegment());
        }
    }

    private int HashFunction(string key)
    {
        return Math.Abs(key.GetHashCode()) % _segmentCount;
    }

    public void Insert(string key, string value)
    {
        int segmentIndex = HashFunction(key);
        var entry = new DictionaryEntry { Key = key, Value = value, IsDeleted = false };
        _segments[segmentIndex].AddEntry(entry);
        SaveToFile();
    }

    public string Search(string key)
    {
        int segmentIndex = HashFunction(key);
        var entry = _segments[segmentIndex].FindEntry(key);
        return entry != null ? entry.Value : null;
    }

    public bool Delete(string key)
    {
        int segmentIndex = HashFunction(key);
        bool result = _segments[segmentIndex].RemoveEntry(key);
        SaveToFile();
        return result;
    }

    public void SaveToFile()
    {
        using (var writer = new StreamWriter(_filePath))
        {
            foreach (var segment in _segments)
            {
                writer.WriteLine(segment.ToString());
            }
        }
    }

    public void LoadFromFile()
    {
        if (!File.Exists(_filePath)) return;

        using (var reader = new StreamReader(_filePath))
        {
            _segments = new List<HashSegment>(_segmentCount);

            for (int i = 0; i < _segmentCount; i++)
            {
                var segment = new HashSegment();
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    var parts = line.Split(':');
                    segment.AddEntry(new DictionaryEntry
                    {
                        Key = parts[0],
                        Value = parts[1],
                        IsDeleted = parts[2] == "1"
                    });
                }
                _segments.Add(segment);
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath = "dictionary.txt";
        var dictionary = new FileHashTable(filePath, 10);

        while (true)
        {
            Console.WriteLine("Выберите операцию: 1 - Вставка, 2 - Поиск, 3 - Удаление, 0 - Выход");
            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Введите ключ: ");
                    string insertKey = Console.ReadLine();
                    Console.Write("Введите значение: ");
                    string insertValue = Console.ReadLine();
                    dictionary.Insert(insertKey, insertValue);
                    break;
                case "2":
                    Console.Write("Введите ключ для поиска: ");
                    string searchKey = Console.ReadLine();
                    Console.WriteLine($"Значение: {dictionary.Search(searchKey)}");
                    break;
                case "3":
                    Console.Write("Введите ключ для удаления: ");
                    string deleteKey = Console.ReadLine();
                    Console.WriteLine("Удаление " + (dictionary.Delete(deleteKey) ? "Успешно" : "Неудачно"));
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Некорректный ввод.");
                    break;
            }
        }
    }
}
