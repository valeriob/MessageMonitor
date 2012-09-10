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
        //public List<Node> Pending_Nodes { get; set; }

        private long _index_Pointer;
        //private long Index_Pointer()
        //{
        //    return _index_Pointer;
        //}

        private long _data_Pointer;
        private long Data_Pointer()
        {
            return _data_Pointer;
        }
       // private long _committed_Index_Pointer;




  

        protected void Write_Node(Node node)
        {
            Pending_Changes.Free_Address(node.Address);
            Pending_Changes.Append_Node(node);
        }
        
        protected Node Read_Node(Node parent, long address)
        {
            if (Pending_Changes != null)
            {
                var cachedNode = Pending_Changes.Last_Cached_Nodes().SingleOrDefault(n => n.Address == address);
                if (cachedNode != null)
                    return cachedNode;
            }
            else
                if (Cached_Nodes.ContainsKey(address))
                {
                    cache_hits++;
                    return Cached_Nodes[address];
                }

            cache_misses++;

            Index_Stream.Seek(address, SeekOrigin.Begin);

            var buffer = new byte[Block_Size];

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
