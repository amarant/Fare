/*
 * Copyright 2009 Wilfred Springer
 * http://github.com/moodmosaic/Fare/
 * Original Java code:
 * http://code.google.com/p/xeger/
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fare
{
    /// <summary>
    /// An object that will generate text from a regular expression. In a way, 
    /// it's the opposite of a regular expression matcher: an instance of this class
    /// will produce text that is guaranteed to match the regular expression passed in.
    /// </summary>
    public class Xeger
    {
        private const RegExpSyntaxOptions AllExceptAnyString = RegExpSyntaxOptions.All & ~RegExpSyntaxOptions.Anystring;

        private Automaton automaton;
        private readonly Random random;
        private readonly int? _minLength;
        private readonly int? _maxLength;
        private RegExp regExp;


        /// <summary>
        /// Initializes a new instance of the <see cref="Xeger" /> class.
        /// </summary>
        /// <param name="regex">The regex.</param>
        /// <param name="random">The random.</param>
        /// <param name="minLength">The minimum length.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <exception cref="System.ArgumentNullException">
        /// regex
        /// or
        /// random
        /// </exception>
        public Xeger(string regex, Random random, int? minLength, int? maxLength)
        {
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            if (random == null)
            {
                throw new ArgumentNullException("random");
            }

            regExp = new RegExp(regex, AllExceptAnyString);
            this.random = random;
            _minLength = minLength;
            _maxLength = maxLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xeger"/> class.
        /// </summary>
        /// <param name="regex">The regex.</param>
        /// <param name="random">The random.</param>
        public Xeger(string regex, Random random)
            : this(regex, random, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xeger"/> class.
        /// </summary>
        /// <param name="regex">The regex.</param>
        public Xeger(string regex)
            : this(regex, new Random())
        {
        }

        /// <summary>
        /// Add a regular expression that will make an intersection with the current one.
        /// </summary>
        /// <param name="regex">The regex.</param>
        public void AddIntersection(string regex)
        {
            var exp = new RegExp(regex, AllExceptAnyString);
            this.regExp = RegExp.MakeIntersection(regExp, exp);
            this.automaton = null;
        }

        /// <summary>
        /// Generates a random String that is guaranteed to match the regular expression passed to the constructor.
        /// </summary>
        /// <returns></returns>
        public string Generate()
        {
            if (automaton == null)
            {
                this.automaton = regExp.ToAutomaton();
            }

            var builder = new StringBuilder();
            this.Generate(builder, automaton.Initial);
            return builder.ToString().TrimStart('^').TrimEnd('$');
        }

        /// <summary>
        /// Generates a random number within the given bounds.
        /// </summary>
        /// <param name="min">The minimum number (inclusive).</param>
        /// <param name="max">The maximum number (inclusive).</param>
        /// <param name="random">The object used as the randomizer.</param>
        /// <returns>A random number in the given range.</returns>
        private static int GetRandomInt(int min, int max, Random random)
        {
            int dif = max - min;
            double number = random.NextDouble();
            return min + (int)Math.Round(number * dif);
        }

        private void Generate(StringBuilder builder, State state)
        {
            var transitions = state.GetSortedTransitions(true);
            if (transitions.Count == 0)
            {
                if (!state.Accept)
                {
                    throw new InvalidOperationException("state");
                }

                return;
            }

            int nroptions = state.Accept ? transitions.Count : transitions.Count - 1;
            var minOption = 0;
            if (_minLength != null
                && builder.Length < _minLength)
            {
                minOption = 1;
            }
            if (_maxLength != null
                && builder.Length == _maxLength)
            {
                minOption = 0;
                nroptions = 0;
            }
            int option = Xeger.GetRandomInt(minOption, nroptions, random);
            if (state.Accept && option == 0)
            {
                // 0 is considered stop.
                return;
            }

            // Moving on to next transition.
            Transition transition = transitions[option - (state.Accept ? 1 : 0)];
            this.AppendChoice(builder, transition);
            Generate(builder, transition.To);
        }

        private void AppendChoice(StringBuilder builder, Transition transition)
        {
            var c = (char)Xeger.GetRandomInt(transition.Min, transition.Max, random);
            builder.Append(c);
        }
    }
}