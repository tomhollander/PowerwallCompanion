using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerwallCompanion
{
    public class PropertyAnimator
    {
        public static async Task AnimatePropertyAsync<T>(
            Action<T> setter,
            T fromValue,
            T toValue,
            T finalValue,
            TimeSpan duration,
            Func<double, double> easingFunction = null)
        {
            easingFunction ??= (t) => t; // Linear by default

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < duration)
            {
                var progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                var easedProgress = easingFunction(progress);

                if (typeof(T) == typeof(double))
                {
                    var from = Convert.ToDouble(fromValue);
                    var to = Convert.ToDouble(toValue);
                    var current = from + (to - from) * easedProgress;
                    setter((T)(object)current);
                }

                await Task.Delay(16); // ~60 FPS
            }

            setter(finalValue); // Ensure final value is set
        }
    }
}
