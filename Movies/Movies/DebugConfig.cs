#if DEBUG
using Xamarin.Forms;

namespace Movies
{
    public class DebugConfig
    {
        public static bool AllowTMDbRequests = false;
        public static bool AllowTMDbImages = false;
        public static bool LogWebRequests = true;

        public static bool AllowLiveRequests = false;
        public static bool BreakOnRequest = true;
        public static int SimulatedDelay { get; set; } = 0;

        public static void Breakpoint()
        {
            if (BreakOnRequest)
                ;
        }
    }
}
#endif
