namespace TinyPG.Highlighter
{
    public enum TokenType
    {

        //Non terminal tokens:
        _NONE_  = 0,
        _UNDETERMINED_= 1,

        //Non terminal tokens:
        Start   = 2,
        CommentBlock= 3,
        DirectiveBlock= 4,
        GrammarBlock= 5,
        AttributeBlock= 6,
        CodeBlock= 7,

        //Terminal tokens:
        WHITESPACE= 8,
        EOF     = 9,
        GRAMMARCOMMENTLINE= 10,
        GRAMMARCOMMENTBLOCK= 11,
        DIRECTIVESTRING= 12,
        DIRECTIVEKEYWORD= 13,
        DIRECTIVESYMBOL= 14,
        DIRECTIVENONKEYWORD= 15,
        DIRECTIVEOPEN= 16,
        DIRECTIVECLOSE= 17,
        ATTRIBUTESYMBOL= 18,
        ATTRIBUTEKEYWORD= 19,
        ATTRIBUTENONKEYWORD= 20,
        ATTRIBUTEOPEN= 21,
        ATTRIBUTECLOSE= 22,
        CS_KEYWORD= 23,
        VB_KEYWORD= 24,
        DOTNET_KEYWORD= 25,
        DOTNET_TYPES= 26,
        CS_COMMENTLINE= 27,
        CS_COMMENTBLOCK= 28,
        CS_SYMBOL= 29,
        CS_NONKEYWORD= 30,
        CS_STRING= 31,
        VB_COMMENTLINE= 32,
        VB_COMMENTBLOCK= 33,
        VB_SYMBOL= 34,
        VB_NONKEYWORD= 35,
        VB_STRING= 36,
        DOTNET_COMMENTLINE= 37,
        DOTNET_COMMENTBLOCK= 38,
        DOTNET_SYMBOL= 39,
        DOTNET_NONKEYWORD= 40,
        DOTNET_STRING= 41,
        CODEBLOCKOPEN= 42,
        CODEBLOCKCLOSE= 43,
        GRAMMARKEYWORD= 44,
        GRAMMARARROW= 45,
        GRAMMARSYMBOL= 46,
        GRAMMARNONKEYWORD= 47,
        GRAMMARSTRING= 48
    }
}