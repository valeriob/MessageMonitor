using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }
       
        public Dictionary<long,Node> Cached_Nodes { get; set; }

        protected void Free_Address(long address)
        {
            if (address != 0)
                Freed_Empty_Slots.Add(address);
        }

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


        public Block[] Empty_Slots = new Block[0];
        public Dictionary<long, Block> _base_Address_Index = new Dictionary<long, Block>();
        public Dictionary<long, Block> _end_Address_Index = new Dictionary<long, Block>();

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

            var emptyIndex = Array.BinarySearch(Empty_Slots, null );
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

    }


    public class Block : IComparable<Block>
    {
        public Block(long baseAddress, int lenght)
        {
            _Base_Address = baseAddress;
            Length = lenght;
        }

        public long End_Address()
        {
            return Base_Address() + Length;
        }
        public long Base_Address()
        {
            return _Base_Address.Value;
        }

        protected long? _Base_Address;

        public int Length { get; protected set; }
      

        public bool IsEmpty()
        {
            return Length == 0;
        }

        public bool IsValid()
        {
            return _Base_Address != null;
        }

        public void Reserve_Size(int lenght)
        {
            Length -= lenght;
            _Base_Address += lenght;
        }

        public void Append_Block(int size)
        {
            Length += size;
        }

        public bool Has_Space_Before(int size)
        {
            return Base_Address() - size >= 0;
        }
        public void Prepend_Block(int size)
        {
            _Base_Address -= size;
            Length += size;
        }


        public override string ToString()
        {
            if (IsValid())
                return string.Format("From : {0}, To: {1}, Lenght: {2}", _Base_Address, End_Address(), Length);
            else
                return string.Format("Invalid. Lenght: {0}",  Length);
        }




        public bool Equals(Block other)
        {
            return other.Base_Address() == Base_Address();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Block;
            return other != null && other.Base_Address() == Base_Address();
        }

        public override int GetHashCode()
        {
            return Base_Address().GetHashCode();
        }



        public int CompareTo(Block other)
        {
            return other == null ? int.MaxValue : Length - other.Length;
        }

    }

    public class Block_Usage
    {
        public Block_Usage(Block block, int index)
        {
            Block = block;
            Index = index;
        }

        public Block Block { get; protected set; }
        public int Used_Length { get; protected set; }
        public int Index { get; protected set; }

        public void Use(int size)
        {
            Used_Length += size;
        }

        public int Length { get { return Block.Length; } }

        public long Base_Address() { return Block.Base_Address(); }


        public override string ToString()
        {
            return string.Format("Used {0} of {1}", Used_Length, Block.ToString());
        }
    }
}
