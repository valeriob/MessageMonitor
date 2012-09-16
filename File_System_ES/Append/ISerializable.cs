using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public interface ISerializer<T> where T: IEquatable<T>, IComparable<T>
    {
        byte[] GetBytes(T value);
        void To_Buffer(T[] values, int end_Index, byte[] buffer, int buffer_offset);
        T Get_Instance(byte[] value, int startIndex);
        T[] Get_Instances(byte[] value, int startIndex, int length);

        int Serialized_Size_For_Single_Key_In_Bytes();
    }
}
