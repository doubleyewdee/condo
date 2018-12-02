using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace condo

{
    public class SolidBrushCache
    {
        Dictionary<uint, SolidColorBrush> cache = new Dictionary<uint, SolidColorBrush>();

        public SolidBrushCache() { }

        public SolidColorBrush GetBrush(byte R, byte G, byte B, byte A = 255)
        {
            byte[] bar = { R, B, G, A };
            uint hash = BitConverter.ToUInt32(bar, 0);

            
            if (this.cache.TryGetValue(hash, out var br))
            {
                return br;
            }

            br = new SolidColorBrush(new Color { R = R, G = G, B = B, A = 255 });

            this.cache.Add(hash, br);

            return br;
        }
    }
}
