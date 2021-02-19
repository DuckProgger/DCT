﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCT
{
    struct YCbCr
    {
        public byte Y { get; set; }
        public byte Cb { get; set; }
        public byte Cr { get; set; }

        public YCbCr(byte y, byte cb, byte cr)
        {
            Y = y;
            Cb = cb;
            Cr = cr;
        }
    }
}