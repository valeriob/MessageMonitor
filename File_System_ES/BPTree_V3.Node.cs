using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
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


        public Node Clone()
        {
            var node = Create_New(Keys.Length, IsLeaf);
            node.Key_Num = Key_Num;
            node.Keys = Keys;
            node.Pointers = Pointers;
            node.Versions = Versions;
            node.Parent = Parent;
            node.Address = Address;
            return node;
        }

        public byte[] To_Bytes()
        {
            var size = Size_In_Bytes(Keys.Length);

            var buffer = new byte[size];

            Array.Copy(BitConverter.GetBytes(Key_Num), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(IsLeaf), 0, buffer, 4, 1);


            for (int i = 0; i < Key_Num; i++)
                Array.Copy(BitConverter.GetBytes(Keys[i]), 0, buffer,  5 + 4 * i, 4);

            for (int i = 0; i < Key_Num +1; i++)
                Array.Copy(BitConverter.GetBytes(Pointers[i]), 0, buffer,  5 + 4 * Keys.Length + i * 8, 8);

            return buffer;
        }

        public static int Size_In_Bytes(int size)
        {
            return 4 + 1 + 4 * size + 8 * (size + 1);
        }

        public static Node From_Bytes(byte[] buffer, int size)
        {
            var byteCount = Size_In_Bytes(size);

            var node = Node.Create_New(size, BitConverter.ToBoolean(buffer, 4));
            node.Key_Num = BitConverter.ToInt32(buffer, 0);

            for (int i = 0; i < node.Key_Num; i++)
                node.Keys[i] = BitConverter.ToInt32(buffer, 5 + 4 * i);


            for (int i = 0; i < node.Key_Num + 1; i++)
                node.Pointers[i] = BitConverter.ToInt64(buffer, 5 + 4 * size + 8 * i);

            return node;
        }
    
    }

    public class Data
    {
        public int Key { get; set; }
        public DateTime Timestamp { get; set; }
        public int Version { get; set; }
        public byte[] Payload { get; set; }


        public byte[] To_Bytes()
        {
            var size = 4 + 8 + 4 +4+ Payload.Length;

            var buffer = new byte[size];

            Array.Copy(BitConverter.GetBytes(size), 0, buffer, 0, 4);

            Array.Copy(BitConverter.GetBytes(Key), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(Timestamp.Ticks), 0, buffer, 8, 8);

            Array.Copy(BitConverter.GetBytes(Version), 0, buffer, 16, 4);

            Array.Copy(Payload, 0, buffer, 20, Payload.Length);

            return buffer;
        }

        public static Data From_Bytes(Stream stream)
        {
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);

            int lenght = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[lenght];
            stream.Read(buffer, 0, lenght);

            return From_Bytes(buffer, lenght);
        }

        public static Data From_Bytes(byte[] buffer, int totalLenght)
        {
            var data = new Data
            {
                Key = BitConverter.ToInt32(buffer, 0),
                Timestamp = DateTime.FromBinary(BitConverter.ToInt64(buffer, 4)),
                Version = BitConverter.ToInt32(buffer, 12),
                Payload = new byte[totalLenght - 20]
            };

            Array.Copy(buffer, 16, data.Payload, 0, totalLenght - 20);

            return data;
        }


    }
}
