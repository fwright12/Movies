#if DEBUG
using Xamarin.Forms;

namespace Movies
{
    public class DebugConfig
    {
        public static bool FilterPerformanceTest = false;

        public static bool AllowTMDbRequests = false;
        public static bool AllowTMDbImages = false;
        public static bool LogWebRequests = !FilterPerformanceTest && true;

        public static bool AllowLiveRequests = false;
        public static int SimulatedDelay { get; set; } = 0;

        public static bool ClearLocalWebCache = FilterPerformanceTest || false;

        public static void Breakpoint()
        {
            ;
        }
    }
}
#endif
