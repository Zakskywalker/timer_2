using System;
using System.Globalization;

namespace Timer2.Core.Calculations
{
    public static class TimeCalculator
    {
        /// <summary>
        /// Calculates whole hours and the percent of the hour (e.g., 4h 15m => 4.25 hours).
        /// </summary>
        public static double CalculateDecimalHours(TimeSpan time)
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            return Math.Round(time.TotalHours, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Formats decimal hours as string, e.g. 4.25 hrs
        /// </summary>
        public static string FormatDecimalHours(TimeSpan time)
        {
            double dec = CalculateDecimalHours(time);
            return $"{dec.ToString("0.00", CultureInfo.InvariantCulture)} hrs.";
        }

        /// <summary>
        /// Formats as H:P (e.g., 4.25 hrs or 4.25)
        /// </summary>
        public static string FormatHP(TimeSpan time, bool includeUnit = true)
        {
            double dec = CalculateDecimalHours(time);
            string formatted = dec.ToString("0.00", CultureInfo.InvariantCulture);
            return includeUnit ? $"{formatted} hrs" : formatted;
        }

        /// <summary>
        /// Formats as H:M (e.g., 4 Hrs. 15 Min. or 4 Hrs. 15 Min. 00 Sec.)
        /// </summary>
        public static string FormatHM(TimeSpan time, bool includeSeconds = false)
        {
            int hours = (int)time.TotalHours;
            int minutes = time.Minutes;
            int seconds = time.Seconds;

            if (includeSeconds)
            {
                return $"{hours} Hrs. {minutes} Min. {seconds} Sec.";
            }
            return $"{hours} Hrs. {minutes} Min.";
        }

        /// <summary>
        /// Formats the run time line as seen in logs and UI console:
        /// "*Total time in this run: 0 hours, 20 minutes, 39 seconds. (0.34 hrs)"
        /// </summary>
        public static string FormatRunLine(TimeSpan runTime)
        {
            int hours = (int)runTime.TotalHours;
            int minutes = runTime.Minutes;
            int seconds = runTime.Seconds;
            double dec = CalculateDecimalHours(runTime);

            return $"*Total time in this run: {hours} hours, {minutes} minutes, {seconds} seconds. ({dec.ToString("0.00", CultureInfo.InvariantCulture)} hrs)";
        }

        /// <summary>
        /// Formats total time line matching the original log format plus clean decimal hours:
        /// "Total time is: 4 Hrs. 57 Min. 42 Sec.  or (Rounded up): 4.96 hrs."
        /// </summary>
        public static string FormatTotalLine(TimeSpan totalTime)
        {
            int hours = (int)totalTime.TotalHours;
            int minutes = totalTime.Minutes;
            int seconds = totalTime.Seconds;
            double dec = CalculateDecimalHours(totalTime);

            return $"Total time is: {hours} Hrs. {minutes} Min. {seconds} Sec.  or (Rounded up): {dec.ToString("0.00", CultureInfo.InvariantCulture)} hrs.";
        }

        /// <summary>
        /// Notification tray tooltip line matching image 2:
        /// "Timer - Run 7: 5 Hrs. 36 Min. 16 Sec. | 5.60 hrs."
        /// </summary>
        public static string FormatTrayTooltip(int runNumber, TimeSpan runTime, TimeSpan totalTime, bool isPaused)
        {
            int rH = (int)runTime.TotalHours;
            int rM = runTime.Minutes;
            int rS = runTime.Seconds;
            double totDec = CalculateDecimalHours(totalTime);
            double dec = CalculateDecimalHours(totalTime);

            string pausedSuffix = isPaused ? " [Paused]" : "";
            //return $"Timer - Run {runNumber}: {rH} Hrs. {rM} Min. {rS} Sec. | {totDec.ToString("0.00", CultureInfo.InvariantCulture)} hrs.{pausedSuffix} | Total Time is: {hours} Hrs. {minutes} Min. {seconds} Sec.  or (Rounded up): {dec.ToString("0.00", CultureInfo.InvariantCulture)} hrs.";
            return $"Timer - Run {runNumber}: {rH}:{rM}:{rS} | Total(Time): {dec.ToString("0.00", CultureInfo.InvariantCulture)} hrs.";

        }
    }
}
