﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES
{
    // https://github.com/gugugupan/B-plus-tree/blob/master/bpt.cpp
    public partial class BPlusTree<T>
    {
        public Node Root { get; set; }
        public Node UncommittedRoot { get; set; }

        public Stream Stream { get; set; }
        public int Size { get; set; }

        public BPlusTree(Stream stream)
        {
            Size = 3;
            Stream = stream;
            Init();
        }


        public void Commit()
        {
            Update_Node(UncommittedRoot);
        }

        private void Init()
        {
            Node root = null;
            try 
            {
                root = Read_Node(null, 0);
            }
            catch (Exception) 
            {
                root = Node.Create_New(Size, true);
                Write_Node(root, 0);
            }
        }
      
   

        public T Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, T value)
        {
            var leaf = Find_Leaf_Node(key);

            var data_Address = Current_Pointer();
            Write_Object(value, data_Address);


            Insert_in_node(leaf, key, data_Address);

            //if (leaf.Has_Empty_Slot())
            //{
            //    leaf.Insert_Value_Address(key, data_Address);

            //    Update_Node(leaf);
            //}
            //else
            //{
            //    var split = leaf.Split_Node(key, data_Address);

            //    Write_Node(split.First);
            //    Write_Node(split.Second);
            //    R_Put(split);

            //    if (leaf.Parent.Has_Empty_Slot())
            //    {
            //        var newLeaf = Node.Create_New(Size, true);
            //        newLeaf.Insert_Value_Address(key, data_Address);
            //        Write_Node(newLeaf);

            //        // TODO split ?!?!?
            //        leaf.Parent.Insert_Child_Address(key, newLeaf.Address);

            //        Update_Node(leaf.Parent);
            //    }
            //}
        }

        protected void Insert_in_node(Node node, int key, long address)
        {

            if (true) // node needs split
                Split(node);
        }

        protected void Split(Node node)
        {
            var newNode = Node.Create_New(Size, node.IsLeaf);
            var mid_Key = node.Children[Size / 2].Key;
            // copia dei valori.

            if (node.IsLeaf)
            {
                mid_Key = node.Children[Size / 2 + 1].Key;
            }
            if (node.Parent == null)
            {
                //var parent = Node.Create_New(Size, false);
                //parent.First_Child = split.First.Address;
                //parent.Insert_Child_Address(split.Second.Children[0].Key, split.Second.Address);

                //Write_Node(parent);
                //UncommittedRoot = parent;
            }
            else 
            {
                node.Parent = newNode.Parent;
                Insert_in_node(node.Parent, mid_Key, newNode.Address);
            }
        }


        protected Node Find_Leaf_Node(int key)
        {
            Node node = Root;
            if (UncommittedRoot != null)
                node = UncommittedRoot;

            while (node != null && !node.IsLeaf)
            {
                var address = node.Get_Child_Node_Address(key);
                node = Read_Node(node, address);
            }
            return node;
        }
    }
    
 

}
