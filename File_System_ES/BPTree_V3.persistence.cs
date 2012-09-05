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
        public Queue<long> EmptySlots { get; set; }
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }

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
            if (EmptySlots.Any())
            {
                var address = EmptySlots.Dequeue();
                Reserved_Empty_Slots.Add(address);
                Write_Node(node, address);
            }
            else
            {
                var address = Index_Pointer();
                Write_Node(node, address);
                _index_Pointer += Node.Size_In_Bytes(Size);
            }
        }

        [Obsolete("Just append new node")]
        protected void Update_Node(Node node)
        {
            Write_Node(node, node.Address);
        }

        protected void Delete_Node(Node node)
        { 
            
        }


        protected void Write_Node(Node node, long address)
        {
            node.Address = address;

            // TODO Enqueue operation
            Index_Stream.Seek(address, SeekOrigin.Begin);

            var bytes = node.To_Bytes();
            Index_Stream.Write(bytes, 0, bytes.Length);
            Index_Stream.Flush();


            if (_writeMemory_Count.ContainsKey(address))
                _writeMemory_Count[address] += 1;
            else
                _writeMemory_Count[address] = 1;
        }


        protected void Write_Data(string value, int key, int version)
        {
            var data = new Data 
            { 
                Key = key, 
                Version = version, 
                Payload= value, 
                Timestamp = DateTime.Now
            };

            var address = Data_Pointer();
            Data_Stream.Seek(address, SeekOrigin.Begin);

            var bytes = data.To_Bytes();
            Data_Stream.Write(bytes, 0, bytes.Length);

            _data_Pointer += bytes.Length;
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

        protected string Read_Data(long address) 
        {
            Data_Stream.Seek(address, SeekOrigin.Begin);

            var data = Data.From_Bytes(Data_Stream);
            return data.Payload;
        }


    }
    
}
