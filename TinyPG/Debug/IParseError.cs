namespace TinyPG.Debug
{
    public interface IParseError
    {
        int Code { get; }
        int Line { get; }
        int Column { get; }
        int Position { get; }
        int Length { get; }
        string Message { get; }
    }
}