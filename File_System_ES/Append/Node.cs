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
    public class Node 
    {
        public bool IsLeaf { get; set; }

        public int[] Keys { get; set; }
        public long[] Pointers { get; set; }
        public int Key_Num { get; set; }
        public int[] Versions { get; set; }

        public Node Parent { get; set; }
        public long Address { get; set; }


        protected Node(int size, bool isLeaf) 
        {
            IsLeaf = isLeaf;
            Pointers = new long[size + 1];
            Keys = new int[size];
            Versions = new int[size];
        }


        public void Insert_Key(int key, long address)
        {
            int x = 0;
            while (x < Key_Num && Keys[x] < key) x++;

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

        public Split Split()
        {
            int size = Keys.Length;

            var node_Left = Create_New_One_Like_This();
            var node_Right = Node.Create_New(Keys.Length, node_Left.IsLeaf);
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

            return new Split { Node_Left= node_Left,  Node_Right = node_Right, Mid_Key = mid_Key };
        }



        public long Get_Data_Address(int key)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (Keys[i] == key)
                    return Pointers[i+1];

            throw new Exception("Key " + key + " not found !");
        }

        public bool IsValid
        {
            get { return Key_Num > 0; } 
        }


        public override string ToString()
        {
            if(Key_Num <= 0)
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




        public Node Create_New_One_Like_This()
        {
            var node = Create_New(Keys.Length, IsLeaf);
            node.Key_Num = Key_Num;
            Array.Copy(Keys, node.Keys, Keys.Length);
            Array.Copy(Pointers, node.Pointers, Pointers.Length);
            Array.Copy(Versions, node.Versions, Versions.Length);
            node.Parent = Parent;
            node.Address = Address;
            return node;
        }

        public void To_Bytes_In_Buffer(byte[] buffer, int startIndex)
        {
            int key_Num = Key_Num;

            Array.Copy(BitConverter.GetBytes(key_Num), 0, buffer, startIndex, 4);
            buffer[startIndex + 4] = IsLeaf ? (byte)1 : (byte)0;

            int offset = startIndex + 5;
            for (int i = 0; i < key_Num; i++)
                Array.Copy(BitConverter.GetBytes(Keys[i]), 0, buffer, offset + 4 * i, 4);

            offset = startIndex + 5 + 4 * Keys.Length;
            for (int i = 0; i < key_Num + 1; i++)
                Array.Copy(BitConverter.GetBytes(Pointers[i]), 0, buffer, offset + i * 8, 8);
        }

        public void To_Bytes_Explicit(byte[] buffer, int startIndex)
        {
            int key_Num = Key_Num;

            Array.Copy(BitConverter.GetBytes(key_Num), 0, buffer, startIndex, 4);
            buffer[startIndex + 4] = IsLeaf ? (byte)1 : (byte)0;

            int offset = startIndex + 5;
            for (int i = 0; i < key_Num; i++)
            {
                var tmp = new Bytes_To_Int { integer = Keys[i] };
                buffer[offset + i * 8] = tmp.byte0;
                buffer[offset + i * 8 + 1] = tmp.byte1;
                buffer[offset + i * 8 + 2] = tmp.byte2;
                buffer[offset + i * 8 + 3] = tmp.byte3;
            }
                
            offset = startIndex + 5 + 4 * Keys.Length;

            for (int i = 0; i < key_Num + 1; i++)
            {
                var tmp = new Bytes_To_Long { longint = Pointers[i]  };
                buffer[offset + i * 8] = tmp.byte0;
                buffer[offset + i * 8 + 1] = tmp.byte1;
                buffer[offset + i * 8 + 2] = tmp.byte2;
                buffer[offset + i * 8 + 3] = tmp.byte3;
                buffer[offset + i * 8 + 4] = tmp.byte4;
                buffer[offset + i * 8 + 5] = tmp.byte5;
                buffer[offset + i * 8 + 6] = tmp.byte6;
                buffer[offset + i * 8 + 7] = tmp.byte7;
            }
        }

        public byte[] To_Bytes()
        {
            var size = Size_In_Bytes(Keys.Length);

            var buffer = new byte[size];

            To_Bytes_In_Buffer(buffer, 0);

            return buffer;
        }


        public static Node Create_New(int size, bool isLeaf)
        {
            return new Node(size, isLeaf);
        }

        public static int Size_In_Bytes(int size)
        {
            return 4 + 1 + 4 * size + 8 * (size + 1);
        }

        public static Node From_Bytes(byte[] buffer, int size)
        {
            var byteCount = Size_In_Bytes(size);

            var node = Node.Create_New(size, BitConverter.ToBoolean(buffer, 4));
            var key_Num = BitConverter.ToInt32(buffer, 0);

            node.Key_Num = key_Num;

            for (int i = 0; i < key_Num; i++)
                node.Keys[i] = BitConverter.ToInt32(buffer, 5 + 4 * i);

            int offset = 5 + 4 * size;
            for (int i = 0; i < key_Num + 1; i++)
                node.Pointers[i] = BitConverter.ToInt64(buffer, offset + 8 * i);

            return node;
        }
    
    }

    public class Split
    {
        public Node Node_Left { get; set; }
        public Node Node_Right { get; set; }
        public int Mid_Key { get; set; }
    }



    [StructLayout(LayoutKind.Explicit)]
    struct Bytes_To_Int
    {
        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;

        [FieldOffset(0)]
        public int integer;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct Bytes_To_Long
    {
        [FieldOffset(0)]
        public byte byte0;
        [FieldOffset(1)]
        public byte byte1;
        [FieldOffset(2)]
        public byte byte2;
        [FieldOffset(3)]
        public byte byte3;

        [FieldOffset(4)]
        public byte byte4;
        [FieldOffset(5)]
        public byte byte5;
        [FieldOffset(6)]
        public byte byte6;
        [FieldOffset(7)]
        public byte byte7;

        [FieldOffset(0)]
        public long longint;
    }
}
