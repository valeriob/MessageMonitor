using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public interface ISerializer<T> where T: IEquatable<T>, IComparable<T>
    {
        byte[] To_Bytes(T value);
        T Get_Instance(byte[] value, int startIndex);

        int Fixed_Size();
    }
}
