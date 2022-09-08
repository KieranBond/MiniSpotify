using System;
using System.Globalization;
using System.IO;

namespace MiniSpotify.Source.Logging
{
    public class FileLogger
    {
        public void logError(Exception ex)
        {
            var currentDate = DateTime.Now;
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentDate.Month);

            var root = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            var yearPath = root + "\\" + currentDate.Year.ToString() + "\\";
            var MonthPath = yearPath + currentDate.Year + "-" + monthName + "\\";
            var errorFile = MonthPath + "ErrorLogs-" + String.Format("{0:d-M-yyyy}", currentDate.Date) + ".txt";

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            if (!Directory.Exists(yearPath))
            {
                Directory.CreateDirectory(yearPath);
            }
            if (!Directory.Exists(MonthPath))
            {
                Directory.CreateDirectory(MonthPath);
            }
            if (!File.Exists(errorFile))
            {
                FileStream fs = File.Create(errorFile);
                fs.Close();
            }
            using (StreamWriter sw = File.AppendText(errorFile))
            {
                sw.WriteLine();
                sw.WriteLine("=============Error Logging ===========");
                sw.WriteLine("===========Start============= " + currentDate);
                sw.WriteLine("Error Message: ");
                sw.WriteLine(ex.Message);
                sw.WriteLine("Stack Trace: ");
                sw.WriteLine(ex.StackTrace);
                sw.WriteLine("===========End============= " + currentDate);
                sw.WriteLine();
            }
        }
    }
}
