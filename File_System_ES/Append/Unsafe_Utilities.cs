﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Append
{
    public static class Unsafe_Utilities
    {
        public unsafe static void Memcpy(byte* dest, byte* src, int len)
        {
            if (len >= 16)
            {
                do
                {
                    *(long*)dest = *(long*)src;
                    *(long*)(dest + 8) = *(long*)(src + 8);
                    dest += 16;
                    src += 16;
                }
                while ((len -= 16) >= 16);
            }
            if (len > 0)
            {
                if ((len & 8) != 0)
                {
                    *(long*)dest = *(long*)src;
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    *(int*)dest = *(int*)src;
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    *(short*)dest = *(short*)src;
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                {
                    byte* expr_75 = dest;
                    dest = expr_75 + 1;
                    byte* expr_7C = src;
                    src = expr_7C + 1;
                    *expr_75 = *expr_7C;
                }
            }
        }
    }
}
