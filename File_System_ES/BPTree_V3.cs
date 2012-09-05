using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
{
    // https://github.com/gugugupan/B-plus-tree/blob/master/bpt.cpp

    public partial class BPlusTree<T> where T : class
    {
        public Node Root { get; set; }
        public Node UncommittedRoot { get; set; }

        public Stream Index_Stream { get; set; }
        public Stream Data_Stream { get; set; }
        public int Size { get; set; }

        public Dictionary<long, int> _readMemory_Count = new Dictionary<long, int>();
        public Dictionary<long, int> _writeMemory_Count = new Dictionary<long, int>();

        public BPlusTree(Stream indexStream, Stream dataStream)
        {
            Size = 11;
            Index_Stream = indexStream;
            Data_Stream = dataStream;
            Init();
        }


        public void Commit()
        {
            Update_Node(UncommittedRoot);
        }

        private void Init()
        {
            var root = Node.Create_New(Size, true);
            Write_Node(root);
            Root = root;
        }

   

        public T Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data<T>(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, T value)
        {
            var leaf = Find_Leaf_Node(key);

            var data_Address = Data_Pointer();
            Write_Data(value, data_Address);

            for(int i=0; i< leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    leaf.Pointers[i] = data_Address;
                    Update_Node(leaf);
                    return;
                }

            Insert_in_node(leaf, key, data_Address);
        }
        public bool Delete(int key)
        {
            var leaf = Find_Leaf_Node(key);
            for (int i = 0; i < leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    Delete_Key_In_Node(leaf, key);
                    Update_Node(leaf);
                    return true;
                }
            return false;
        }

        protected void Delete_Key_In_Node(Node node, int key)
        {
            int x = 0;
            while (key != node.Keys[x]) x++;
            for (int i = x; i < node.Key_Num - 1; i++)
                node.Keys[i] = node.Keys[i + 1];
            for (int i = x + 1; i < node.Key_Num; i++)
                node.Pointers[i] = node.Pointers[i + 1];
            node.Key_Num--;
        }

        protected void Insert_in_node(Node node, int key, long address)
        {
            int x = 0;
            while (x < node.Key_Num && node.Keys[x] < key) x++;

            for (int i = node.Key_Num; i > x; i--)
                node.Keys[i] = node.Keys[i - 1];

            for (int i = node.Key_Num + 1; i > x + 1; i--)
                node.Pointers[i] = node.Pointers[i - 1];

            node.Keys[x] = key;
            node.Pointers[x + 1] = address;
            node.Key_Num++;

            if (node.Key_Num == Size) 
                Split(node);
            else
                Update_Node(node);
        }

        protected void Split(Node node)
        {
            var newNode = Node.Create_New(Size, node.IsLeaf);
            var mid_Key = node.Keys[Size / 2];

            newNode.Key_Num = Size - Size / 2 - 1;
            for (int i = 0; i < newNode.Key_Num; i++)
            {
                newNode.Keys[i] = node.Keys[i + (Size / 2 + 1)];
                newNode.Pointers[i] = node.Pointers[i + (Size / 2 + 1)];
            }

            newNode.Pointers[newNode.Key_Num] = node.Pointers[Size];
            node.Key_Num = Size / 2;

            if (node.IsLeaf)
            {
                node.Key_Num++;
                newNode.Pointers[0] = node.Pointers[0];
                Write_Node(newNode);
                node.Pointers[0] = newNode.Address;
                mid_Key = node.Keys[Size / 2 + 1];
            }
            else
                Write_Node(newNode);

            Update_Node(node);
           
            if (node.Parent == null)
            {

                Root = Node.Create_New(Size, false);
                Root.Keys[0] = mid_Key;
                Root.Pointers[0] = node.Address;
                Root.Pointers[1] = newNode.Address;
                Root.Key_Num = 1;
                node.Parent = Root;
                Write_Node(Root);

                node.Parent = newNode.Parent = Root;
            }
            else
            {
                newNode.Parent = node.Parent;
                Insert_in_node(node.Parent, mid_Key, newNode.Address);
            }
        }

        protected Node Find_Leaf_Node(int key)
        {
            Node node = Root;// Read_Node(null, 0);
            if (UncommittedRoot != null)
                node = UncommittedRoot;
            int depth = 0;
            while (!node.IsLeaf)
            {
                for (int i = 0; i <= node.Key_Num; i++)
                    if (i == node.Key_Num || key < node.Keys[i])
                    {
                        node = Read_Node(node, node.Pointers[i]);
                        depth++;
                        break;
                    }
            }
            return node;
        }


      
    }
    
}
