namespace TinyPG.Compiler
{
    public enum RuleType
    {
        //Production = 0, // production rule
        /// <summary>
        /// represents a terminal symbol
        /// </summary>
        Terminal = 1,

        /// <summary>
        /// represents a non terminal symbol
        /// </summary>
        NonTerminal = 2,

        /// <summary>
        /// represents the | symbol, choose between one or the other symbol or subrule (OR)
        /// </summary>
        Choice = 3, // |

        /// <summary>
        /// puts two symbols or subrules in sequental order (AND)
        /// </summary>
        Concat = 4, // <whitespace>

        /// <summary>
        /// represents the ? symbol
        /// </summary>
        Option = 5, // ?

        /// <summary>
        /// represents the * symbol
        /// </summary>
        ZeroOrMore = 6, // *

        /// <summary>
        /// represents the + symbol
        /// </summary>
        OneOrMore = 7 // +
    }
}