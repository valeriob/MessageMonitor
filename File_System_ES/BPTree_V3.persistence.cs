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
    
   

        private long _currentPointer;
        private long Current_Pointer()
        {
            return _currentPointer;
        }
        BinaryFormatter serializer = new BinaryFormatter();

 

        protected void Write_Node(Node node)
        {
            var address = Current_Pointer();
            Write_Node(node, address);
            _currentPointer += Node.Size_In_Bytes(Size);
        }
        protected void Update_Node(Node node)
        {
            Write_Node(node, node.Address);
        }


        protected void Write_Node(Node node, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);

             var bytes = node.To_Bytes();
             Stream.Write(bytes, 0, bytes.Length);
             Stream.Flush();

             if (_writeMemory_Count.ContainsKey(address))
                 _writeMemory_Count[address] += 1;
             else
                 _writeMemory_Count[address] = 1;

            node.Address = address;
        }


        protected void Write_Object<V>(V value, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);

            if (typeof(V) != typeof(string))
                throw new NotSupportedException("");

            var bytes = Encoding.UTF8.GetBytes(value as string);
            Stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            Stream.Write(bytes, 0, bytes.Length);
            Stream.Flush();

            _currentPointer += bytes.Length + 4;
        }

        protected Node Read_Node(Node parent, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);

            var buffer = new byte[Node.Size_In_Bytes(Size)];

            Stream.Read(buffer, 0, buffer.Length);

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
            Stream.Seek(address, SeekOrigin.Begin);

            if (typeof(V) != typeof(string))
                throw new NotSupportedException("");

            var buffer = new byte[4];
            Stream.Read(buffer, 0, 4);

            int lenght = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[lenght];
            Stream.Read(buffer, 0, lenght);

            object obj = Encoding.UTF8.GetString(buffer);
            return obj as V;
        }


    }
    
}
