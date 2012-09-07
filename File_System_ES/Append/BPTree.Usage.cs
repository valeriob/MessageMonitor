using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree
    {
        public Queue<long> Empty_Slots { get; set; }
        public List<long> Reserved_Empty_Slots { get; set; }
        public List<long> Freed_Empty_Slots { get; set; }

        protected void Free_Address(long address)
        {
            if (address != 0)
                Freed_Empty_Slots.Add(address);
        }


        protected bool Should_Reuse_Old_Addresses()
        {
            return false;
            var size = Node.Size_In_Bytes(Size);
            //var interval = Get_Intervall((current_Hight + 1) * size * 2 + 1, size);
            //return interval.HasValue && interval.
            return Contiguos_Space(size) > ((current_Depth + 1) * size * 2 + 1);
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


        protected void Fixup_Addresses(Node root)
        {
            if (root.IsLeaf)
                return;
            for (int i = 0; i < root.Key_Num + 1; i++)
            {
                var child = Pending_Nodes.Single(n => n.Address == root.Pointers[i]);
                child.Parent = root;
                Fixup_Addresses(child);
            }
        }


        protected void Update_Addresses_From(List<Node> nodes, Node root, ref long base_Address)
        {
            root.Address = base_Address;
            base_Address += Block_Size;
            if (root.IsLeaf)
                return;

            foreach (var child in nodes.Where(n => n.Parent == root))
            {
                //root.Update_Child_Address(child.Address, base_Address += Block_Size);
                var new_Child_Address = base_Address;
                var old_Child_Address = child.Address;
                root.Update_Child_Address(old_Child_Address, new_Child_Address);

                Update_Addresses_From(nodes, child, ref base_Address);
                //root.Update_Child_Address(old_Child_Address, new_Child_Address);
            }
        }

    }


    public struct Block
    {
        public long Base_Address { get; set; }
        public int Lenght { get; set; }
    }



}
