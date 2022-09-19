using System;
using System.Globalization;
using System.IO;

namespace MiniSpotify.Source.Logging
{
    public class FileLogger
    {
        public void LogError(Exception ex, string info = null)
        {
            var currentDate = DateTime.Now;
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentDate.Month);

            var root = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            var yearPath = Path.Combine(root, currentDate.Year.ToString());
            var monthPath = Path.Combine(yearPath, currentDate.Year, "-", monthName);
            var errorFilePath = Path.Combine(monthPath, "ErrorLogs-", String.Format("{0:d-M-yyyy}", currentDate.Date), ".txt");

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            if (!Directory.Exists(yearPath))
            {
                Directory.CreateDirectory(yearPath);
            }
            if (!Directory.Exists(monthPath))
            {
                Directory.CreateDirectory(monthPath);
            }
            if (!File.Exists(errorFilePath))
            {
                using(var _ = File.Create(errorFilePath));
            }
            using (var sw = File.AppendText(errorFilePath))
            {
                sw.WriteLine();
                sw.WriteLine("=============Error Logging ===========");
                sw.WriteLine("===========Start============= " + currentDate);
                if( !String.IsNullOrWhitespace(info) ) sw.WriteLine($"Extra information: \n{info}");
                sw.WriteLine($"Error Message: \n{ex.Message}");
                sw.WriteLine($"Stack Trace: \n{ex.StackTrace}");
                sw.WriteLine("===========End============= " + currentDate);
                sw.WriteLine();
            }
        }
    }
}
