using System;
using System.Collections.Generic;
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
        public Block[] Get_Empty_Slots() { return Empty_Slots; }

        public Pending_Changes(Stream index_Stream, int blockSize, Block[] emptySlots)
        {
            Index_Stream = index_Stream;
            Block_Size = blockSize;

            Freed_Empty_Slots = new List<long>();
            Pending_Nodes = new List<Node>();
            Nodes = new List<Node>();
            Empty_Slots = emptySlots;

            _index_Pointer = Math.Max(8, index_Stream.Length);
        }


        Block[] Empty_Slots;
        List<long> Freed_Empty_Slots;
        List<Node> Pending_Nodes;
        List<Node> Nodes;


        public Dictionary<long, Block> _base_Address_Index = new Dictionary<long, Block>();
        public Dictionary<long, Block> _end_Address_Index = new Dictionary<long, Block>();


        protected void Block_Usage_Finished(Block_Usage usage_Block)
        {
            var block = usage_Block.Block;

            var base_Address = block.Base_Address();

            _base_Address_Index.Remove(base_Address);
            block.Reserve_Size(usage_Block.Used_Length);

            if (block.IsEmpty())
            {
                _end_Address_Index.Remove(block.End_Address());
                Empty_Slots[usage_Block.Index] = null;
            }
            else
                _base_Address_Index[block.Base_Address()] = block;
        }


        protected void Add_Block_Address_To_Available_Space(IEnumerable<long> addresses)
        {
            foreach (var address in addresses)
            {
                if (_end_Address_Index.ContainsKey(address))
                {
                    Block before = _end_Address_Index[address];

                    before.Append_Block(Block_Size);
                    if (_base_Address_Index.ContainsKey(address + Block_Size))
                    {
                        Block after = _base_Address_Index[address + Block_Size];
                        before.Append_Block(after.Length);

                        _base_Address_Index.Remove(address + Block_Size);

                        var idx = Array.IndexOf(Empty_Slots, after);
                        Empty_Slots[idx] = null;
                    }

                    _end_Address_Index.Remove(address);
                    _end_Address_Index[before.End_Address()] = before;
                    continue;
                }

                if (_base_Address_Index.ContainsKey(address + Block_Size))
                {
                    Block after = _base_Address_Index[address + Block_Size];

                    _base_Address_Index.Remove(after.Base_Address());
                    after.Prepend_Block(Block_Size);
                    _base_Address_Index[after.Base_Address()] = after;
                    continue;
                }

                Insert_Block(address, Block_Size);
            }
        }

        protected IEnumerable<Block_Usage> Look_For_Available_Blocks(int size)
        {
            Array.Sort(Empty_Slots);

            int foundIndex = Array.BinarySearch(Empty_Slots, new Block(0, size));
            if (foundIndex > 0)
            {
                var result = Empty_Slots[foundIndex];
                yield return new Block_Usage(result, foundIndex);
            }
            else
            {
                int complement = ~foundIndex;
                if (complement != Empty_Slots.Length)
                {
                    var result = Empty_Slots[complement];
                    yield return new Block_Usage(result, complement);
                }
                else  // scamuzzolandia !
                {
                    int index = Empty_Slots.Length - 1;
                    while (index > 0 && size > 0)
                    {
                        var result = Empty_Slots[index];
                        if (result == null)
                            break;
                        yield return new Block_Usage(result, index);
                        size -= result.Length;
                        index--;
                    }
                }
            }
        }

        protected void Insert_Block(long address, int lenght)
        {
            Array.Sort(Empty_Slots);

            var emptyIndex = Array.BinarySearch(Empty_Slots, null);
            var old_lenght = Empty_Slots.Length;

            Block block = null;
            if (emptyIndex < 0)
            {
                Array.Resize(ref Empty_Slots, old_lenght + 8);
                block = Empty_Slots[old_lenght] = new Block(address, lenght);
            }
            else
                block = Empty_Slots[emptyIndex] = new Block(address, lenght);

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

        public void Append_New_Root(Node root)
        {
            var blocks = Look_For_Available_Blocks(Pending_Nodes.Count * Block_Size);
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
                    toUpdate[i].To_Bytes(buffer, i * Block_Size);

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

            Add_Block_Address_To_Available_Space(Freed_Empty_Slots);


            Nodes.AddRange(Pending_Nodes);
            Pending_Nodes.Clear();
            Freed_Empty_Slots.Clear();
            Uncommitted_Root = root;
        }


        public IEnumerable<Node> Last_Cached_Nodes()
        {
            return Nodes;
        }
    }
}
