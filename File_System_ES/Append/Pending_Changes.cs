using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public class Pending_Changes
    {
        public Stream Index_Stream { get; protected set; }
        public int Block_Size { get; protected set; }
        public Node Uncommitted_Root { get; protected set; }

        private long _index_Pointer;
        public long Get_Index_Pointer() { return _index_Pointer; }
        public List<Block_Group> Get_Empty_Slots() { return Empty_Slots; }

        public Pending_Changes(Stream index_Stream, int blockSize, long index_Pointer, List<Block_Group> emptySlots)
        {
            Index_Stream = index_Stream;
            Block_Size = blockSize;

            Freed_Empty_Slots = new List<long>();
            Pending_Nodes = new List<Node>();
            Nodes = new List<Node>();
            Empty_Slots = emptySlots; // TODO copy by value !

            _base_Address_Index = Empty_Slots.SelectMany(s => s.Blocks.Select(m => m.Value)).ToDictionary(d => d.Base_Address());
            _end_Address_Index = Empty_Slots.SelectMany(s => s.Blocks.Select(m => m.Value)).ToDictionary(d => d.End_Address());

            _index_Pointer = index_Pointer;
        }


        List<Block_Group> Empty_Slots;
        List<long> Freed_Empty_Slots;
        List<Node> Pending_Nodes;
        List<Node> Nodes;


        public Dictionary<long, Block> _base_Address_Index = new Dictionary<long, Block>();
        public Dictionary<long, Block> _end_Address_Index = new Dictionary<long, Block>();


        protected void Block_Usage_Finished(Block_Usage usage_Block)
        {
            var block = usage_Block.Block;

            var base_Address = block.Base_Address();
           
            Fix_Block_Position_In_Groups(block, block.Length, block.Length - usage_Block.Used_Length);

            _base_Address_Index.Remove(base_Address);
            block.Reserve_Size(usage_Block.Used_Length);

            if (block.IsEmpty())
                _end_Address_Index.Remove(block.End_Address());
            else
                _base_Address_Index[block.Base_Address()] = block;
        }


        public void Add_Block_Address_To_Available_Space()
        {
            // TODO compact addresses
            foreach (var address in Freed_Empty_Slots)
            {
                if (_end_Address_Index.ContainsKey(address))
                {
                    Block before = _end_Address_Index[address];

                    int beforeLength = before.Length;
                    before.Append_Block(Block_Size);
                    int newLength = before.Length;

                    if (_base_Address_Index.ContainsKey(address + Block_Size))
                    {
                        Block after = _base_Address_Index[address + Block_Size];

                        Fix_Block_Position_In_Groups(after, after.Length, 0);
                        newLength += after.Length;
                        before.Append_Block(after.Length);

                        _base_Address_Index.Remove(address + Block_Size);
                    }

                    _end_Address_Index.Remove(address);
                    _end_Address_Index[before.End_Address()] = before;

                    Fix_Block_Position_In_Groups(before, beforeLength, newLength);
                    continue;
                }

                if (_base_Address_Index.ContainsKey(address + Block_Size))
                {
                    Block after = _base_Address_Index[address + Block_Size];

                    Fix_Block_Position_In_Groups(after, after.Length, after.Length + Block_Size);

                    _base_Address_Index.Remove(after.Base_Address());
                    after.Prepend_Block(Block_Size);
                    _base_Address_Index[after.Base_Address()] = after;
                    continue;
                }

                Insert_Block(address, Block_Size);
            }
        }

        protected Block_Group? Find_Block_Group(int length)
        {
            // TODO slow
            return Empty_Slots.Where(s => s.Length == length)
                    .Select(s => new Nullable<Block_Group>(s))
                    .DefaultIfEmpty(new Nullable<Block_Group>())
                    .SingleOrDefault();
        }

        protected void Fix_Block_Position_In_Groups(Block block, int old_Length, int length)
        {
            var group = Find_Block_Group(old_Length);
            group.Value.Blocks.Remove(old_Length);

            if (length == 0)
                return;
            group = Find_Block_Group(length);
            if (group == null)
            {
                group = new Block_Group { Length = length, Blocks = new Dictionary<long,Block>() };
                Empty_Slots.Add(group.Value);
            }
            group.Value.Blocks[length] = block;
        }

        protected IEnumerable<Block_Usage> Look_For_Available_Blocks(int length)
        {
            Empty_Slots.Sort(new Length_Comparer(length));

            var enumerator = Empty_Slots.GetEnumerator();
            while (enumerator.MoveNext() && length > 0)
            {
                var group = enumerator.Current;

                var blocks = group.Blocks.Values.GetEnumerator();
                while (blocks.MoveNext() && length > 0)
                {
                    var block = blocks.Current;
                    yield return new Block_Usage(block, -1);
                    length -= group.Length;
                }
            }

            //Array.Sort(Empty_Slots);

            //int foundIndex = Array.BinarySearch(Empty_Slots, new Block(0, lenght));
            //if (foundIndex > 0)
            //{
            //    var result = Empty_Slots[foundIndex];
            //    yield return new Block_Usage(result, foundIndex);
            //}
            //else
            //{
            //    int complement = ~foundIndex;
            //    if (complement != Empty_Slots.Length)
            //    {
            //        var result = Empty_Slots[complement];
            //        yield return new Block_Usage(result, complement);
            //    }
            //    else  // scamuzzolandia !
            //    {
            //        int index = Empty_Slots.Length - 1;
            //        int max = 10;
            //        while (index > 0 && lenght > 0 && max > 0)
            //        {
            //            var result = Empty_Slots[index];
            //            if (result == null)
            //                break;
            //            yield return new Block_Usage(result, index);
            //            lenght -= result.Length;
            //            index--;
            //            max--;
            //        }
            //    }
            //}
        }

        protected void Insert_Block(long address, int lenght)
        {
            Block_Group? group = Empty_Slots.Where(s => s.Length == lenght)
                .Select(s=> new Nullable<Block_Group>(s)) 
                .DefaultIfEmpty(new Nullable<Block_Group>())
                .SingleOrDefault();

            if (group == null)
            {
                group = new Block_Group() { Length = lenght, Blocks = new Dictionary<long,Block>() };
                Empty_Slots.Add(group.Value);
            }

            var block = new Block(address, lenght);
            group.Value.Blocks[lenght] = block;

            _base_Address_Index[address] = block;
            _end_Address_Index[block.End_Address()] = block;
        }


        protected void Update_Addresses_From(Node[] nodes, Node root, Queue<long> addresses)
        {
            root.Address = addresses.Dequeue();
            if (root.IsLeaf)
                return;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Parent != root)
                    continue;

                var old_Child_Address = nodes[i].Address;

                Update_Addresses_From(nodes, nodes[i], addresses);

                root.Update_Child_Address(old_Child_Address, nodes[i].Address);
            }
        }

        protected void Update_Addresses_From_Base(Node[] nodes, Node root, ref long base_Address)
        {
            root.Address = base_Address;
            base_Address += Block_Size;
            if (root.IsLeaf)
                return;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Parent != root)
                    continue;

                var new_Child_Address = base_Address;
                var old_Child_Address = nodes[i].Address;
                root.Update_Child_Address(old_Child_Address, new_Child_Address);

                Update_Addresses_From_Base(nodes, nodes[i], ref base_Address);
            }
        }


        public void Free_Address(long address)
        {
            if (address != 0)
                Freed_Empty_Slots.Add(address);
        }
        public void Append_Node(Node node)
        {
            Pending_Nodes.Add(node);
        }

        public static List<int> _statistics_blocks_found = new List<int>();
        public void Append_New_Root(Node root)
        {
            var blocks = Look_For_Available_Blocks(Pending_Nodes.Count * Block_Size).ToList();
            _statistics_blocks_found.Add(blocks.Count);

            var block_At_End_Of_File = new Block_Usage(new Block(_index_Pointer, int.MaxValue), -1);

            blocks = blocks.Concat(new[] { block_At_End_Of_File }).OrderBy(b => b.Base_Address()).ToList();

            var addressesQueue = new Queue<long>();
            foreach (var block in blocks)
                for (int i = 0; i < block.Length && Pending_Nodes.Count > addressesQueue.Count; i += Block_Size)
                    addressesQueue.Enqueue(block.Base_Address() + i);

            Update_Addresses_From(Pending_Nodes.ToArray(), root, addressesQueue);

            var nodes = new Queue<Node>(Pending_Nodes.OrderBy(d => d.Address));
            foreach (var block in blocks)
            {
                if (nodes.Count == 0)
                    break;

                var toUpdate = new List<Node>();
                for (int j = 0; j < block.Length && nodes.Count > 0; j += Block_Size)
                {
                    toUpdate.Add(nodes.Dequeue());
                    block.Use(Block_Size);
                }

                int buffer_Size = toUpdate.Count * Block_Size;
                var buffer = new byte[buffer_Size];
                for (int i = 0; i < toUpdate.Count; i++)
                    toUpdate[i].To_Bytes_In_Buffer(buffer, i * Block_Size);

                Index_Stream.Seek(block.Base_Address(), SeekOrigin.Begin);
                Index_Stream.Write(buffer, 0, buffer.Length);
            }

            foreach (var block in blocks)
            {
                if (block == block_At_End_Of_File)
                   _index_Pointer = block.Base_Address() + block.Used_Length;
                else
                    Block_Usage_Finished(block);
            }

            //Add_Block_Address_To_Available_Space(Freed_Empty_Slots);

            Nodes.Clear();
            Nodes.AddRange(Pending_Nodes);
            Pending_Nodes.Clear();
            //Freed_Empty_Slots.Clear();
            Uncommitted_Root = root;
        }


        public IEnumerable<Node> Last_Cached_Nodes()
        {
            return Nodes;
        }
    }

    public struct Block_Group
    {
        public int Length { get; set; }
        public Dictionary<long,Block> Blocks { get; set; }
    }


    public class Length_Comparer : IComparer<Block_Group>
    {
        public long Length { get; set; }
        public Length_Comparer(long length)
        {
            Length = length;
        }

        public int Compare(Block_Group x, Block_Group y)
        {
            long dx = Length - x.Length;
            long dy = Length - y.Length;

            if (dx == 0 && dy == 0)
                return 0;

            if (dx == 0)
                return -1;
            if (dy == 0)
                return 1;

            if (dx < 0 && dy < 0)
                return Math.Sign(dy - dx);

            if (dx > 0 && dy > 0)
                return Math.Sign(dx - dy);

            if (dx > 0 && dy < 0)
                return 1;

            if (dx < 0 && dy > 0)
                return -1;

            return 0;
        }
    }
}
