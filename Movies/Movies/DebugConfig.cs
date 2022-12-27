#if DEBUG
namespace Movies
{
    public class DebugConfig
    {
        public const bool ALLOW_TMDB_REQUESTS = false;
        public const bool ALLOW_TMDB_IMAGES = false;

        public static bool AllowLiveRequests = true;
        public static bool BreakOnRequest = true;
        public static int SimulatedDelay = 0;

        public static void Breakpoint(params object[] args)
        {
            if (BreakOnRequest)
                ;
        }
    }
}
#endif
