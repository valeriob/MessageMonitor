using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
{
    [Serializable]
    public class Node : ISerializable
    {
        //public long First_Child { get; set; }
        public bool IsLeaf { get; set; }
       // public File_Address[] Children { get; set; }
        public int[] Keys { get; set; }
        public long[] Pointers { get; set; }
        public int Key_Num { get; set; }

        public Node Parent { get; set; }
        public long Address { get; set; }


        protected Node(int size, bool isLeaf) 
        {
            IsLeaf = isLeaf;
           //Children = new File_Address[size];
            Pointers = new long[size +1];
            Keys = new int[size];

            for (int i = 0; i < size; i++)
                Keys[i] = int.MinValue;

            for (int i = 0; i < size +1; i++)
                Pointers[i] = int.MinValue;
        }


        public Node(bool isLeaf, int[] keys, long[] pointers, int key_num)
        {
            IsLeaf = isLeaf;
            Keys = keys;
            Pointers = pointers;
            Key_Num = key_num;
        }
        protected Node(SerializationInfo info, StreamingContext ctx) 
        {
            IsLeaf= info.GetBoolean("isLeaf");
            Keys = (int[])info.GetValue("keys", typeof(int[]));
            Pointers = (long[])info.GetValue("pointers", typeof(long[]));
            Key_Num = info.GetInt32("key_num");
        }

        //public long Get_Child_Node_Address(int key)
        //{
        //    if (IsLeaf)
        //        throw new InvalidOperationException("Leafs do not have children");

        //    if (!Children[0].IsValid)
        //        return First_Child;


        //    if(key > Children.Where(v=> v.IsValid).Last().Key)
        //        return Children.Where(v => v.IsValid).Last().Address;

        //    for (int i = Children.Length-1; i >= 0; i--)
        //    {
        //        var child = Children[i];
        //        if ( child.IsValid && key >= child.Key)
        //            return child.Address;
        //    }

        //    return First_Child;
        //}








        public long Get_Data_Address(int key)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (Keys[i] == key)
                    return Pointers[i+1];

            throw new Exception("Key " + key + " not found !");
        }



        public override string ToString()
        {
            var keys = "";
            for (int i = 0; i < Key_Num; i++)
                keys += Keys[i] + ", ";
            keys = keys.TrimEnd(' ', ',');

            string root = Parent == null ? "(Root)" : "";
            if (IsLeaf)
                return string.Format("{2} {1} Leaf : {0}", keys, root, Address);
            else
                return string.Format("{2} {1} Node : {0}", keys, root, Address);
        }


        public static Node Create_New(int size, bool isLeaf)
        { 
            return new Node(size, isLeaf);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            info.AddValue("isLeaf", this.IsLeaf);
            info.AddValue("keys", this.Keys);
            info.AddValue("pointers", this.Pointers);
            info.AddValue("key_num", this.Key_Num);
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
