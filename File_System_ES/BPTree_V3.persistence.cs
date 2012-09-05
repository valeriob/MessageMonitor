using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
{
    public partial class BPlusTree<T>
    {
        private long _index_Pointer;
        private long Index_Pointer()
        {
            return _index_Pointer;
        }

        private long _data_Pointer;
        private long Data_Pointer()
        {
            return _data_Pointer;
        }

        BinaryFormatter serializer = new BinaryFormatter();

 

        protected void Write_Node(Node node)
        {
            var address = Index_Pointer();
            Write_Node(node, address);
            _index_Pointer += Node.Size_In_Bytes(Size);
        }
        protected void Update_Node(Node node)
        {
            Write_Node(node, node.Address);
        }


        protected void Write_Node(Node node, long address)
        {
            Index_Stream.Seek(address, SeekOrigin.Begin);

             var bytes = node.To_Bytes();
             Index_Stream.Write(bytes, 0, bytes.Length);
             Index_Stream.Flush();

             if (_writeMemory_Count.ContainsKey(address))
                 _writeMemory_Count[address] += 1;
             else
                 _writeMemory_Count[address] = 1;

            node.Address = address;
        }


        protected void Write_Data<V>(V value, long address)
        {
            Data_Stream.Seek(address, SeekOrigin.Begin);

            if (typeof(V) != typeof(string))
                throw new NotSupportedException("");

            var bytes = Encoding.UTF8.GetBytes(value as string);
            Data_Stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            Data_Stream.Write(bytes, 0, bytes.Length);

            _data_Pointer += bytes.Length + 4;
        }

        protected Node Read_Node(Node parent, long address)
        {
            Index_Stream.Seek(address, SeekOrigin.Begin);

            var buffer = new byte[Node.Size_In_Bytes(Size)];

            Index_Stream.Read(buffer, 0, buffer.Length);

            if (_readMemory_Count.ContainsKey(address))
                _readMemory_Count[address] += 1;
            else
                _readMemory_Count[address] = 1;

            var node = Node.From_Bytes(buffer, Size);
            node.Parent = parent;
            node.Address = address;
            return node;
        }

        protected V Read_Data<V>(long address) where V: class
        {
            Data_Stream.Seek(address, SeekOrigin.Begin);

            if (typeof(V) != typeof(string))
                throw new NotSupportedException("");

            var buffer = new byte[4];
            Data_Stream.Read(buffer, 0, 4);

            int lenght = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[lenght];
            Data_Stream.Read(buffer, 0, lenght);

            object obj = Encoding.UTF8.GetString(buffer);
            return obj as V;
        }


    }
    
}
