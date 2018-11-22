using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBuffer
{
    public sealed class Line : IEnumerable<Character>
    {
        public static readonly Line Empty = new Line();

        private readonly List<Character> chars = new List<Character>();
        public int Length => this.chars.Count;

        /// <summary>
        /// Set a character value at a specified position. If the line is not long enough it is extended with blanks.
        /// </summary>
        /// <param name="pos">Position to set.</param>
        /// <param name="ch">Character value.</param>
        public void Set(int pos, Character ch)
        {
            if (this.chars.Count <= pos)
            {
                this.Extend(pos);
            }
            this.chars[pos] = ch;
        }

        public void Clear()
        {
            this.chars.Clear();
        }

        private void Extend(int pos)
        {
            // XXX: not efficient.
            while (this.chars.Count <= pos)
            {
                this.chars.Add(new Character { Glyph = ' ' });
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
