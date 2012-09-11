using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree : IBPlusTree
    {
        public Node Root { get; set; }
        Pending_Changes Pending_Changes;

        protected Stream Index_Stream { get; set; }
        protected Stream Data_Stream { get; set; }
        protected Stream Metadata_Stream { get; set; }
        
        protected int Size { get; set; }

        public List<Block_Group> Empty_Slots = new List<Block_Group>();
        public Dictionary<long, Node> Cached_Nodes { get; set; }

        public Dictionary<long, int> _readMemory_Count = new Dictionary<long, int>();
        public Dictionary<long, int> _writeMemory_Count = new Dictionary<long, int>();
        public long cache_hits;
        public long cache_misses;

        public int Block_Size { get; set; }

        public BPlusTree(Stream metadataStream, Stream indexStream, Stream dataStream, int order)
        {
            Size = order;
            Block_Size = Node.Size_In_Bytes(Size);

            Index_Stream = indexStream;
            Data_Stream = dataStream;
            Metadata_Stream = metadataStream;

            Cached_Nodes = new Dictionary<long, Node>();

            _index_Pointer = Math.Max(8, indexStream.Length);
            _data_Pointer = Data_Stream.Length;

            Init();
        }


        public int commitsCount;
        public int writes;

        public Usage Count_Empty_Slots()
        {
            int invalid = 0;
            int valid = 0;
            int blockSize = File_System_ES.Append.Node.Size_In_Bytes(3);
            long position = Index_Stream.Position;

            Index_Stream.Seek(8, SeekOrigin.Begin);
            var buffer = new byte[blockSize];
            while (Index_Stream.Read(buffer, 0, buffer.Length) > 0)
            {
                var node = File_System_ES.Append.Node.From_Bytes(buffer, 3);
                if (node.IsValid)
                    valid++;
                else
                    invalid++;
            }

            int used = valid * blockSize;
            int wasted = invalid * blockSize;

            Index_Stream.Seek(position, SeekOrigin.Begin);
            return new Usage { Invalid = invalid, Valid = valid };
        }

        public void Commit()
        {
            writes++;
            Metadata_Stream.Seek(0, SeekOrigin.Begin);
            Metadata_Stream.Write(BitConverter.GetBytes(Pending_Changes.Uncommitted_Root.Address), 0, 8);
            Metadata_Stream.Flush();

            Pending_Changes.Add_Block_Address_To_Available_Space();

            //foreach (var address in Pending_Changes.Freed_Empty_Slots)
            //{
            //    Index_Stream.Seek(address, SeekOrigin.Begin);
            //    Index_Stream.Write(BitConverter.GetBytes(-1), 0, 4);
            //}
            //var usage = Count_Empty_Slots();

            Root = Pending_Changes.Uncommitted_Root;

            // TODO persist empty pages on metadata
            Cached_Nodes.Clear();
            foreach (var node in Pending_Changes.Last_Cached_Nodes())
                Cached_Nodes[node.Address] = node;

            _index_Pointer = Pending_Changes.Get_Index_Pointer();
            Empty_Slots = Pending_Changes.Get_Empty_Slots();

            Pending_Changes = null;

            Index_Stream.Flush();
        }

        public void RollBack()
        {
            Index_Stream.SetLength(_index_Pointer);
            Cached_Nodes.Clear();
            Pending_Changes = null;
        }

        private void Init()
        {
            try
            {
                var buffer = new byte[8];
                Metadata_Stream.Seek(0, SeekOrigin.Begin);
                Metadata_Stream.Read(buffer, 0, 8);
                long rootIndex = BitConverter.ToInt64(buffer, 0);

                Root = Read_Node(null, rootIndex);
                if(Root.IsValid)
                    return;
            }
            catch (Exception) { }
            
            Pending_Changes = new Pending_Changes(Index_Stream, Block_Size, _index_Pointer, Empty_Slots);
            var root = Node.Create_New(Size, true);
            Write_Node(root);
            Pending_Changes.Append_New_Root(root);
        }


        Dictionary<long, Node> cache = new Dictionary<long, Node>();
        public byte[] Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, byte[] value)
        {
            Node leaf = Find_Leaf_Node(key);

            var data_Address = Data_Pointer();
            Write_Data(value, key, 1);

            if (Pending_Changes == null)
                Pending_Changes = new Pending_Changes(Index_Stream, Block_Size, _index_Pointer, Empty_Slots);

            Node newRoot = null;
            for(int i=0; i< leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    var newLeaf = leaf.Create_New_One_Like_This();
                    newLeaf.Versions[i]++;
                    newLeaf.Pointers[i] = data_Address;

                    Write_Data(value, key, newLeaf.Versions[i]); 

                    newRoot = Clone_Anchestors_Of(newLeaf);

                    Pending_Changes.Append_New_Root(newRoot);
                    return;
                }

            newRoot = Insert_in_node(leaf, key, data_Address);

            Pending_Changes.Append_New_Root(newRoot);
        }

        public bool Delete(int key)
        {
            var leaf = Find_Leaf_Node(key);
            for (int i = 0; i < leaf.Key_Num; i++)
                if (key == leaf.Keys[i])
                {
                    Delete_Key_In_Node(leaf, key);
                    long previous_Address = leaf.Address;
                    Write_Node(leaf);
                    //UncommittedRoot = Clone_Anchestors_Of(leaf);
                    return true;
                }
            return false;
        }

        public void Flush()
        {
            Index_Stream.Flush();
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

        protected Node Insert_in_node(Node node, int key, long address, Node[] children = null)
        {
            var newNode = node.Create_New_One_Like_This();
            newNode.Insert_Key(key, address);
            
            Node newRoot = null;
            if (newNode.Needs_To_Be_Splitted())
            {
                var split = newNode.Split();

                if (children != null)
                    foreach (var child in children)
                    {
                        if(child.Keys[child.Key_Num - 1] < split.Mid_Key)
                            child.Parent = split.Node_Left;
                        else
                            child.Parent = split.Node_Right;
                    }

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
                    newRoot = Insert_in_node(split.Node_Left.Parent, split.Mid_Key, split.Node_Right.Address, new[] { split.Node_Left, split.Node_Right });
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

        protected Node Clone_Anchestors_Of(Node node)
        {
            if (node.Parent == null)
                return node;

            var newParent = node.Parent.Create_New_One_Like_This();
            node.Parent = newParent;
            Write_Node(newParent);

            return Clone_Anchestors_Of(newParent);
        }

        protected Node Find_Leaf_Node(int key)
        {
            Node node = Root;
            if(Pending_Changes != null)
                node = Pending_Changes.Uncommitted_Root;

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
                        if (!root.IsValid)
                            throw new Exception("An Invalid node was read");
                        depth++;
                        break;
                    }
                Debug.Assert(depth < 100);
            }

            return root;
        }



    }

    public class Usage
    {
        public int Invalid { get; set; }
        public int Valid { get; set; }

        public override string ToString()
        {
            return string.Format("Valid : {0}, Invalid : {1}", Valid, Invalid);
        }
    }
    
}
