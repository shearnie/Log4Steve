using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

/*
 * I'm sorry, but log4net and the way the Java world does things hurts my brain.
 * Here's my DIY implementation.
 * 
 * https://github.com/shearnie/Log4Steve
 */

namespace Log4NetSucksToConfigure
{
    public class Log4Steve
    {
        // basic rolling file logging with date as path

        public string PathRelativeToAssembly { get; set; }
        public string AbsolutePath { get; set; }
        public string DatePattern { get; set; }
        public int GiveUpWaitingForReleaseInMilliseconds { get; set; }

        public Log4Steve(string pathRelativeToAssembly = "",
                         string absolutePath = "",
                         string datePattern = "dd.MM.yyyy'.txt'",
                         int giveUpWaitingForReleaseInMilliseconds = 3000)
        {
            this.PathRelativeToAssembly = pathRelativeToAssembly;
            this.AbsolutePath = absolutePath;
            this.DatePattern = datePattern;
            this.GiveUpWaitingForReleaseInMilliseconds = giveUpWaitingForReleaseInMilliseconds;
        }

        public void Info(string message)
        {
            this.Log("INFO", message);
        }

        public void Debug(string message)
        {
            this.Log("DEBUG", message);
        }

        public void Warn(string message)
        {
            this.Log("WARN", message);
        }

        public void Error(string message)
        {
            this.Log("ERROR", message);
        }

        public void Fatal(string message)
        {
            this.Log("FATAL", message);
        }

        private void Log(string level, string message)
        {
            Task.Run(() => 
                this.Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + level + " | " + "- " + message));
        }

        private async Task Log(string message)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(this.PathRelativeToAssembly) && string.IsNullOrEmpty(this.AbsolutePath))
            {
                // nothing specified just log into this assembly's location
            }
            else if (!string.IsNullOrEmpty(this.AbsolutePath))
            {
                // absolute path - first precedence
                path = this.AbsolutePath;
            }
            else if (!string.IsNullOrEmpty(this.PathRelativeToAssembly))
            {
                // relative path

                path = Path.Combine(path, this.PathRelativeToAssembly);
            }

            var outputPath = Path.Combine(path, DateTime.Today.ToString(this.DatePattern));

            try
            {
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var ok = false;
                var start = DateTime.Now;

                while (!ok && DateTime.Now < start.AddMilliseconds(GiveUpWaitingForReleaseInMilliseconds))
                {
                    try
                    {
                        using (var sw = new StreamWriter(outputPath, true))
                            sw.WriteLine(message);
                        ok = true;
                    }
                    catch (IOException ex)
                    {
                        // might be locked, try again
                    }
                }
            }
            catch (Exception ex)
            {
                // pokemon catch and be annoyingly silent like log4net
            }
        }
    }
}
