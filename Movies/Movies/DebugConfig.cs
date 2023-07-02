#if DEBUG
namespace Movies
{
    public class DebugConfig
    {
        public const bool ALLOW_TMDB_REQUESTS = false;
        public const bool ALLOW_TMDB_IMAGES = false;
        public static bool LOG_WEB_REQUESTS = false;

        public static bool AllowLiveRequests = false;
        public static bool BreakOnRequest = true;
        public static int SimulatedDelay { get; set; } = 0;

        public static void Breakpoint(object arg1)
        {
            if (BreakOnRequest)
                ;
        }

        public static void Breakpoint(params object[] args)
        {
            if (BreakOnRequest)
                ;
        }
    }
}
#endif
