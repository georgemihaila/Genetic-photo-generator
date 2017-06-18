using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genetic_photo_generator
{
    public class DNA
    {
        public DNA(byte minr, byte ming, byte minb, byte maxr, byte maxg, byte maxb)
        {
            MinR = minr;
            MinG = ming;
            MinB = minb;
            MaxR = maxr;
            MaxG = maxg;
            MaxB = maxb;
        }

        public byte MinR;
        public byte MinG;
        public byte MinB;
        public byte MaxR;
        public byte MaxG;
        public byte MaxB;
    }
}
