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
    private readonly string _filePath; // Основной файл для данных
    private readonly string _segmentFilePath; // Файл для таблицы сегментов
    private readonly int _segmentCount;
    private readonly int _blockSize; // Размер блока (максимальное количество записей)
    private long[] _segmentTable; // Таблица сегментов

    public FileHashTable(string filePath, string segmentFilePath, int segmentCount, int blockSize)
    {
        _filePath = filePath;
        _segmentFilePath = segmentFilePath;
        _segmentCount = segmentCount;
        _blockSize = blockSize;
        _segmentTable = new long[segmentCount];

        // Загружаем таблицу сегментов при старте
        LoadSegmentTable();
    }

    private void LoadSegmentTable()
    {
        // Проверка, что файл существует и достаточно данных для чтения
        if (!File.Exists(_segmentFilePath) || new FileInfo(_segmentFilePath).Length < _segmentCount * sizeof(long))
        {
            // Если файл не существует или содержит недостаточно данных, инициализируем таблицу заново
            InitializeSegmentTable();
            return; // Выходим из метода, так как таблица уже инициализирована и сохранена
        }

        // Файл существует и содержит достаточно данных для чтения
        using (var reader = new BinaryReader(File.Open(_segmentFilePath, FileMode.Open)))
        {
            for (int i = 0; i < _segmentCount; i++)
            {
                _segmentTable[i] = reader.ReadInt64(); // Читаем каждый указатель
            }
        }
    }

    // Инициализация таблицы сегментов
    private void InitializeSegmentTable()
    {
        for (int i = 0; i < _segmentCount; i++)
        {
            _segmentTable[i] = -1; // Указатель на пустой сегмент
        }
        SaveSegmentTable(); // Сохраняем таблицу в файл
    }

    // Сохранение таблицы сегментов в файл
    private void SaveSegmentTable()
    {
        try
        {
            using (var writer = new BinaryWriter(File.Open(_segmentFilePath, FileMode.Create)))
            {
                for (int i = 0; i < _segmentCount; i++)
                {
                    writer.Write(_segmentTable[i]); // Записываем каждый указатель
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при сохранении таблицы сегментов: {ex.Message}");
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

        using (var reader = new BinaryReader(File.Open(_filePath, FileMode.Open, FileAccess.Read)))
        {
            while (blockPointer != -1)
            {
                reader.BaseStream.Seek(blockPointer, SeekOrigin.Begin);
                if (reader.BaseStream.Position + sizeof(int) + sizeof(long) > reader.BaseStream.Length)
                {
                    throw new EndOfStreamException("Недостаточно данных для чтения записи.");
                }

                int recordCount = reader.ReadInt32();
                long nextBlockPointer = reader.ReadInt64();

                for (int i = 0; i < recordCount; i++)
                {
                    if (reader.BaseStream.Position + sizeof(int) + sizeof(long) > reader.BaseStream.Length)
                    {
                        throw new EndOfStreamException("Недостаточно данных для чтения ключа.");
                    }

                    string storedKey = reader.ReadString();
                    string value = reader.ReadString();
                    bool isDeleted = reader.ReadBoolean();

                    if (storedKey == key && !isDeleted)
                    {
                        return value;
                    }
                }

                blockPointer = nextBlockPointer;
            }
        }

        return null;
    }

    public void Insert(string key, string value)
    {
        int segmentIndex = HashFunction(key);
        long blockPointer = _segmentTable[segmentIndex];

        using (var stream = new FileStream(_filePath, FileMode.OpenOrCreate))
        using (var reader = new BinaryReader(stream))
        using (var writer = new BinaryWriter(stream))
        {
            while (blockPointer != -1)
            {
                stream.Seek(blockPointer, SeekOrigin.Begin);
                int recordCount = reader.ReadInt32();
                long nextBlockPointer = reader.ReadInt64();

                if (recordCount < _blockSize)
                {
                    stream.Seek(blockPointer + sizeof(int) + sizeof(long) + (recordCount * (key.Length + value.Length + sizeof(bool))), SeekOrigin.Begin);
                    writer.Write(key);
                    writer.Write(value);
                    writer.Write(false); // Флаг "не удалено"

                    stream.Seek(blockPointer, SeekOrigin.Begin);
                    writer.Write(recordCount + 1); // Обновляем количество записей
                    return;
                }

                blockPointer = nextBlockPointer;
            }

            // Если блоки заполнены, создаем новый блок
            long newBlockPointer = stream.Length;
            if (_segmentTable[segmentIndex] == -1)
            {
                _segmentTable[segmentIndex] = newBlockPointer; // Обновляем указатель на первый блок
                SaveSegmentTable();
            }

            stream.Seek(newBlockPointer, SeekOrigin.Begin);
            writer.Write(1); // Одна запись в новом блоке
            writer.Write((long)-1); // Следующего блока нет
            writer.Write(key);
            writer.Write(value);
            writer.Write(false); // Флаг удаления
        }
    }




    public void Delete(string key)
    {
        int segmentIndex = HashFunction(key);
        long blockPointer = _segmentTable[segmentIndex];

        using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.ReadWrite))
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
                        stream.Seek(recordPosition, SeekOrigin.Begin);
                        writer.Write(storedKey);
                        writer.Write(value);
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
    public void Compact()
    {
        string tempFilePath = _filePath + ".tmp";

        using (var stream = File.Open(_filePath, FileMode.Open))
        using (var tempStream = File.Open(tempFilePath, FileMode.Create))
        using (var reader = new BinaryReader(stream))
        using (var writer = new BinaryWriter(tempStream))
        {
            foreach (long blockPointer in _segmentTable)
            {
                if (blockPointer == -1)
                    continue;

                stream.Seek(blockPointer, SeekOrigin.Begin);
                if (reader.BaseStream.Position + sizeof(int) + sizeof(long) > reader.BaseStream.Length)
                {
                    Console.WriteLine("Недостаточно данных для чтения заголовка блока.");
                    continue;
                }

                int recordCount = reader.ReadInt32();
                long nextBlockPointer = reader.ReadInt64();

                Console.WriteLine($"BlockPointer: {blockPointer}, RecordCount: {recordCount}, NextBlockPointer: {nextBlockPointer}");

                // Временная структура для хранения актуальных записей блока
                var entries = new List<DictionaryEntry>();

                for (int i = 0; i < recordCount; i++)
                {
                    if (reader.BaseStream.Position + sizeof(int) + sizeof(long) > reader.BaseStream.Length)
                    {
                        Console.WriteLine("Недостаточно данных для чтения записи.");
                        break; // Выходим из цикла, если недостаточно данных для чтения
                    }

                    string key = reader.ReadString();
                    string value = reader.ReadString();
                    bool isDeleted = reader.ReadBoolean();

                    Console.WriteLine($"Key: {key}, Value: {value}, IsDeleted: {isDeleted}");

                    if (!isDeleted)
                    {
                        entries.Add(new DictionaryEntry { Key = key, Value = value, IsDeleted = false });
                    }
                }

                // Записываем только актуальные записи в новый файл
                if (entries.Count > 0)
                {
                    long newBlockPointer = tempStream.Position;
                    writer.Write(entries.Count);
                    writer.Write((long)-1);

                    foreach (var entry in entries)
                    {
                        writer.Write(entry.Key);
                        writer.Write(entry.Value);
                        writer.Write(entry.IsDeleted);
                    }

                    Console.WriteLine($"Новый блок записан: {newBlockPointer}, Записей: {entries.Count}");

                    // Обновляем указатель в таблице сегментов
                    _segmentTable[Array.IndexOf(_segmentTable, blockPointer)] = newBlockPointer;
                }
            }
        }

        // Обновляем файл сегментов и заменяем старый файл новым
        SaveSegmentTable();
        File.Delete(_filePath);
        File.Move(tempFilePath, _filePath);
    }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath = "dictionary.bin";
        string segmentFilePath = "segmentFile.bin";
        var dictionary = new FileHashTable(filePath: filePath, segmentFilePath: segmentFilePath, segmentCount: 10,  blockSize: 4);

        while (true)
        {
            Console.WriteLine("Выберите операцию: 1 - Вставка, 2 - Поиск, 3 - Удаление, 4 - сжатие файла, 0 - Выход");
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
                case "4":
                    dictionary.Compact();
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
