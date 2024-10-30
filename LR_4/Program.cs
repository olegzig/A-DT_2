using System;
using System.Collections.Generic;

class BTreeNode
{
    public int[] Keys { get; set; }
    public BTreeNode[] Children { get; set; }
    public int KeyCount { get; set; }
    public bool IsLeaf { get; set; }

    public BTreeNode(int t, bool isLeaf)
    {
        Keys = new int[2 * t - 1];
        Children = new BTreeNode[2 * t];
        IsLeaf = isLeaf;
        KeyCount = 0;
    }
}

class BTree
{
    private readonly int _t;
    private BTreeNode _root;

    public BTree(int t)
    {
        _t = t;
        _root = null;
    }

    public void Insert(int key)
    {
        if (_root == null)
        {
            _root = new BTreeNode(_t, true);
            _root.Keys[0] = key;
            _root.KeyCount = 1;
        }
        else
        {
            if (_root.KeyCount == 2 * _t - 1)
            {
                BTreeNode newNode = new BTreeNode(_t, false);
                newNode.Children[0] = _root;
                SplitChild(newNode, 0, _root);
                InsertNonFull(newNode, key);
                _root = newNode;
            }
            else
            {
                InsertNonFull(_root, key);
            }
        }
    }

    private void SplitChild(BTreeNode parent, int index, BTreeNode child)
    {
        BTreeNode newChild = new BTreeNode(_t, child.IsLeaf);
        parent.Children[index + 1] = newChild;
        parent.Keys[index] = child.Keys[_t - 1];
        parent.KeyCount++;

        for (int i = parent.KeyCount - 1; i > index; i--)
        {
            parent.Keys[i] = parent.Keys[i - 1];
        }

        for (int i = parent.KeyCount; i > index + 1; i--)
        {
            parent.Children[i] = parent.Children[i - 1];
        }

        for (int i = 0; i < _t - 1; i++)
        {
            newChild.Keys[i] = child.Keys[i + _t];
        }

        if (!child.IsLeaf)
        {
            for (int i = 0; i < _t; i++)
            {
                newChild.Children[i] = child.Children[i + _t];
            }
        }

        child.KeyCount = _t - 1;
    }

    private void InsertNonFull(BTreeNode node, int key)
    {
        int i = node.KeyCount - 1;

        if (node.IsLeaf)
        {
            while (i >= 0 && key < node.Keys[i])
            {
                node.Keys[i + 1] = node.Keys[i];
                i--;
            }
            node.Keys[i + 1] = key;
            node.KeyCount++;
        }
        else
        {
            while (i >= 0 && key < node.Keys[i])
            {
                i--;
            }
            i++;
            if (node.Children[i].KeyCount == 2 * _t - 1)
            {
                SplitChild(node, i, node.Children[i]);
                if (key > node.Keys[i])
                {
                    i++;
                }
            }
            InsertNonFull(node.Children[i], key);
        }
    }

    public void Delete(int key)
    {
        if (_root == null)
        {
            Console.WriteLine("The tree is empty");
            return;
        }

        Delete(_root, key);

        if (_root.KeyCount == 0)
        {
            if (_root.IsLeaf)
            {
                _root = null;
            }
            else
            {
                _root = _root.Children[0];
            }
        }
    }

    private void Delete(BTreeNode node, int key)
    {
        int idx = FindKey(node, key);

        if (idx < node.KeyCount && node.Keys[idx] == key)
        {
            if (node.IsLeaf)
            {
                RemoveFromLeaf(node, idx);
            }
            else
            {
                RemoveFromNonLeaf(node, idx);
            }
        }
        else
        {
            if (node.IsLeaf)
            {
                Console.WriteLine("The key " + key + " is not found");
                return;
            }

            bool isLastChild = (idx == node.KeyCount);

            if (node.Children[idx].KeyCount < _t)
            {
                Fill(node, idx);
            }

            if (isLastChild && idx > node.KeyCount)
            {
                Delete(node.Children[idx - 1], key);
            }
            else
            {
                Delete(node.Children[idx], key);
            }
        }
    }

    private void RemoveFromLeaf(BTreeNode node, int idx)
    {
        for (int i = idx + 1; i < node.KeyCount; i++)
        {
            node.Keys[i - 1] = node.Keys[i];
        }
        node.KeyCount--;
    }

    private void RemoveFromNonLeaf(BTreeNode node, int idx)
    {
        int key = node.Keys[idx];

        if (node.Children[idx].KeyCount >= _t)
        {
            int pred = GetPredecessor(node, idx);
            node.Keys[idx] = pred;
            Delete(node.Children[idx], pred);
        }
        else if (node.Children[idx + 1].KeyCount >= _t)
        {
            int succ = GetSuccessor(node, idx);
            node.Keys[idx] = succ;
            Delete(node.Children[idx + 1], succ);
        }
        else
        {
            Merge(node, idx);
            Delete(node.Children[idx], key);
        }
    }

    private int GetPredecessor(BTreeNode node, int idx)
    {
        BTreeNode current = node.Children[idx];
        while (!current.IsLeaf)
        {
            current = current.Children[current.KeyCount];
        }
        return current.Keys[current.KeyCount - 1];
    }

    private int GetSuccessor(BTreeNode node, int idx)
    {
        BTreeNode current = node.Children[idx + 1];
        while (!current.IsLeaf)
        {
            current = current.Children[0];
        }
        return current.Keys[0];
    }

    private void Merge(BTreeNode node, int idx)
    {
        BTreeNode left = node.Children[idx];
        BTreeNode right = node.Children[idx + 1];

        left.Keys[left.KeyCount] = node.Keys[idx];

        for (int i = 0; i < right.KeyCount; i++)
        {
            left.Keys[i + left.KeyCount + 1] = right.Keys[i];
        }

        if (!left.IsLeaf)
        {
            for (int i = 0; i <= right.KeyCount; i++)
            {
                left.Children[i + left.KeyCount + 1] = right.Children[i];
            }
        }

        for (int i = idx + 1; i < node.KeyCount; i++)
        {
            node.Keys[i - 1] = node.Keys[i];
        }

        for (int i = idx + 2; i <= node.KeyCount; i++)
        {
            node.Children[i - 1] = node.Children[i];
        }

        left.KeyCount += right.KeyCount + 1;
        node.KeyCount--;
    }

    private void Fill(BTreeNode node, int idx)
    {
        if (idx != 0 && node.Children[idx - 1].KeyCount >= _t)
        {
            BorrowFromPrev(node, idx);
        }
        else if (idx != node.KeyCount && node.Children[idx + 1].KeyCount >= _t)
        {
            BorrowFromNext(node, idx);
        }
        else
        {
            if (idx != node.KeyCount)
            {
                Merge(node, idx);
            }
            else
            {
                Merge(node, idx - 1);
            }
        }
    }

    private void BorrowFromPrev(BTreeNode node, int idx)
    {
        BTreeNode child = node.Children[idx];
        BTreeNode sibling = node.Children[idx - 1];

        for (int i = child.KeyCount - 1; i >= 0; i--)
        {
            child.Keys[i + 1] = child.Keys[i];
        }

        if (!child.IsLeaf)
        {
            for (int i = child.KeyCount; i >= 0; i--)
            {
                child.Children[i + 1] = child.Children[i];
            }
        }

        child.Keys[0] = node.Keys[idx - 1];
        node.Keys[idx - 1] = sibling.Keys[sibling.KeyCount - 1];

        child.KeyCount++;
        sibling.KeyCount--;
    }

    private void BorrowFromNext(BTreeNode node, int idx)
    {
        BTreeNode child = node.Children[idx];
        BTreeNode sibling = node.Children[idx + 1];

        child.Keys[child.KeyCount] = node.Keys[idx];
        node.Keys[idx] = sibling.Keys[0];

        for (int i = 1; i < sibling.KeyCount; i++)
        {
            sibling.Keys[i - 1] = sibling.Keys[i];
        }

        if (!child.IsLeaf)
        {
            child.Children[child.KeyCount + 1] = sibling.Children[0];
            for (int i = 1; i <= sibling.KeyCount; i++)
            {
                sibling.Children[i - 1] = sibling.Children[i];
            }
        }

        child.KeyCount++;
        sibling.KeyCount--;
    }

    private int FindKey(BTreeNode node, int key)
    {
        int idx = 0;
        while (idx < node.KeyCount && node.Keys[idx] < key)
        {
            idx++;
        }
        return idx;
    }

    public void Display()
    {
        Display(_root);
    }

    private void Display(BTreeNode node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.KeyCount; i++)
            {
                if (!node.IsLeaf)
                {
                    Display(node.Children[i]);
                }
                Console.Write(node.Keys[i] + " ");
            }
            if (!node.IsLeaf)
            {
                Display(node.Children[node.KeyCount]);
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        BTree bTree = new BTree(5);
        int[] numbers = new int[15];
        Random random = new Random();

        Console.WriteLine("Введите 10-15 целых чисел (или введите 'auto' для автоматического ввода):");
        string input = Console.ReadLine();

        if (input.ToLower() == "auto")
        {
            for (int i = 0; i < 10; i++)
            {
                numbers[i] = random.Next(1, 100);
                Console.WriteLine(numbers[i]);
                bTree.Insert(numbers[i]);
            }
        }
        else
        {
            try
            {
                numbers = Array.ConvertAll(input.Split(' '), int.Parse);
                foreach (var number in numbers)
                {
                    bTree.Insert(number);
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Ошибка: неверный формат ввода. Пожалуйста, введите целые числа.");
                return;
            }
        }

        Console.WriteLine("Содержимое B-дерева:");
        bTree.Display();
        Console.WriteLine();

        Console.WriteLine("Введите число для удаления из B-дерева:");
        if (int.TryParse(Console.ReadLine(), out int deleteKey))
        {
            bTree.Delete(deleteKey);
            Console.WriteLine("Содержимое B-дерева после удаления:");
            bTree.Display();
        }
        else
        {
            Console.WriteLine("Ошибка: неверный ввод.");
        }
    }
}
