using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        public List<Node> Pending_Nodes { get; set; }

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
        private long _committed_Index_Pointer;




  

        protected void Write_Node(Node node)
        {
            //Debug.Assert(node.Address != 0);

            Free_Address(node.Address);
            Pending_Nodes.Add(node);
            //if (Should_Reuse_Old_Addresses())
            //{
            //    var address = Empty_Slots.Dequeue();
            //    Reserved_Empty_Slots.Add(address);
            //    Write_Node(node, address);
            //}
            //else
            //{
            //    var address = Index_Pointer();
            //    Write_Node(node, address);
            //    _index_Pointer += Node.Size_In_Bytes(Size);
            //}
        }

        //protected void Write_Node(Node node, long address)
        //{
        //    //node.Address = address;

        //    Pending_Nodes.Add(node);


        //    if (_writeMemory_Count.ContainsKey(address))
        //        _writeMemory_Count[address] += 1;
        //    else
        //        _writeMemory_Count[address] = 1;
        //}

        protected Node Read_Node(Node parent, long address)
        {
            //if (Cached_Nodes.ContainsKey(address))
            //{
            //    var cached = Cached_Nodes[address];
            //    return cached;
            //}

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

        protected void Write_Data(byte[] value, int key, int version)
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

        protected byte[] Read_Data(long address)
        {
            Data_Stream.Seek(address, SeekOrigin.Begin);

            var data = Data.From_Bytes(Data_Stream);
            return data.Payload;
        }


       
    }


}
