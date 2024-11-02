using System;

class BTreeNode
{
    public int numKeys; // Количество ключей в узле
    public int[] keys; // Массив ключей
    public BTreeNode[] children; // Массив указателей на дочерние узлы
    public bool isLeaf; // True, если узел является листом

    public BTreeNode(int degree, bool isLeaf)
    {
        this.isLeaf = isLeaf;
        keys = new int[degree - 1];
        children = new BTreeNode[degree];
        numKeys = 0;
    }
}

class BTree
{
    private BTreeNode root;
    private int degree;

    public BTree(int degree)
    {
        this.degree = degree;
        root = null;
    }

    // Разделение полного дочернего узла
    private void SplitChild(BTreeNode parent, int index)
    {
        BTreeNode child = parent.children[index];
        BTreeNode newNode = new BTreeNode(degree, child.isLeaf);
        newNode.numKeys = degree / 2 - 1;

        // Перемещение ключей и дочерних узлов в новый узел
        for (int i = 0; i < degree / 2 - 1; i++)
        {
            newNode.keys[i] = child.keys[i + degree / 2];
        }

        if (!child.isLeaf)
        {
            for (int i = 0; i < degree / 2; i++)
            {
                newNode.children[i] = child.children[i + degree / 2];
            }
        }

        child.numKeys = degree / 2 - 1;

        // Сдвиг дочерних узлов родителя
        for (int i = parent.numKeys; i > index; i--)
        {
            parent.children[i + 1] = parent.children[i];
        }
        parent.children[index + 1] = newNode;

        // Сдвиг ключей родителя
        for (int i = parent.numKeys - 1; i >= index; i--)
        {
            parent.keys[i + 1] = parent.keys[i];
        }
        parent.keys[index] = child.keys[degree / 2 - 1];
        parent.numKeys++;
    }

    // Вставка в неполный узел
    private void InsertNonFull(BTreeNode node, int key)
    {
        int i = node.numKeys - 1;

        if (node.isLeaf)
        {
            // Вставка ключа в отсортированном порядке
            while (i >= 0 && node.keys[i] > key)
            {
                node.keys[i + 1] = node.keys[i];
                i--;
            }
            node.keys[i + 1] = key;
            node.numKeys++;
        }
        else
        {
            // Поиск дочернего узла для вставки ключа
            while (i >= 0 && node.keys[i] > key)
            {
                i--;
            }
            i++;

            if (node.children[i].numKeys == degree - 1)
            {
                // Разделение дочернего узла, если он полный
                SplitChild(node, i);
                if (node.keys[i] < key)
                {
                    i++;
                }
            }
            InsertNonFull(node.children[i], key);
        }
    }

    // Вставка ключа в B-дерево
    public void Insert(int key)
    {
        if (root == null)
        {
            root = new BTreeNode(degree, true);
            root.keys[0] = key;
            root.numKeys = 1;
        }
        else
        {
            if (root.numKeys == degree - 1)
            {
                BTreeNode newRoot = new BTreeNode(degree, false);
                newRoot.children[0] = root;
                SplitChild(newRoot, 0);
                root = newRoot;
            }
            InsertNonFull(root, key);
        }
    }

    // Обход B-дерева в порядке возрастания
    public void Traverse(BTreeNode node)
    {
        if (node != null)
        {
            for (int i = 0; i < node.numKeys; i++)
            {
                Traverse(node.children[i]);
                Console.Write(node.keys[i] + " ");
            }
            Traverse(node.children[node.numKeys]);
        }
    }

    public void Traverse()
    {
        Traverse(root);
    }
}

class Program
{
    static void Main(string[] args)
    {
        BTree bTree = new BTree(4); // Степень B-дерева

        bTree.Insert(10);
        bTree.Insert(20);
        bTree.Insert(5);
        bTree.Insert(6);
        bTree.Insert(12);
        bTree.Insert(30);

        Console.Write("Обход B-дерева в порядке возрастания: ");
        bTree.Traverse();
        Console.WriteLine();
    }
}
