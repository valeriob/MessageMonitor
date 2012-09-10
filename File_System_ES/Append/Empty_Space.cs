using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public class Empty_Space
    {
        Dictionary<long, Block> _base_Address_Index = new Dictionary<long, Block>();
        Dictionary<long, Block> _end_Address_Index = new Dictionary<long, Block>();

        List<Block_Group> Empty_Slots;

    }
}
