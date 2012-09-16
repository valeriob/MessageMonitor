﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree<T> : IBPlusTree<T> where T: IComparable<T>, IEquatable<T>
    {
        public Node<T> Root { get; protected set; }
        Pending_Changes<T> Pending_Changes;

        protected Stream Index_Stream { get; set; }
        protected Stream Data_Stream { get; set; }
        protected Stream Metadata_Stream { get; set; }
        
        protected int Size { get; set; }

        public List<Block_Group> Empty_Slots = new List<Block_Group>();
        public Dictionary<long, Node<T>> Cached_Nodes { get; set; }

        public int Block_Size { get; protected set; }
        ISerializer<T> Serializer;
        Node_Factory<T> Node_Factory;
        Cache_LRU<long, Node<T>> Cache;

        public BPlusTree(Stream metadataStream, Stream indexStream, Stream dataStream, int order, ISerializer<T> serializer)
        {
            Size = order;

            Serializer = serializer;
            Node_Factory = new Node_Factory<T>(serializer);
            Cache = new Cache_LRU<long, Node<T>>((k) => Read_Node(k));
            Block_Size = Node_Factory.Size_In_Bytes(Size);

            Index_Stream = indexStream;
            Data_Stream = dataStream;
            Metadata_Stream = metadataStream;

            Cached_Nodes = new Dictionary<long, Node<T>>();

            _index_Pointer = Math.Max(8, indexStream.Length);
            _data_Pointer = Data_Stream.Length;

            Pending_Changes = new Pending_Changes<T>(Index_Stream, Block_Size, _index_Pointer, Empty_Slots, Node_Factory);
            Init();
        }

        public void Commit()
        {
            foreach (var address in Pending_Changes.Freed_Empty_Slots)
                Cache.Invalidate(address);

            var newRoot = Pending_Changes.Commit(Index_Stream);

            writes++;
            Metadata_Stream.Seek(0, SeekOrigin.Begin);
            Metadata_Stream.Write(BitConverter.GetBytes(newRoot.Address), 0, 8);
            Metadata_Stream.Flush();

            //Pending_Changes.Add_Block_Address_To_Available_Space();

            //foreach (var address in Pending_Changes.Freed_Empty_Slots)
            //{
            //    Index_Stream.Seek(address, SeekOrigin.Begin);
            //    Index_Stream.Write(BitConverter.GetBytes(-1), 0, 4);
            //}
            //var usage = Count_Empty_Slots();

            Root = newRoot;

            Pending_Changes.Clean_Root();
            // TODO persist empty pages on metadata
            //Cached_Nodes.Clear();
            //foreach (var node in Pending_Changes.Last_Cached_Nodes())
            //    Cached_Nodes[node.Address] = node;

            _index_Pointer = Pending_Changes.Get_Index_Pointer();
            //Empty_Slots = Pending_Changes.Get_Empty_Slots();

            //Pending_Changes = null;

            Index_Stream.Flush();
        }

        public void RollBack()
        {
            Index_Stream.SetLength(_index_Pointer);
            Cached_Nodes.Clear();
            //Pending_Changes = null;
        }

        private void Init()
        {
            try
            {
                var buffer = new byte[8];
                Metadata_Stream.Seek(0, SeekOrigin.Begin);
                Metadata_Stream.Read(buffer, 0, 8);
                long root_Address = BitConverter.ToInt64(buffer, 0);

                Root = Read_Root(root_Address);
                if(Root.IsValid)
                    return;
            }
            catch (Exception) { }

            var root = Node_Factory.Create_New(Size, true);
            Write_Node(root);
            Pending_Changes.Append_New_Root(root);
        }


        public byte[] Get(T key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(T key, byte[] value)
        {
            var leaf = Find_Leaf_Node(key);

            var data_Address = Data_Pointer();
            Write_Data(value, key, 1);

            Node<T> newRoot = null;
            for(int i=0; i< leaf.Key_Num; i++)
                if (key.Equals(leaf.Keys[i]))
                {
                    var newLeaf = Node_Factory.Create_New_One_Like_This(leaf);
                    newLeaf.Versions[i]++;
                    newLeaf.Pointers[i] = data_Address;

                    Write_Data(value, key, newLeaf.Versions[i]); 

                    //newRoot = Clone_Anchestors_Of(newLeaf);

                    Pending_Changes.Append_New_Root(newRoot);
                    return;
                }

            newRoot = Insert_in_node(leaf, key, data_Address);

            Pending_Changes.Append_New_Root(newRoot);
        }

        public bool Delete(T key)
        {
            var leaf = Find_Leaf_Node(key);
            for (int i = 0; i < leaf.Key_Num; i++)
                if (key.Equals(leaf.Keys[i]))
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



        protected void Delete_Key_In_Node(Node<T> node, T key)
        {
            int x = 0;
            while (!key.Equals(node.Keys[x])) 
                x++;
            for (int i = x; i < node.Key_Num - 1; i++)
                node.Keys[i] = node.Keys[i + 1];
            for (int i = x + 1; i < node.Key_Num; i++)
                node.Pointers[i] = node.Pointers[i + 1];
            node.Key_Num--;
        }

        protected Node<T> Insert_in_node(Node<T> node, T key, long address, Node<T> child = null)
        {
            Renew_Node_And_Dispose_Space(node);
            node.Insert_Key(key, address, child);

            Node<T> newRoot = null;
            if (node.Needs_To_Be_Splitted())
            {
                var split = node.Split(Node_Factory);

                // Fix parent relation for moved children
                //foreach (var descendant in split.Node_Right.Children.Where(c => c != null))
                //    descendant.Parent = split.Node_Right;

                Write_Node(split.Node_Right);
                //Write_Node(split.Node_Left);

                if (node.Parent == null) // if i'm splitting the root, i need a new up level
                {
                    var root = Node_Factory.Create_New(Size, false);
                    root.Keys[0] = split.Mid_Key;
                    root.Pointers[0] = split.Node_Left.Address;
                    root.Pointers[1] = split.Node_Right.Address;

                    root.Children[0] = split.Node_Left;
                    root.Children[1] = split.Node_Right;

                    root.Key_Num = 1;
                    split.Node_Left.Parent = root;
                    Write_Node(root);

                    split.Node_Left.Parent = split.Node_Right.Parent = root;
                    newRoot = root;
                }
                else
                {
                    newRoot = Insert_in_node(split.Node_Left.Parent, split.Mid_Key, 0, split.Node_Right);
                }
            }
            else
            {
                //child.Parent = node;
                Write_Node(node);

                while (node.Parent != null)
                {
                    node = node.Parent;
                    Renew_Node_And_Dispose_Space(node);
                    //node.Address = 0;
                    //node.Is_Volatile = true;
                }

                newRoot = node;
            }

            return newRoot;
        }

        public Node<T> Clone_Tree_From_Root(Node<T> node)
        {
            var newNode = Node_Factory.Create_New_One_Like_This(node);
            newNode.Address = 0;

            for(int i=0; i < newNode.Key_Num +1; i++)
            {
                if (newNode.Children[i] == null)
                    continue;
                newNode.Children[i] = Clone_Tree_From_Root(newNode.Children[i]);
            }
            return newNode;
        }

        protected Node<T> Find_Leaf_Node(T key)
        {
            var node = Root;
            if(Pending_Changes.Has_Pending_Changes())
                node = Pending_Changes.Get_Uncommitted_Root();

            return Find_Leaf_Node(key, node);
        }

        

        protected Node<T> Find_Leaf_Node(T key, Node<T> root)
        {
            int depth = 0;
            while (!root.IsLeaf)
            {
                for (int i = 0; i <= root.Key_Num; i++)
                    if (i == root.Key_Num || key.CompareTo(root.Keys[i])< 0)
                    {
                        if (root.Children[i] != null)
                            root = root.Children[i];
                        else
                            root = Read_Node_From_Pointer(root, i);

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
