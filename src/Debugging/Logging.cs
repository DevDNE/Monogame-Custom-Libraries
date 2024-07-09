using System;

namespace Debugging
{
  public class Logging
  {
    public static void LogMessage(string message, LogLevel level)
    {
      string formattedMessage = $"[{DateTime.Now}] [{level}] {message}";
      Console.WriteLine(formattedMessage);
    }

    public static void LogPerformanceMetrics(float fps, float memoryUsage)
    {
      string metrics = $"FPS: {fps}, Memory Usage: {memoryUsage} MB";
      Console.WriteLine(metrics);
    }
  }

  public enum LogLevel
  {
    Debug,
    Info,
    Warning,
    Error
  }
}