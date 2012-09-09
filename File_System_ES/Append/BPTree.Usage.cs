using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        //public Queue<long> Empty_Slots { get; set; }
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }
       
        public Dictionary<long,Node> Cached_Nodes { get; set; }

        protected void Free_Address(long address)
        {
            if (address != 0)
                Freed_Empty_Slots.Add(address);
        }

        /*
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
        }*/


        //protected void Fixup_Addresses(Node root)
        //{
        //    if (root.IsLeaf)
        //        return;
        //    for (int i = 0; i < root.Key_Num + 1; i++)
        //    {
        //        var child = Pending_Nodes.Single(n => n.Address == root.Pointers[i]);
        //        child.Parent = root;
        //        Fixup_Addresses(child);
        //    }
        //}

        protected void Block_Usage_Finished(Block block, int size)
        {
            var base_Address = block.Base_Address();

            //if (block.IsEmpty())
            //{
            //    _base_Address_Index.Remove(base_Address);
            //    _end_Address_Index.Remove(base_Address);

            //    Empty_Slots[block.Index] = null;//.Invalidate();
            //    _base_Address_Index.Remove(base_Address);
            //}
            //else
            //{
            //    _base_Address_Index.Remove(base_Address);
            //    block.Reserve_Size(size);
            //    _base_Address_Index[base_Address] = block;
            //}

            _base_Address_Index.Remove(base_Address);
            block.Reserve_Size(size);

            if (block.IsEmpty())
            {
                _end_Address_Index.Remove(block.End_Address());
                Empty_Slots[block.Index] = null;
            }
            else
                _base_Address_Index[block.Base_Address()] = block;
        }


        Block[] Empty_Slots = new Block[0];
        Dictionary<long, Block> _base_Address_Index = new Dictionary<long, Block>();
        Dictionary<long, Block> _end_Address_Index = new Dictionary<long, Block>();

        protected void Add_Block_Address_To_Available_Space(IEnumerable<long> addresses)
        {
            foreach (var address in addresses)
            {
                bool a = false;
                bool b = false;

                if (_end_Address_Index.ContainsKey(address))
                {
                    Block before = _end_Address_Index[address];
                    //long before_End_Address = before.End_Address();

                    before.Append_Block(Block_Size);
                    if (_base_Address_Index.ContainsKey(address + Block_Size))
                    {
                        Block after = _base_Address_Index[address + Block_Size];
                        before.Append_Block(after.Lenght);

                        _base_Address_Index.Remove(address + Block_Size);
                      
                        //after.Invalidate();
                        var idx = Array.IndexOf(Empty_Slots, after);
                        Empty_Slots[idx] = null;
                    }

                    //before.Append_Block(Block_Size);

                    _end_Address_Index.Remove(address);
                    _end_Address_Index[before.End_Address()] = before;
                    a = true;
                    continue;
                }

                if (_base_Address_Index.ContainsKey(address + Block_Size))
                {
                    Block after = _base_Address_Index[address + Block_Size];
                    //if (after.Has_Space_Before(Block_Size))
                    {
                        _base_Address_Index.Remove(after.Base_Address());
                        after.Prepend_Block(Block_Size);
                        _base_Address_Index[after.Base_Address()] = after;
                    }
                    b = true;
                    continue;
                }

                Insert_Block(address, Block_Size);
            }
        }

        protected Block Look_For_Available_Block(int size)
        {
            Array.Sort(Empty_Slots);

            int foundIndex = Array.BinarySearch(Empty_Slots, new Block(0, size));
            if (foundIndex > 0)
            {
                var result = Empty_Slots[foundIndex];
                result.Index = foundIndex;
                return result;
            }
            int complement = ~foundIndex;
            if (complement != Empty_Slots.Length)
            {
                var result = Empty_Slots[complement];
                result.Index = complement;
                return result;
            }

            return null;
        }

        protected void Insert_Block(long address, int lenght)
        {
           // Array.Sort(Empty_Slots);

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
        


        protected void Update_Addresses_From(Node[] nodes, Node root, ref long base_Address)
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

                Update_Addresses_From(nodes, nodes[i], ref base_Address);
            }
            //foreach (var child in nodes.Where(n => n.Parent == root))
            //{
            //    var new_Child_Address = base_Address;
            //    var old_Child_Address = child.Address;
            //    root.Update_Child_Address(old_Child_Address, new_Child_Address);

            //    Update_Addresses_From(nodes, child, ref base_Address);
            //}
        }

    }


    public class Block : IComparable<Block>
    {
        public Block(long baseAddress, int lenght)
        {
            _Base_Address = baseAddress;
            Lenght = lenght;
        }

        public long End_Address()
        {
            return Base_Address() + Lenght;
        }
        public long Base_Address()
        {
            return _Base_Address.Value;
        }

        protected long? _Base_Address;

        public int Lenght { get; protected set; }
        public int Index { get; set; }


        //public void Invalidate() 
        //{
        //    _Base_Address = null;
        //    Lenght = 0; 
        //}
        //public void Restore(long baseAddress, int lenght)
        //{
        //    _Base_Address = baseAddress;
        //    Lenght = lenght;
        //}


        public bool IsEmpty()
        {
            return Lenght == 0;
        }

        public bool IsValid()
        {
            return _Base_Address != null;
        }

        public void Reserve_Size(int lenght)
        {
            Lenght -= lenght;
            _Base_Address += lenght;
        }

        public void Append_Block(int size)
        {
            Lenght += size;
        }

        public bool Has_Space_Before(int size)
        {
            return Base_Address() - size >= 0;
        }
        public void Prepend_Block(int size)
        {
            _Base_Address -= size;
            Lenght += size;
        }


        public override string ToString()
        {
            if (IsValid())
                return string.Format("From : {0}, To: {1}, Lenght: {2}", _Base_Address, End_Address(), Lenght);
            else
                return string.Format("Invalid. Lenght: {0}",  Lenght);
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
            return other == null ? int.MaxValue : Lenght - other.Lenght;
        }

    }
  

}
