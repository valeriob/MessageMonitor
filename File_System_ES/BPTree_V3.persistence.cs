using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace File_System_ES.V3
{
    public partial class BPlusTree<T>
    {
    
   

        private long _currentPointer;
        private long Current_Pointer()
        {
            return _currentPointer;
        }
        BinaryFormatter serializer = new BinaryFormatter();

        protected Node Read_Node(Node parent, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            var obj = serializer.Deserialize(Stream);
            var node = obj as Node;
            if (node == null)
                throw new Exception("Expected Type Node, found " + obj.GetType());

            node.Parent = parent;
            node.Address = address;
            return node;
        }

        protected T Read_Data(long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            var obj = serializer.Deserialize(Stream);
            if (!(obj is T))
                throw new Exception("Expected Type "+typeof(T)+", found " + obj.GetType());
            return (T)obj;
        }
        protected void Update_Node(Node node)
        {
            Write_Object(node, node.Address);
        }
        protected void Write_Object(object value, long address)
        {
            Stream.Seek(address, SeekOrigin.Begin);
            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(buffer, value);
                var bytes = buffer.GetBuffer();
                // TODO CHECK INT LONG ADDRESS
                Stream.Write(bytes, 0, bytes.Length);
                _currentPointer += bytes.Length;
            }
        }

        protected void Write_Node(Node node)
        {
            var address = Current_Pointer();
            Write_Node(node, address);
        }
        protected void Write_Node(Node node, long address)
        {
            Write_Object(node, address);
            node.Address = address;
        }
    }
    
}
