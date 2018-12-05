using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Linq;

namespace condo

{
    public class SolidBrushCache
    {
        private const int maxCache = 16;

        
        public class BrushInfo
        {
            private static int timeCounter = 0;

            // Each brush gets a 'time' whitch is a monotonically increasing number
            public BrushInfo() { this.createTime = timeCounter++; }
            public int createTime { get; }
            public SolidColorBrush brush {get;set;}
            public override string ToString()
            {
                return $"Brush: {this.brush.Color} Time: {this.createTime}";
            }
        }

        Dictionary<uint, BrushInfo> cache = new Dictionary<uint, BrushInfo>();

        public SolidBrushCache() { }

        public SolidColorBrush GetBrush(byte R, byte G, byte B, byte A = 255)
        {
            // Compute a unique hash
            byte[] bar = { R, B, G, A };
            uint hash = BitConverter.ToUInt32(bar, 0);

            
            // Do we have it?
            if (this.cache.TryGetValue(hash, out var br))
            {
                // We have the brush in the cache
                return br.brush;
            }

            // We don't have the brush

            var newBr = new SolidColorBrush(new Color { R = R, G = G, B = B, A = 255 });
            var brInfo = new BrushInfo { brush = newBr };

            // If we are going to spill over our max count (count plus 1), then trim
            if(this.cache.Count + 1 > maxCache)
            {
                Debug.WriteLine("Need to trim cache");

                var x = this.cache
                    .OrderBy(o => o.Value.createTime)
                    .Take(maxCache / 2);

                foreach (var item in x)
                {
                    this.cache.Remove(item.Key);
                }

                
            }

            // Put the new brush in the cache and return it
            this.cache.Add(hash, brInfo);

            return newBr;
        }
    }
}
