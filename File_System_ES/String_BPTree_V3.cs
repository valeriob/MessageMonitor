using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES
{
    public partial class String_BPlusTree 
    {
        public IBPlusTree BPlusTree { get; set; }

        public String_BPlusTree(IBPlusTree tree)
        {
            BPlusTree = tree;
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

        public void Flush()
        {
            BPlusTree.Flush();
        }

        public void Commit()
        {
            BPlusTree.Commit();
        }

        public void RollBack()
        {
            BPlusTree.RollBack();
        }
    }
    
}
