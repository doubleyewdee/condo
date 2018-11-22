namespace ConsoleBuffer
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public sealed class Line : IEnumerable<Character>
    {
        private readonly List<Character> chars;

        public Line(Line previous)
        {
            var hintSize = 80;
            // our first character should inherit attributes of the last line's character.
            var lastCh = new Character();
            if (previous != null)
            {
                lastCh = previous.chars[previous.chars.Count - 1];
                hintSize = previous.chars.Count;
            }
            lastCh.Glyph = 0x20;

            this.chars = new List<Character>(hintSize)
            {
                lastCh
            };
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

            var ch = this.chars[this.chars.Count - 1];
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

        public void Clear()
        {
            for (var x = 0; x < this.chars.Count; ++x)
            {
                this.SetGlyph(x, 0x20);
            }
        }

        private void Extend(int pos)
        {
            var newChar = this.chars[this.chars.Count - 1];
            newChar.Glyph = 0x20;
            while (this.chars.Count <= pos)
            {
                this.chars.Add(newChar);
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
