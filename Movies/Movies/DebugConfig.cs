#if DEBUG
using Xamarin.Forms;

namespace Movies
{
    public class DebugConfig
    {
        public static bool FilterPerformanceTest { get; set; } = false;

        public static bool AllowLiveRequests { get; set; } = false;
        public static bool LogWebRequests { get; set; } = true && !FilterPerformanceTest;
        public static bool AllowTMDbRequests { get; set; } = false;
        public static bool AllowTMDbImages { get; set; } = false;

        public static bool ClearLocalWebCache { get; set; } = false || FilterPerformanceTest;

        public static int SimulatedDelay { get; set; } = 0;

        public static void Breakpoint()
        {
            ;
        }
    }
}
#endif
