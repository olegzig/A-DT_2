using System;

class BTreeNode
{
    public int NumKeys { get; set; } // Количество ключей в узле
    public int[] Keys { get; set; } // Массив ключей
    public BTreeNode[] Children { get; set; } // Массив указателей на дочерние узлы
    public bool IsLeaf { get; set; } // True, если узел является листом

    public BTreeNode(int order, bool isLeaf)
    {
        Keys = new int[order - 1];
        Children = new BTreeNode[order];
        NumKeys = 0;
        IsLeaf = isLeaf;
    }
}

class BTree
{
    private BTreeNode root;
    private readonly int order;

    public BTree(int order)
    {
        this.order = order;
        root = null;
    }

    // Функция для вставки ключа в B-дерево
    public void Insert(int key)
    {
        if (root == null)
        {
            root = new BTreeNode(order, true);
            root.Keys[0] = key;
            root.NumKeys = 1;
        }
        else
        {
            if (root.NumKeys == order - 1)
            {
                BTreeNode newRoot = new BTreeNode(order, false);
                newRoot.Children[0] = root;
                SplitChild(newRoot, 0);
                int index = newRoot.Keys[0] < key ? 1 : 0;
                InsertNonFull(newRoot.Children[index], key);
                root = newRoot;
            }
            else
            {
                InsertNonFull(root, key);
            }
        }
    }

    // Вставка ключа в неполный узел
    private void InsertNonFull(BTreeNode node, int key)
    {
        int i = node.NumKeys - 1;

        if (node.IsLeaf)
        {
            while (i >= 0 && node.Keys[i] > key)
            {
                node.Keys[i + 1] = node.Keys[i];
                i--;
            }
            node.Keys[i + 1] = key;
            node.NumKeys++;
        }
        else
        {
            while (i >= 0 && node.Keys[i] > key)
            {
                i--;
            }
            i++;

            if (node.Children[i].NumKeys == order - 1)
            {
                SplitChild(node, i);
                if (node.Keys[i] < key)
                {
                    i++;
                }
            }
            InsertNonFull(node.Children[i], key);
        }
    }

    // Функция для разделения полного дочернего узла
    private void SplitChild(BTreeNode parent, int index)
    {
        BTreeNode child = parent.Children[index];
        BTreeNode newNode = new BTreeNode(order, child.IsLeaf);

        newNode.NumKeys = order / 2 - 1;

        // Перемещение ключей и дочерних узлов в новый узел
        for (int i = 0; i < order / 2 - 1; i++)
        {
            newNode.Keys[i] = child.Keys[i + order / 2];
        }

        if (!child.IsLeaf)
        {
            for (int i = 0; i < order / 2; i++)
            {
                newNode.Children[i] = child.Children[i + order / 2];
            }
        }

        child.NumKeys = order / 2 - 1;

        // Сдвиг дочерних узлов родителя, чтобы освободить место для нового узла
        for (int i = parent.NumKeys; i > index; i--)
        {
            parent.Children[i + 1] = parent.Children[i];
        }

        parent.Children[index + 1] = newNode;

        // Сдвиг ключей родителя, чтобы вставить средний ключ из дочернего узла
        for (int i = parent.NumKeys - 1; i >= index; i--)
        {
            parent.Keys[i + 1] = parent.Keys[i];
        }

        parent.Keys[index] = child.Keys[order / 2 - 1];
        parent.NumKeys++;
    }

    // Функция для обхода и печати B-дерева в порядке
    public void Traverse()
    {
        Traverse(root);
    }

    private void Traverse(BTreeNode node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.NumKeys; i++)
            {
                Traverse(node.Children[i]);
                Console.Write(node.Keys[i] + " ");
            }
            Traverse(node.Children[node.NumKeys]);
        }
    }
}

class Program
{
    static void Main()
    {
        BTree bTree = new BTree(5); // Создание B-дерева порядка 5

        bTree.Insert(10);
        bTree.Insert(20);
        bTree.Insert(5);
        bTree.Insert(6);
        bTree.Insert(12);
        bTree.Insert(30);

        Console.WriteLine("Обход B-дерева в порядке: ");
        bTree.Traverse();
        Console.WriteLine();
    }
}
