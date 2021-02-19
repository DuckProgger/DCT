using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCT
{
    struct YCbCr
    {
        public sbyte Y { get; set; }
        public sbyte Cb { get; set; }
        public sbyte Cr { get; set; }

        public YCbCr(sbyte y, sbyte cb, sbyte cr)
        {
            Y = y;
            Cb = cb;
            Cr = cr;
        }
    }
}
