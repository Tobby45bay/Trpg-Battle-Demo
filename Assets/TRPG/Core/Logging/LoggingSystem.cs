namespace TRPG.Core.Logging
{
    public enum LogLevel
    {
        Trace,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Core logging abstraction.
    /// Core systems depend on this interface only.
    /// </summary>
    public interface ICoreLogger
    {
        void Log(LogLevel level, string category, string message);
    }
}