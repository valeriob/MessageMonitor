using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V2
{
    public class BPlusTree<T>
    {
        public Node Root { get; set; }

        public Stream Stream { get; set; }
        public int M { get; set; }

        public BPlusTree(Stream stream)
        {
            M = 2;
            Stream = stream;

            Root = Node.Create_New(M, true, true);
        }


        public T Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, T value)
        {
            var node = Find_Leaf_Node(key);
        }


        protected Node Find_Leaf_Node(int key)
        {
            Node node = Root;
            while (!node.IsLeaf)
            {
                for (int i = 0; i <= node.Key_Num; i++)
                    if (i == node.Key_Num || key < node.Keys[i])
                    {
                        node = Read_Node(node, node.Pointers[i]);
                        break;
                    }
            }
            return node;
        }


        protected void Insert_in_node(Node node, int key, long valueAddress)
        {
            int x = 0;
            while (x < node.Key_Num && node.Keys[x] < key) x++;

            for (int i = node.Key_Num; i > x; i--)
                node.Keys[i] = node.Keys[i - 1];

            for (int i = node.Key_Num + 1; i > x + 1; i--)
                node.Pointers[i] = node.Pointers[i - 1];

            node.Keys[x] = key;
            node.Pointers[x + 1] = valueAddress;
            node.Key_Num++;

            if (node.Key_Num == M) // node needs split
                Split(node);
        }

        protected void Split(Node node)
        {
            var newNode = Node.Create_New(M, node.IsLeaf);
            var mid_Key = node.Keys[M / 2];

            newNode.Key_Num = M - M / 2 - 1;
            for (int i = 0; i < newNode.Key_Num; i++)
            {
                newNode.Keys[i] = node.Keys[i + (M / 2 + 1)];
                newNode.Pointers[i] = node.Pointers[i + (M / 2 + 1)];
            }

            newNode.Pointers[newNode.Key_Num] = node.Pointers[M];
            node.Key_Num = M / 2;

            if (node.IsLeaf)
            {
                node.Key_Num++;
                newNode.Pointers[0] = node.Pointers[0];
                node.Pointers[0] = newNode.Address;
                mid_Key = node.Keys[M / 2 + 1];
            }
            if (node.IsRoot)
            {
                //node->is_root = false;
                //root = Node.Create_New(M, false);
                //root->is_root = true;
                //root->key[0] = mid_key;
                //root->pointer[0] = node;
                //root->pointer[1] = nodd;
                //root->key_num = 1;

                //node->father = nodd->father = root;
            }
            else
            {
                node.Parent = newNode.Parent;
                Insert_in_node(node.Parent, mid_Key, newNode.Address);
            }
        }



        #region persistence

        private long _currentPointer;
        private long Current_Pointer()
        {
            return _currentPointer;
        }

        BinaryFormatter serializer = new BinaryFormatter();

        protected Node Read_Node(Node parent, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            var obj = serializer.Deserialize(Stream);
            var node = (Node)obj;
            node.Parent = parent;
            node.Address = address;
            return node;
        }

        protected T Read_Data(long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            var obj = serializer.Deserialize(Stream);
            return (T)obj;
        }
        protected void Update_Node(Node node)
        {
            Write_Object(node, node.Address);
        }
        protected void Write_Object(object value, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(buffer, value);
                var bytes = buffer.GetBuffer();
                // TODO CHECK INT LONG ADDRESS
                Stream.Write(bytes, 0, bytes.Length);
                _currentPointer += bytes.Length;
            }
        }

        protected void Write_Node(Node node)
        {
            var address = Current_Pointer();
            Write_Node(node, address);
        }
        protected void Write_Node(Node node, long address)
        {
            Write_Object(node, address);
            node.Address = address;
        }
        #endregion
    }


    public class Node
    {
        public bool IsRoot { get; set; }
        public bool IsLeaf { get; set; }

        public int[] Keys { get; set; }
        public long[] Pointers { get; set; }

        public Node Parent { get; set; }
        public long Address { get; set; }

        public int Key_Num { get; set; }



        public static Node Create_New(int M, bool isLeaf, bool isRoot = false)
        {
            return new Node 
            {
                IsRoot = isRoot,
                IsLeaf = isLeaf,
                Pointers = new long[M +1],
                Keys = new int[M],
            };
        }



        public long Get_Data_Address(int key)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (Keys[i] == key)
                    return Pointers[i];

            throw new Exception("Key " + key + " not found !");
        }
    }
}
