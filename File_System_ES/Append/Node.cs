using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
{
    public class Node<T> : IDisposable where T: IComparable<T>, IEquatable<T>
    {
        public bool IsLeaf { get; set; }

        public T[] Keys { get; set; }
        public long[] Pointers { get; set; }
        public int Key_Num { get; set; }
        public int[] Versions { get; set; }

        public Node<T> Parent { get; set; }
        public long Address { get; set; }

        Node_Factory<T> _Factory;

        public void Dispose()
        {
            Key_Num = -2;
            Array.Clear(Keys, 0, Keys.Length);
            Array.Clear(Pointers, 0, Pointers.Length);
            Array.Clear(Versions, 0, Versions.Length);
            Address = 0;
            Parent = null;
            IsLeaf = false;
            _Factory.Return(this);
            GC.SuppressFinalize(this);
        }

        public Node(Node_Factory<T> factory, int size, bool isLeaf) 
        {
            _Factory = factory;
            IsLeaf = isLeaf;
            Pointers = new long[size + 1];
            Keys = new T[size];
            Versions = new int[size];
        }


        public void Insert_Key(T key, long address)
        {
            int x = 0;
            while (x < Key_Num && Keys[x].CompareTo(key) < 0)
                x++;

            for (int i = Key_Num; i > x; i--)
                Keys[i] = Keys[i - 1];

            for (int i = Key_Num + 1; i > x + 1; i--)
                Pointers[i] = Pointers[i - 1];

            Keys[x] = key;
            Pointers[x + 1] = address;
            Key_Num++;
        }

        public bool Needs_To_Be_Splitted()
        {
            return Key_Num == Keys.Length;
        }

        public void Update_Child_Address(long previousAddress, long newAddress)
        {
            for (int i = 0; i < Key_Num + 1; i++)
                if (Pointers[i] == previousAddress)
                {
                    Pointers[i] = newAddress;
                    return;
                }

            throw new Exception("this should not happen");
        }

        public Node_Split_Result<T> Split(Node_Factory<T> node_Factory)
        {
            int size = Keys.Length;

            var node_Left = node_Factory.Create_New_One_Like_This(this);
            var node_Right = node_Factory.Create_New(Keys.Length, node_Left.IsLeaf);

            node_Right.Parent = node_Left.Parent;
            var mid_Key = node_Left.Keys[size / 2];

            node_Right.Key_Num = size - size / 2 - 1;
            for (int i = 0; i < node_Right.Key_Num; i++)
            {
                node_Right.Keys[i] = node_Left.Keys[i + (size / 2 + 1)];
                node_Right.Pointers[i] = node_Left.Pointers[i + (size / 2 + 1)];
            }

            node_Right.Pointers[node_Right.Key_Num] = node_Left.Pointers[size]; 
            node_Left.Key_Num = size / 2;

            if (node_Left.IsLeaf)
            {
                node_Left.Key_Num++;
                node_Right.Pointers[0] = node_Left.Pointers[0];

                node_Left.Pointers[0] = node_Right.Address;  //TODO double linked list
                mid_Key = node_Left.Keys[size / 2 + 1];
            }

            return new Node_Split_Result<T> { Node_Left= node_Left,  Node_Right = node_Right, Mid_Key = mid_Key };
        }



        public long Get_Data_Address(T key)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (Keys[i].Equals(key))
                    return Pointers[i+1];

            throw new Exception("Key " + key + " not found !");
        }

        public bool IsValid
        {
            get { return Key_Num > 0; } 
        }

 
   

        public override string ToString()
        {
            if (Key_Num <= 0)
                return string.Format("{0} Invalid", Address);

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
    }

    public class Node_Split_Result<T> where T:IComparable<T>, IEquatable<T>
    {
        public Node<T> Node_Left { get; set; }
        public Node<T> Node_Right { get; set; }
        public T Mid_Key { get; set; }
    }

}
