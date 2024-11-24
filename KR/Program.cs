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
    private readonly int _blockSize; // Размер блока (максимальное число записей в одном блоке)
    private Dictionary<int, long> _segmentTable; // Таблица сегментов (номер -> позиция в файле)

    public FileHashTable(string filePath, int segmentCount, int blockSize)
    {
        _filePath = filePath;
        _segmentCount = segmentCount;
        _blockSize = blockSize;
        _segmentTable = new Dictionary<int, long>();
        InitializeFile();
    }

    private void InitializeFile()
    {
        using (var writer = new BinaryWriter(File.Open(_filePath, FileMode.Create)))
        {
            // Инициализация пустой таблицы сегментов
            for (int i = 0; i < _segmentCount; i++)
            {
                writer.Write((long)-1); // Указатель на первый блок сегмента (-1 означает "пустой сегмент")
            }
        }

        // Загрузка таблицы сегментов в память
        for (int i = 0; i < _segmentCount; i++)
        {
            _segmentTable[i] = -1;
        }
    }

    private void LoadSegmentTable()
    {
        // Проверяем существование файла
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Файл {_filePath} не найден.");
        }

        using (var reader = new BinaryReader(File.Open(_filePath, FileMode.Open)))
        {
            // Проверяем, содержит ли файл достаточно данных для таблицы сегментов
            long expectedSize = _segmentCount * sizeof(long); // Ожидаемый размер таблицы сегментов
            if (reader.BaseStream.Length < expectedSize)
            {
                throw new InvalidDataException("Файл не содержит валидной таблицы сегментов.");
            }

            // Загружаем таблицу сегментов
            for (int i = 0; i < _segmentCount; i++)
            {
                _segmentTable[i] = reader.ReadInt64();
            }
        }
    }

    private int HashFunction(string key)
    {
        return Math.Abs(key.GetHashCode()) % _segmentCount;
    }

    public string Search(string key)
    {
        int segmentIndex = HashFunction(key);
        long blockPointer = _segmentTable[segmentIndex];

        using (var reader = new BinaryReader(File.Open(_filePath, FileMode.Open)))
        {
            while (blockPointer != -1)
            {
                reader.BaseStream.Seek(blockPointer, SeekOrigin.Begin);
                int recordCount = reader.ReadInt32(); // Число записей в блоке

                for (int i = 0; i < recordCount; i++)
                {
                    string storedKey = reader.ReadString();
                    string value = reader.ReadString();
                    bool isDeleted = reader.ReadBoolean();

                    if (storedKey == key && !isDeleted)
                        return value;
                }

                blockPointer = reader.ReadInt64(); // Указатель на следующий блок
            }
        }

        return null; // Ключ не найден
    }

    public void Insert(string key, string value)
    {
        int segmentIndex = HashFunction(key);
        long blockPointer = _segmentTable[segmentIndex];

        using (var stream = File.Open(_filePath, FileMode.OpenOrCreate))
        using (var reader = new BinaryReader(stream))
        using (var writer = new BinaryWriter(stream))
        {
            while (blockPointer != -1)
            {
                if (blockPointer < 0 || blockPointer >= stream.Length)
                {
                    throw new IOException($"Неверное значение blockPointer: {blockPointer}. Файл имеет длину {stream.Length} байт.");
                }

                stream.Seek(blockPointer, SeekOrigin.Begin);
                int recordCount = reader.ReadInt32();
                long nextBlockPointer = reader.ReadInt64();

                if (recordCount < _blockSize)
                {
                    // Записываем новую запись в текущий блок
                    stream.Seek(blockPointer + sizeof(int) + sizeof(long) + (recordCount * (key.Length + value.Length + sizeof(bool))), SeekOrigin.Begin);
                    writer.Write(key);
                    writer.Write(value);
                    writer.Write(false); // Флаг "не удалено"

                    // Увеличиваем счетчик записей
                    stream.Seek(blockPointer, SeekOrigin.Begin);
                    writer.Write(recordCount + 1);
                    return;
                }

                blockPointer = nextBlockPointer; // Переходим к следующему блоку
            }

            // Если все блоки заполнены, создаем новый
            long newBlockPointer = stream.Length;
            if (blockPointer != -1)
            {
                stream.Seek(blockPointer, SeekOrigin.Begin);
                writer.Write(newBlockPointer);
            }

            stream.Seek(newBlockPointer, SeekOrigin.Begin);
            writer.Write(1); // Одна запись в новом блоке
            writer.Write((long)-1); // Следующего блока нет
            writer.Write(key);
            writer.Write(value);
            writer.Write(false); // Флаг "не удалено"
        }
    }

    public void Delete(string key)
    {
        int segmentIndex = HashFunction(key);
        long blockPointer = _segmentTable[segmentIndex];

        using (var stream = File.Open(_filePath, FileMode.Open))
        using (var reader = new BinaryReader(stream))
        using (var writer = new BinaryWriter(stream))
        {
            while (blockPointer != -1)
            {
                stream.Seek(blockPointer, SeekOrigin.Begin);
                int recordCount = reader.ReadInt32(); // Число записей в блоке
                long nextBlockPointer = reader.ReadInt64(); // Указатель на следующий блок

                for (int i = 0; i < recordCount; i++)
                {
                    long recordPosition = stream.Position; // Сохраняем позицию записи
                    string storedKey = reader.ReadString();
                    string value = reader.ReadString();
                    bool isDeleted = reader.ReadBoolean();

                    if (storedKey == key && !isDeleted)
                    {
                        // Помечаем запись как удаленную
                        stream.Seek(recordPosition + storedKey.Length + value.Length + 1, SeekOrigin.Begin);
                        writer.Write(true); // Устанавливаем флаг удаления
                        Console.WriteLine($"Ключ '{key}' успешно удален.");
                        return;
                    }
                }

                blockPointer = nextBlockPointer; // Переходим к следующему блоку
            }
        }

        Console.WriteLine($"Ключ '{key}' не найден.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath = "dictionary.bin";
        var dictionary = new FileHashTable(filePath, segmentCount: 10, blockSize: 4);

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
                    dictionary.Delete(deleteKey);
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
