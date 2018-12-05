namespace condo
{
    using System.Collections.Generic;
    using System.Windows.Media;

    sealed class SolidBrushCache
    {
        struct CacheEntry
        {
            public SolidColorBrush Brush;
            public LinkedListNode<uint> LRUNode;
        }

        // Set the cache size to 256 to cover the expected common case of people using the xterm 256 color palette
        // in some form. This does allow us to push unused colors from that cache when users instead elect to
        // work with their own RGB values. It's also fairly likely a screen won't have > 256 active colors outside
        // testing for RGB drawing etc.
        private const int CacheSize = 256;

        private readonly Dictionary<uint, CacheEntry> cache = new Dictionary<uint, CacheEntry>(CacheSize);
        private readonly LinkedList<uint> lruEntries = new LinkedList<uint>();

        public SolidBrushCache() { }

        public SolidColorBrush GetBrush(byte R, byte G, byte B, byte A = 255)
        {
            LinkedListNode<uint> node;
            var index = ColorToIndex(R, G, B, A);
            
            if (this.cache.TryGetValue(index, out var cacheEntry))
            {
                node = cacheEntry.LRUNode;
                this.lruEntries.Remove(node);
                this.lruEntries.AddFirst(node);

                return cacheEntry.Brush;
            }

            var brush = new SolidColorBrush(new Color { R = R, G = G, B = B, A = A });
            brush.Freeze();
            if (this.cache.Count == CacheSize)
            {
                node = this.lruEntries.Last;
                this.lruEntries.RemoveLast();
                this.cache.Remove(node.Value);
                node.Value = index;
            }
            else
            {
                node = new LinkedListNode<uint>(index);
            }

            this.lruEntries.AddFirst(node);
            this.cache.Add(index, new CacheEntry { Brush = brush, LRUNode = node });
            return brush;
        }

        private static uint ColorToIndex(byte R, byte G, byte B, byte A)
        {
            int ret = R;
            ret += (G << 8);
            ret += B << 16;
            ret += A << 24;

            return (uint)ret;
        }
    }
}
