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
        /// Throw another character on to the end of the line.
        /// </summary>
        /// <param name="ch"></param>
        public void Append(Character ch)
        {
            this.chars.Add(ch);
        }

        /// <summary>
        /// Set a character value at a specified position. If the line is not long enough it is extended with blanks.
        /// </summary>
        /// <param name="pos">Position to set.</param>
        /// <param name="ch">Character value.</param>
        public void Set(int pos, Character ch)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Character> GetEnumerator()
        {
            return chars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return chars.GetEnumerator();
        }
    }
}
