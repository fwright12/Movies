#if DEBUG
using System.Collections.Generic;
using Xamarin.Forms;

namespace Movies
{
    public class DebugConfig
    {
        public static bool FilterPerformanceTest { get; set; } = false;

        public static bool LogWebRequests { get; set; } = true && !FilterPerformanceTest;

        // Force live requests to be used for these api endpoints, even if a mock response exists
        public static List<object> UseLiveRequestsFor = new List<object>
        {
            //API.MOVIES.GET_DETAILS
            //API.TV.GET_DETAILS
        };
        // False means no live requests can be made. When true, live requests will only be made when a mock response is not available
        public static bool AllowLiveRequests { get; set; } = false || UseLiveRequestsFor.Count > 0;
        // Disable live requests only for TMDb endpoints
        public static bool AllowTMDbRequests { get; set; } = false || AllowLiveRequests;
        public static bool AllowTMDbImages { get; set; } = false;

        public static bool ClearLocalWebCache { get; set; } = false || FilterPerformanceTest;
        public static bool SimulateStaleCache { get; set; } = false;

        public static int SimulatedDelay { get; set; } = 0;

        public static void Breakpoint()
        {
            ;
        }
    }
}
#endif
