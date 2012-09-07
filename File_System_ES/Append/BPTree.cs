using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
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
        int current_Depth;

        public int Block_Size { get; set; }

        public BPlusTree(Stream indexStream, Stream dataStream, int order)
        {
            Size = order;
            Block_Size = Node.Size_In_Bytes(Size);

            Index_Stream = indexStream;
            Data_Stream = dataStream;

            Empty_Slots = new Queue<long>();
            Reserved_Empty_Slots = new List<long>();
            Freed_Empty_Slots = new List<long>();
            Pending_Nodes = new List<Node>();
            Cached_Nodes = new Dictionary<long, Node>();

            _index_Pointer = Math.Max(8, indexStream.Length);
            _data_Pointer = Data_Stream.Length;
            _committed_Index_Pointer = _index_Pointer;

            Init();
        }

     
        public void Commit()
        {
            try
            {
                long baseAddress = Index_Pointer();
                long nextPointer = baseAddress;
                Update_Addresses_From(Pending_Nodes, UncommittedRoot, ref nextPointer);

                var toUpdate = Pending_Nodes.OrderBy(n=> n.Address).ToArray();
                var buffer = new byte[toUpdate.Length * Block_Size];
                for (int i = 0; i < toUpdate.Length; i++)
                    toUpdate[i].To_Bytes(buffer, i * Block_Size);
                

                Index_Stream.Seek(baseAddress, SeekOrigin.Begin);
                Index_Stream.Write(buffer, 0, buffer.Length);


                Index_Stream.Seek(0, SeekOrigin.Begin);
                Index_Stream.Write(BitConverter.GetBytes(UncommittedRoot.Address), 0, 8);
                _committed_Index_Pointer = _index_Pointer = nextPointer;


                Root = UncommittedRoot;
                UncommittedRoot = null;

                //Index_Stream.Flush();

                // add free page to
                foreach (var address in Freed_Empty_Slots)
                    Empty_Slots.Enqueue(address);

                Cached_Nodes.Clear();
                foreach (var node in Pending_Nodes)
                    Cached_Nodes[node.Address] = node;

                Pending_Nodes.Clear();
                Freed_Empty_Slots.Clear();
                Reserved_Empty_Slots.Clear();
                
            }
            catch (Exception ex)
            {
                Rollback();
            }

        }

        public void Rollback()
        {
            foreach (var address in Reserved_Empty_Slots)
                Empty_Slots.Enqueue(address);

            UncommittedRoot = null;
            Index_Stream.SetLength(_committed_Index_Pointer);//poor support? write it after root node address maybe?

            _index_Pointer = _committed_Index_Pointer;


            Pending_Nodes.Clear();
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
            Commit();
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

        protected Node Insert_in_node(Node node, int key, long address, Node[] children = null)// int? keyToUpdate, long? addressToUpdate)
        {
            var newNode = node.Create_New_One_Like_This();
            newNode.Insert_Key(key, address);
            
            Node newRoot = null;
            if (newNode.Needs_To_Be_Splitted())
            {
                var split = newNode.Split();
                // TODO? trova il giusto padre per children

                if (children != null)
                    foreach (var child in children)
                        child.Parent = split.Node_Right;

                Write_Node(split.Node_Right);
                Write_Node(split.Node_Left);

                if (split.Node_Left.Parent == null) // if i'm splitting the root, i need a new up level
                {
                    var root = Node.Create_New(Size, false);
                    root.Keys[0] = split.Mid_Key;
                    root.Pointers[0] = split.Node_Left.Address;
                    root.Pointers[1] = split.Node_Right.Address;
                    root.Key_Num = 1;
                    split.Node_Left.Parent = root;
                    Write_Node(root);

                    split.Node_Left.Parent = split.Node_Right.Parent = root;
                    newRoot = root;
                }
                else
                {
                    var newParent = Insert_in_node(split.Node_Left.Parent, split.Mid_Key,
                        split.Node_Right.Address, new[] { split.Node_Left, split.Node_Right });
                    newRoot = newParent;
                }
            }
            else
            {
                if (children != null)
                    foreach (var child in children)
                        child.Parent = newNode;

                Write_Node(newNode);
                newRoot = Clone_Anchestors_Of(newNode);
            }

            return newRoot;
        }

        public Node Clone_Anchestors_Of(Node node)
        {
            if (node.Parent == null)
                return node;

            var newParent = node.Parent.Create_New_One_Like_This();
            node.Parent = newParent;
            Write_Node(newParent);

            var newGranParent = Clone_Anchestors_Of(newParent);
            //newParent.Parent = newGranParent;
            return newGranParent;
        }

        public Node Clone_Chain_To_Root(Node node, long prev_Address)
        {
            var newNode = node.Create_New_One_Like_This();

            if (newNode.Parent == null)
                return newNode;

            var parent = newNode.Parent;
            for (int i = 0; i < parent.Key_Num +1; i++)
                if (parent.Pointers[i] == prev_Address)
                    parent.Pointers[i] = newNode.Address;

            prev_Address = parent.Address;
            Write_Node(parent);

            return Clone_Chain_To_Root(parent, prev_Address);
        }

        protected Node Split(Node node)
        {
            var node_Left = node;//.Create_New_One_Like_This();
            var node_Right = Node.Create_New(Size, node_Left.IsLeaf);
            var mid_Key = node_Left.Keys[Size / 2];

            node_Right.Key_Num = Size - Size / 2 - 1;
            for (int i = 0; i < node_Right.Key_Num; i++)
            {
                node_Right.Keys[i] = node_Left.Keys[i + (Size / 2 + 1)];
                node_Right.Pointers[i] = node_Left.Pointers[i + (Size / 2 + 1)];
            }

            node_Right.Pointers[node_Right.Key_Num] = node_Left.Pointers[Size]; // double linked list
            node_Left.Key_Num = Size / 2;

            if (node_Left.IsLeaf)
            {
                node_Left.Key_Num++;
                node_Right.Pointers[0] = node_Left.Pointers[0];

                node_Left.Pointers[0] = node_Right.Address;  // double linked list
                mid_Key = node_Left.Keys[Size / 2 + 1];
            }

            Write_Node(node_Right);

            //Update_Node(node);
            //long previous_Address = node_Left.Address;
            //Write_Node(node_Left);

            if (node_Left.Parent == null) // if i'm splitting the root, i need a new up level
            {
                var root = Node.Create_New(Size, false);
                root.Keys[0] = mid_Key;
                root.Pointers[0] = node_Left.Address;
                root.Pointers[1] = node_Right.Address;
                root.Key_Num = 1;
                node_Left.Parent = root;
                Write_Node(root);

                node_Left.Parent = node_Right.Parent = root;
                return root;
            }
            else
            {
                //node_Right.Parent = node_Left.Parent;
                //for (int i = 0; i < node_Left.Parent.Key_Num + 1; i++)
                //    if (node_Left.Parent.Pointers[i] == previous_Address)
                //        node_Left.Parent.Pointers[i] = node_Left.Address;
                var newParent = Insert_in_node(node_Left.Parent, mid_Key, node_Right.Address, new [] { node_Left, node_Right});
                //node_Left.Parent = node_Right.Parent = newParent;
                return newParent;
            }
        }

       
        protected Node Find_Leaf_Node(int key)
        {
            Node node = Root;// Read_Node(null, 0);
            if (UncommittedRoot != null)
                node = UncommittedRoot;
            return Find_Leaf_Node(key, node);
        }

        protected Node Find_Leaf_Node(int key, Node root)
        {
            int depth = 0;
            while (!root.IsLeaf)
            {
                for (int i = 0; i <= root.Key_Num; i++)
                    if (i == root.Key_Num || key < root.Keys[i])
                    {
                        root = Read_Node(root, root.Pointers[i]);
                        depth++;
                        break;
                    }
            }
            current_Depth = depth;
            return root;
        }



        public void Flush()
        {
            Index_Stream.Flush();
        }

    }
    
}
