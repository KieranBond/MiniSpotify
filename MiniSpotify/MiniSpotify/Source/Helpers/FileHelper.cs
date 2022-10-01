using System;
using System.IO;
using MiniSpotify.Source.Logging;

namespace MiniSpotify.HelperScripts
{
    public static class FileHelper
    {
        public static string GetFileText(string a_filePath)
        {
            string path = Environment.CurrentDirectory + a_filePath;
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Load File at: " + a_filePath);
                Console.WriteLine(e.Message);
                FileLogger logs = new FileLogger();
                logs.LogError(e);
                return null;
            }
        }

        public static bool WriteFileText(string a_filePath, string a_text, bool a_overwrite = true)
        {
            string path = Environment.CurrentDirectory + a_filePath;
            try
            {
                if(a_overwrite || !File.Exists(a_filePath) || File.Exists(a_filePath) && File.ReadAllText(a_filePath).Length <= 0)//Write
                {
                    File.WriteAllText(path, a_text);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Load File at: " + a_filePath);
                Console.WriteLine(e.Message);
                FileLogger logs = new FileLogger();
                logs.LogError(e);
                return false;
            }
        }
    }
}
