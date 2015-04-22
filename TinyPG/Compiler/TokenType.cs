namespace TinyPG.Compiler
{
    public enum TokenType
    {

        //Non terminal tokens:
        _NONE_  = 0,
        _UNDETERMINED_= 1,

        //Non terminal tokens:
        Start   = 2,
        Directive= 3,
        NameValue= 4,
        ExtProduction= 5,
        Attribute= 6,
        Params  = 7,
        Param   = 8,
        Production= 9,
        Rule    = 10,
        Subrule = 11,
        ConcatRule= 12,
        Symbol  = 13,

        //Terminal tokens:
        BRACKETOPEN= 14,
        BRACKETCLOSE= 15,
        CODEBLOCK= 16,
        COMMA   = 17,
        SQUAREOPEN= 18,
        SQUARECLOSE= 19,
        ASSIGN  = 20,
        PIPE    = 21,
        SEMICOLON= 22,
        UNARYOPER= 23,
        IDENTIFIER= 24,
        INTEGER = 25,
        DOUBLE  = 26,
        HEX     = 27,
        ARROW   = 28,
        DIRECTIVEOPEN= 29,
        DIRECTIVECLOSE= 30,
        EOF     = 31,
        STRING  = 32,
        WHITESPACE= 33,
        COMMENTLINE= 34,
        COMMENTBLOCK= 35
    }
}