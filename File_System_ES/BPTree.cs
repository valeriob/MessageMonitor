using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES
{
    public interface IBPlusTree
    {
        byte[] Get(int key);
        void Put(int key, byte[] value);

    }
    
 

}
