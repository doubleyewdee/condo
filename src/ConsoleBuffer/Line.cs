namespace ConsoleBuffer
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public sealed class Line : IEnumerable<Character>
    {
        private readonly List<Character> chars;

        public int Length => this.chars.Count;

        /// <summary>
        /// Provides the character at the given position, potentially extending the line or providing a suitable default if the position is beyond the current character count.
        /// </summary>
        /// <param name="pos">Position to retrieve.</param>
        /// <returns>The character at the given position, or a suitable default if the position is beyond the current end of the line.</returns>
        public Character this[int pos]
        {
            get
            {
                if (pos < this.chars.Count)
                {
                    return this.chars[pos];
                }

                // for short lines we can lazily keep the attributes (specifically background) from whatever our last character was, assuming we had one.
                var ch = this.chars.Count > 0 ? this.chars[this.chars.Count - 1] : new Character { Options = Character.DefaultOptions };
                ch.Glyph = 0x0;
                return ch;
            }
            set
            {
                this.Extend(pos);
                this.chars[pos] = value;
            }
        }

        public Line()
        {
            var hintSize = 80;
            this.chars = new List<Character>(hintSize);
            this[0] = new Character { Glyph = 0x0, Options = Character.DefaultOptions };
        }

        private void Extend(int pos)
        {
            while (this.chars.Count <= pos)
            {
                this.chars.Add(new Character { Glyph = 0x20 });
            }
        }

        public IEnumerator<Character> GetEnumerator()
        {
            return this.chars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.chars.GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(this.chars.Count);
            foreach (var c in this.chars)
            {
                sb.Append((char)c.Glyph);
            }
            return sb.ToString();
        }
    }
}
