using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES
{
    public class BPlusTree<T>
    {
        public Node Root { get; set; }
        public Node UncommittedRoot { get; set; }

        public Stream Stream { get; set; }
        public int Size { get; set; }

        public BPlusTree(Stream stream)
        {
            Size = 2;
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
                root = Node.Create_New(Size, false);
                //Write_Node(root, 0);
            }
        }
        private long _currentPointer;
        private long Current_Pointer()
        {
            return _currentPointer;
        }
   

        public T Get(int key)
        {
            var leaf = Find_Leaf_Node(key);
            var data = Read_Data(leaf.Get_Data_Address(key));
            return data;
        }

        public void Put(int key, T value)
        {
            //if (Root == null)
            //{
            //    First_Put2(key, value);
            //    return;
            //}

            var leaf = Find_Leaf_Node(key);
            if (leaf == null)
            {
                First_Put2(key, value);
                return;
            }

            var data_Address = Current_Pointer();
            Write_Object(value, data_Address);

            if (leaf.Has_Empty_Slot())
            {
                leaf.Insert_Value_Address(key, data_Address);

                Update_Node(leaf);

                return;
            }
            else
            {
                var split = leaf.Split_Node(key, data_Address);
                //if (key > split.Second.Children[0].Key)
                //    split.Second.Insert_Value_Address(key, data_Address);
                //else
                //    split.First.Insert_Value_Address(key, data_Address);
                Write_Node(split.First);
                Write_Node(split.Second);
                R_Put(split);

                //if (leaf.Parent.Has_Empty_Slot())
                //{
                //    var newLeaf = Node.Create_New(Size, true);
                //    newLeaf.Insert_Value_Address(key, data_Address);
                //    Write_Node(newLeaf);

                //    // TODO split ?!?!?
                //    leaf.Parent.Insert_Child_Address(key, newLeaf.Address);

                //    Update_Node(leaf.Parent);
                //}
            }
        }

        protected void R_Put(Split_Result split)
        {
            if (split.Parent == null)
            {
                var parent = Node.Create_New(Size, false);
                parent.First_Child = split.First.Address;
                parent.Insert_Child_Address(split.Second.Children[0].Key, split.Second.Address);

                Write_Node(parent);
                UncommittedRoot = parent;
            }
            else
            {
                if (split.Parent.Has_Empty_Slot())
                {

                }
                else
                {
                    var parentSplit = split.Parent.Split_Node(0, 0);
                    Write_Node(parentSplit.First);
                    Write_Node(parentSplit.Second);

                    R_Put(parentSplit);
                }
            }
           
        }

        protected void First_Put2(int key, T value)
        {
            var node = Node.Create_New(Size, true);

            Write_Node(node);
            node.Insert_Value_Address(key, Current_Pointer());

            Write_Object(value, Current_Pointer());

            Update_Node(node);

            UncommittedRoot = node;
        }
        protected void First_Put(int key, T value)
        {
            Root = Read_Node(null, 0);

            Write_Object(value, Current_Pointer());

            var leaf = Node.Create_New(Size, true);
            leaf.Children[0].IsValid = true;
            leaf.Children[0].Address = Current_Pointer();
            leaf.Children[0].Key = key;

            Write_Object(value, Current_Pointer());

            // todo addchild
            Root.First_Child = Current_Pointer();

            Write_Node(leaf);
            Update_Node(Root);
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


        BinaryFormatter serializer = new BinaryFormatter();

        protected Node Read_Node(Node parent, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            var obj = serializer.Deserialize(Stream);
            var node = obj as Node;
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
    }
    
    [Serializable]
    public class Node 
    {
        public long First_Child { get; set; }
        public bool IsLeaf { get; set; }
        public File_Address[] Children { get; set; }
        
        public Node Parent { get; set; }
        public long Address { get; set; }

        protected Node(int size, bool isLeaf) 
        {
            if (size % 2 != 0)
                throw new Exception("Size must be power of 2");

            IsLeaf = isLeaf;
            Children = new File_Address[size];
        }


        public long Get_Child_Node_Address(int key)
        {
            if (IsLeaf)
                throw new InvalidOperationException("Leafs do not have children");

            if (!Children[0].IsValid)
                return First_Child;


            //int i = Children.Length - 1;
            //while (!Children[i].IsValid || (i >= 0 && key < Children[i].Key))
            //    i--;
            //return Children[i].Address;

            if(key > Children.Where(v=> v.IsValid).Last().Key)
                return Children.Where(v => v.IsValid).Last().Address;

            for (int i = Children.Length-1; i >= 0; i--)
            {
                var child = Children[i];
                if ( child.IsValid && key >= child.Key)
                    return child.Address;
            }

            return First_Child;
        }

        public long Get_Data_Address(int key)
        {
            for (int i = 0; i < Children.Length; i++)
                if (Children[i].Key == key)
                    return Children[i].Address;

            throw new Exception("Key "+ key+" not found !");
        }

        public bool Has_Empty_Slot()
        {
            return Children.Any(c => !c.IsValid);
        }

        public void Insert_Value_Address(int key, long address)
        {
            if (!IsLeaf)
                throw new Exception("Only Leafs che append data");

            int idx = 0;
            while (Children[idx].IsValid && Children[idx].Key < key && idx <= Children.Length -1)
                idx++;

            for (int i = Children.Length - 1; i > idx; i--)
            {
                Children[i] = Children[i - 1];
            }

            Children[idx] = new File_Address { IsValid = true, Key = key, Address = address};
        }

        public void Insert_Child_Address(int key, long address)
        {
            if (IsLeaf)
                throw new Exception("Only Nodes che append data");

            int idx = 0;
            while (Children[idx].IsValid && Children[idx].Key < key && idx <= Children.Length - 1)
                idx++;

            for (int i = Children.Length - 1; i > idx; i--)
            {
                Children[i] = Children[i - 1];
            }

            Children[idx] = new File_Address { IsValid = true, Key = key, Address = address };
        }


        public Split_Result Split_Node(int key, long address)
        {
            if (!Children.All(v => v.IsValid))
                throw new Exception("Node is not complete !");

            var result = new Split_Result
            {
                First = Create_New(Children.Length, IsLeaf),
                Second = Create_New(Children.Length, IsLeaf),
                Parent = Parent,
            };

            if (key > Children.Last().Key)
            {
                result.First = this;
                result.Second.Insert_Value_Address(key, address);
                return result;
            }

            var temp = new File_Address[Children.Length +1];
            Array.Copy(Children, temp, Children.Length);
            temp[Children.Length] = new File_Address { IsValid = true, Key = key, Address = address };
            Array.Sort(temp);

            int asd = Math.Min(temp.Length / 2, Children.Length);
          
            for (int i = 0; i < temp.Length / 2; i++)
                result.First.Children[i] = temp[i];

            for (int i = Children.Length / 2; i < temp.Length; i++)
                result.Second.Children[i - (Children.Length / 2)] = temp[i];

            return result;
        }

        //public Split_Result Split_Node(int key, long address)
        //{
        //    if (!Children.All(v=> v.IsValid))
        //        throw new Exception("Node is not complete !");

        //    var result = new Split_Result 
        //    {
        //        Second = Create_New(Children.Length, IsLeaf),
        //        Parent = Parent, 
        //    };
        //    if (Children.Last().Key < key)
        //        result.First = this;
        //    else
        //    {
        //        result.First = Create_New(Children.Length, IsLeaf);

        //        for (int i = 0; i < Children.Length / 2; i++)
        //            result.First.Children[i] = Children[i];

        //        result.Second.Children[0] = new File_Address { IsValid = true, Address = address, Key = key };

        //        for (int i = Children.Length / 2; i < Children.Length; i++)
        //            result.Second.Children[i - (Children.Length / 2)] = Children[i];
        //    }
        //    return result;
        //}

        public override string ToString()
        {
            var keys = string.Join(",", Children.Where(c => c.IsValid).Select(s => s.Key));
            if (IsLeaf)
                return string.Format("Leaf : {0}", keys);
            else
                return string.Format("Node : {0}", keys);
        }
        public static Node Create_New(int size, bool isLeaf)
        { 
            return new Node(size, isLeaf);
        }
    }


    [Serializable]
    public struct File_Address : IComparable<File_Address>
    {
        public bool IsValid { get; set; }

        public int Key { get; set; }
        public long Address { get; set; }

        public override string ToString()
        {
            if (!IsValid)
                return "-";
            else
                return string.Format("Key {0}, Address : {1}", Key, Address);
        }

        public int CompareTo(File_Address other)
        {
            return Comparer<int>.Default.Compare(Key, other.Key);
        }
    }


    public class Split_Result
    {
        public Node First { get; set; }
        public Node Second { get; set; }

        public Node Parent { get; set; }
        public int Key { get; set; }
    }

}
