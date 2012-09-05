using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
{
    public partial class StringBPlusTree
    {
        public BPlusTree BPlusTree { get; set; }

        public StringBPlusTree(Stream infoStream, Stream indexStream, Stream dataStream)
        {
            BPlusTree = new BPlusTree(infoStream, indexStream, dataStream);
        }


        public void Commit()
        {
            BPlusTree.Commit();
        }


   

        public string Get(int key)
        {
            var bytes = BPlusTree.Get(key);
            return Encoding.UTF8.GetString(bytes);
        }

        public void Put(int key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            BPlusTree.Put(key, bytes);
        }
    }
    
}
