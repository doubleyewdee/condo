namespace ConsoleBuffer
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public sealed class Line : IEnumerable<Character>
    {
        private readonly List<Character> chars;

        public int Length => this.chars.Count;

        // XXX: should probably remove users of Get/Set and just have them call this for clarity.
        public Character this[int pos] { get => this.Get(pos); set => this.Set(pos, value); }

        public Line()
        {
            var hintSize = 80;
            this.chars = new List<Character>(hintSize);
            this[0] = new Character { Glyph = 0x20, Options = Character.DefaultOptions };
        }

        /// <summary>
        /// Set a character value at a specified position. If the line is not long enough it is extended with blanks.
        /// </summary>
        /// <param name="pos">Position to set.</param>
        /// <param name="ch">Character value.</param>
        public void Set(int pos, Character ch)
        {
            this.Extend(pos);
            this.chars[pos] = ch;
        }

        /// <summary>
        /// Get a character value at the specified position. If the line is not long enough an empty (space) character with the properties of the last character is returned.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Character Get(int pos)
        {
            if (pos < this.chars.Count)
            {
                return this.chars[pos];
            }

            // for short lines we can lazily keep the attributes (specifically background) from whatever our last character was, assuming we had one.
            var ch = this.chars.Count > 0 ? this.chars[this.chars.Count - 1] : new Character { Options = Character.DefaultOptions };
            ch.Glyph = 0x20;
            return ch;
        }

        /// <summary>
        /// Set the glyph value at the given position while retaining existing properties.
        /// </summary>
        /// <param name="pos">Position in line.</param>
        /// <param name="glyph">Value to store.</param>
        public void SetGlyph(int pos, int glyph)
        {
            this.Extend(pos);

            var current = this.chars[pos];
            current.Glyph = glyph;
            this.chars[pos] = current;
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
