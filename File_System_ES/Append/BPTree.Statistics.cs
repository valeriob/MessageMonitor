using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public partial class BPlusTree<T>
    {
        public Dictionary<long, int> _readMemory_Count = new Dictionary<long, int>();
        public Dictionary<long, int> _writeMemory_Count = new Dictionary<long, int>();

        public long cache_hits;
        public long cache_misses;


        public int commitsCount;
        public int writes;

        public Usage Count_Empty_Slots()
        {
            int invalid = 0;
            int valid = 0;
            int blockSize = Node_Factory.Size_In_Bytes(3);
            long position = Index_Stream.Position;

            Index_Stream.Seek(8, SeekOrigin.Begin);
            var buffer = new byte[blockSize];
            while (Index_Stream.Read(buffer, 0, buffer.Length) > 0)
            {
                var node = Node_Factory.From_Bytes(buffer, 3);
                if (node.IsValid)
                    valid++;
                else
                    invalid++;
            }

            int used = valid * blockSize;
            int wasted = invalid * blockSize;

            Index_Stream.Seek(position, SeekOrigin.Begin);
            return new Usage { Invalid = invalid, Valid = valid };
        }
    }
}
