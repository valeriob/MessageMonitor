using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        public Queue<long> Empty_Slots { get; set; }
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }

        public List<Node> Pending_Nodes { get; set; }
        public Dictionary<long,Node> Cached_Nodes { get; set; }

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


        protected void Free_Address(long address)
        {
            if(address != 0)
                Freed_Empty_Slots.Add(address);
        }


        protected bool Should_Reuse_Old_Addresses()
        {
            return false;
            var size = Node.Size_In_Bytes(Size);
            //var interval = Get_Intervall((current_Hight + 1) * size * 2 + 1, size);
            //return interval.HasValue && interval.
            return Contiguos_Space(size) > ((current_Depth + 1 ) * size * 2 + 1);
            return Empty_Slots.Any();
        }

        public long Contiguos_Space(int blockSize)
        {
            if (Empty_Slots.Count == 0)
                return 0;

            var space = Empty_Slots.ToArray();
            long last = space[0];

            for (int i = 1; i < space.Length; i++)
            {
                if (last != 0 && last + blockSize != space[i])
                    break;
                last = space[i];
            }
            return last - space[0];
        }

        public struct Intervall { public long From; public long To; }
        public Intervall? Get_Intervall(int minSize, int blockSize)
        {
            var space = Empty_Slots.ToArray();
            long last = space[0];

            for (int i = 1; i < space.Length; i++)
            {
                if (space[i] - last >= minSize)
                { //found 
                    return new Intervall { From = last, To = space[i] };
                }
                else
                {
                    if (last + blockSize != space[i])
                    { 
                        // reset sequence
                        last = space[i];
                    }
                }
            }
            return null;
        }


        protected void Write_Node(Node node)
        {
            Free_Address(node.Address);
            if (Should_Reuse_Old_Addresses())
            {
                var address = Empty_Slots.Dequeue();
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

        protected void Write_Node(Node node, long address)
        {
            node.Address = address;

            Pending_Nodes.Add(node);


            if (_writeMemory_Count.ContainsKey(address))
                _writeMemory_Count[address] += 1;
            else
                _writeMemory_Count[address] = 1;
        }

        protected Node Read_Node(Node parent, long address)
        {
            //if (Cached_Nodes.ContainsKey(address))
            //    return Cached_Nodes[address];

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
