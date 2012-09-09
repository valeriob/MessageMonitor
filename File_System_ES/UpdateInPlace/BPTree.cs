using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.UpdateInPlace
{
    public partial class BPlusTree : IBPlusTree
    {
        public Node Root { get; set; }
        public Node UncommittedRoot { get; set; }

        protected Stream Index_Stream { get; set; }
        protected Stream Data_Stream { get; set; }
        
        protected int Size { get; set; }

        public Dictionary<long, int> _readMemory_Count = new Dictionary<long, int>();
        public Dictionary<long, int> _writeMemory_Count = new Dictionary<long, int>();


        public BPlusTree(Stream indexStream, Stream dataStream, int order)
        {
            Size = order;
            Index_Stream = indexStream;
            Data_Stream = dataStream;

            Empty_Slots = new Queue<long>();
            Reserved_Empty_Slots = new List<long>();
            Freed_Empty_Slots = new List<long>();

            _index_Pointer = Math.Max(8, indexStream.Length);
            _data_Pointer = Data_Stream.Length;
            _committed_Index_Pointer = _index_Pointer;

            Init();
        }


        public void Commit()
        {
            try
            {
                // TODO Write oneshot
                Index_Stream.Seek(0, SeekOrigin.Begin);
                Index_Stream.Write(BitConverter.GetBytes(UncommittedRoot.Address), 0, 8);
                _committed_Index_Pointer = _index_Pointer;

                //Index_Stream.Flush();

                // add free page to
                foreach (var address in Freed_Empty_Slots)
                    Empty_Slots.Enqueue(address);


                Freed_Empty_Slots.Clear();
                Reserved_Empty_Slots.Clear();
            }
            catch (Exception ex)
            {
                RollBack();
            }
        }

        public void RollBack()
        {
            foreach (var address in Reserved_Empty_Slots)
                Empty_Slots.Enqueue(address);

            UncommittedRoot = null;
            Index_Stream.SetLength(_committed_Index_Pointer);//poor support? write it after root node address maybe?

            _index_Pointer = _committed_Index_Pointer;

            Freed_Empty_Slots.Clear();
            Reserved_Empty_Slots.Clear();
        }

        private void Init()
        {
            //try
            //{
            //    var buffer = new byte[8];
            //    Index_Stream.Seek(0, SeekOrigin.Begin);
            //    Index_Stream.Read(buffer, 0, 8);
            //    long rootIndex = BitConverter.ToInt64(buffer, 0);

            //    Root = Read_Node(null, rootIndex);
            //    return;
            //}
            //catch (Exception) { }

            var root = Node.Create_New(Size, true);
            Write_Node(root);
            UncommittedRoot = root;
        }

   

        public byte[] Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, byte[] value)
        {
            var leaf = Find_Leaf_Node(key);

            var data_Address = Data_Pointer();
            Write_Data(value, key, 0); // todo versionFromLeaf +1;

            for(int i=0; i< leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    //leaf.Pointers[i] = data_Address;
                    //Update_Node(leaf);
                    return;
                }

            UncommittedRoot = Insert_in_node(leaf, key, data_Address);
            //Commit();
        }
        public bool Delete(int key)
        {
            var leaf = Find_Leaf_Node(key);
            for (int i = 0; i < leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    Delete_Key_In_Node(leaf, key);
                    long previous_Address = leaf.Address;
                    //Update_Node(leaf);
                    Write_Node(leaf);
                    Free_Address(previous_Address);
                    UncommittedRoot = Clone_Chain_To_Root(leaf, previous_Address);
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

        protected Node Insert_in_node(Node node, int key, long address)
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
                return Split(node);
            else
            {
                long previous_Address = node.Address;
                Write_Node(node);
                Free_Address(previous_Address);
                //Update_Node(node);
                node = Clone_Chain_To_Root(node, previous_Address);
            }
            
            return node;
        }

        public Node Clone_Chain_To_Root(Node node, long prev_Address)
        {
            if (node.Parent == null)
                return node;

            var parent = node.Parent;
            for (int i = 0; i < parent.Key_Num +1; i++)
                if (parent.Pointers[i] == prev_Address)
                    parent.Pointers[i] = node.Address;

            prev_Address = parent.Address;
            Write_Node(parent);
            Free_Address(prev_Address);
            return Clone_Chain_To_Root(parent, prev_Address);
        }

        protected Node Split(Node node)
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

            //Update_Node(node);
            long previous_Address = node.Address;
            Write_Node(node);
            Free_Address(previous_Address);

            if (node.Parent == null) // if i'm splitting the root, i need a new up level
            {

                var root = Node.Create_New(Size, false);
                root.Keys[0] = mid_Key;
                root.Pointers[0] = node.Address;
                root.Pointers[1] = newNode.Address;
                root.Key_Num = 1;
                node.Parent = root;
                Write_Node(root);

                node.Parent = newNode.Parent = Root;
                return root;
            }
            else
            {
                newNode.Parent = node.Parent;
                for (int i = 0; i < node.Parent.Key_Num + 1; i++)
                    if (node.Parent.Pointers[i] == previous_Address)
                        node.Parent.Pointers[i] = node.Address;
                return Insert_in_node(node.Parent, mid_Key, newNode.Address);
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





        public void Flush()
        {
            Index_Stream.Flush();
        }
    }
    
}
