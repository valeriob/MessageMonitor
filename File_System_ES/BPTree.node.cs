using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES
{

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

            int asd = Math.Max(Children.Length / 2 + 1, Children.Length);
            asd = Children.Length / 2 + 1;

            for (int i = 0; i < asd; i++)
                result.First.Children[i] = temp[i];

            for (int i = asd; i < temp.Length; i++)
                result.Second.Children[i - asd] = temp[i];

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
