using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        //public Queue<long> Empty_Slots { get; set; }
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }
        public List<Block> Empty_Slots { get; set; }
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


        protected void Add_Block_Address_To_Available_Space(long address)
        {
            Block before = null;
            int beforeIdx = -1;
            Block after = null;
            int afterIdx = -1;
            for (int i = 0; i < Empty_Slots.Count; i++)
            {
                if (Empty_Slots[i].Base_Address == address + Block_Size)
                {
                    before = Empty_Slots[i];
                    beforeIdx = i;
                }
                if (Empty_Slots[i].End_Address() == address)
                {
                    after = Empty_Slots[i];
                    afterIdx = i;
                }
            }

            if(before != null && after != null)
            {
                Empty_Slots.RemoveAt(afterIdx);
                before.Append_Block(Block_Size + after.Lenght);
            }
            else
            {
                if(before != null)
                    before.Prepend_Block(Block_Size);
                if(after != null)
                    after.Append_Block(Block_Size);

                 Empty_Slots.Add(new Block(address, Block_Size));
            }

        }

        protected Block Look_For_Available_Block(int size)
        {
            for (int i = 0; i < Empty_Slots.Count; i++)
            {
                if (Empty_Slots[i].Lenght >= size)
                    return Empty_Slots[i];
            }

            return null;
        }


        protected void Update_Addresses_From(List<Node> nodes, Node root, ref long base_Address)
        {
            root.Address = base_Address;
            base_Address += Block_Size;
            if (root.IsLeaf)
                return;

            foreach (var child in nodes.Where(n => n.Parent == root))
            {
                var new_Child_Address = base_Address;
                var old_Child_Address = child.Address;
                root.Update_Child_Address(old_Child_Address, new_Child_Address);

                Update_Addresses_From(nodes, child, ref base_Address);
            }
        }

    }


    public class Block
    {
        public Block(long baseAddress, int lenght)
        {
            Base_Address = baseAddress;
            Lenght = lenght;
        }

        public long Base_Address { get; protected set; }
        public int Lenght { get; protected set; }


        public long End_Address()
        {
            return Base_Address + Lenght;
        }

        public bool IsEmpty()
        {
            return Lenght == 0;
        }

        public void Reserve_Size(int lenght)
        {
            Lenght -= lenght;
            Base_Address += lenght;
        }

        public void Append_Block(int size)
        {
            Lenght += size;
        }
        public void Prepend_Block(int size)
        {
            Base_Address -= size;
            Lenght += size;
        }


        public override string ToString()
        {
            return string.Format("From : {0}, To: {1}, Lenght: {1}",Base_Address, End_Address(), Lenght);
        }
    }



}
