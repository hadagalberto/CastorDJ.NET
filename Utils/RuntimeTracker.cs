using System.Diagnostics;
using System.Text;

namespace CastorDJ.Utils
{
    public class RuntimeTracker
    {
        private static readonly Lazy<RuntimeTracker> instance = new(() => new RuntimeTracker());

        public static RuntimeTracker Instance => instance.Value;

        private readonly Stopwatch _stopwatch;

        private RuntimeTracker()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public TimeSpan GetElapsedTime()
        {
            return _stopwatch.Elapsed;
        }

        public long GetElapsedMilliseconds()
        {
            return _stopwatch.ElapsedMilliseconds;
        }

        public string GetElapsedTimeInPortuguese()
        {
            var elapsed = _stopwatch.Elapsed;
            var sb = new StringBuilder();

            if (elapsed.Days > 0)
            {
                sb.Append($"{elapsed.Days} dia{(elapsed.Days > 1 ? "s" : "")}, ");
            }

            if (elapsed.Hours > 0)
            {
                sb.Append($"{elapsed.Hours} hora{(elapsed.Hours > 1 ? "s" : "")}, ");
            }

            if (elapsed.Minutes > 0)
            {
                sb.Append($"{elapsed.Minutes} minuto{(elapsed.Minutes > 1 ? "s" : "")}, ");
            }

            if (elapsed.Seconds > 0)
            {
                sb.Append($"{elapsed.Seconds} segundo{(elapsed.Seconds > 1 ? "s" : "")}. ");
            }

            return sb.ToString();
        }
    }
}
