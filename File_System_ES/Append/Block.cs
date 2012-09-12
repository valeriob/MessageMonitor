using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public struct Block : IComparable<Block>
    {
        //public Block(long baseAddress, int lenght)
        //{
        //    _Base_Address = baseAddress;
        //    Length = lenght;
        //}

        public long? _Base_Address;
        public int Length;



        public long End_Address()
        {

            return Base_Address() + Length;
        }
        public long Base_Address()
        {
            return _Base_Address.Value;
        }      

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
            return ((Block)obj).Base_Address() == Base_Address();
            //var other = obj as Block;
            //return other != null && other.Base_Address() == Base_Address();
        }

        public override int GetHashCode()
        {
            return Base_Address().GetHashCode();
        }



        public int CompareTo(Block other)
        {
            return Length - other.Length;
            //return other == null ? int.MaxValue : Length - other.Length;
        }

    }

    public struct Block_Usage
    {
        //public Block_Usage(Block block)
        //{
        //    Block = block;
        //}


        public Block Block { get; set; }
        public int Used_Length { get; set; }

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
