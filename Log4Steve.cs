using System;
using System.IO;
using System.Reflection;
using System.Text;
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

        private string outputPath { get; set; }
        public string PathRelativeToAssembly { get; set; }
        public string AbsolutePath { get; set; }
        public string DatePattern { get; set; }
        public int GiveUpWaitingForReleaseInMilliseconds { get; set; }
        public int ExceptionInnerDepthLimit { get; set; }

        public class Destinations
        {
            public bool File { get; set; }
            public bool Console { get; set; }
        }
        public Destinations destinations { get; set; }

        public Log4Steve(string pathRelativeToAssembly = "",
                         string absolutePath = "",
                         string datePattern = "dd.MM.yyyy'.txt'",
                         int giveUpWaitingForReleaseInMilliseconds = 3000,
                         int exceptionInnerDepthLimit = 10,
                         bool outputToFile = true,
                         bool outputToConsole = true)
        {
            this.PathRelativeToAssembly = pathRelativeToAssembly;
            this.AbsolutePath = absolutePath;
            this.DatePattern = datePattern;
            this.GiveUpWaitingForReleaseInMilliseconds = giveUpWaitingForReleaseInMilliseconds;
            this.ExceptionInnerDepthLimit = exceptionInnerDepthLimit;
            this.destinations = new Destinations(){ File = outputToFile, Console = outputToConsole };

            // determine output path
            this.outputPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(this.PathRelativeToAssembly) && string.IsNullOrEmpty(this.AbsolutePath))
            {
                // nothing specified just log into this assembly's location
            }
            else if (!string.IsNullOrEmpty(this.AbsolutePath))
            {
                // absolute path - first precedence
                this.outputPath = Path.GetDirectoryName(this.AbsolutePath);
            }
            else if (!string.IsNullOrEmpty(this.PathRelativeToAssembly))
            {
                // relative path
                this.outputPath = Path.Combine(this.outputPath, this.PathRelativeToAssembly);
            }

            // make sure directory is there
            if (!Directory.Exists(this.outputPath))
                Directory.CreateDirectory(this.outputPath);
        }

        public void Custom(string level, string message)
        {
            this.Log(level, message);
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

        public void Error(Exception ex)
        {
            this.Log("ERROR", ex.Message);
            this.GetInner(ex);
        }

        public void Inner(Exception ex)
        {
            this.GetInner(ex);
        }

        private void GetInner(Exception ex, int currentDepth = 0)
        {
            if (ex.InnerException == null || currentDepth > this.ExceptionInnerDepthLimit) return;
            currentDepth++;
            this.Log("INNER", ex.InnerException.Message);
            this.GetInner(ex.InnerException, currentDepth);
        }

        public void Fatal(string message)
        {
            this.Log("FATAL", message);
        }

        private void Log(string level, string message)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(level))
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + level + " | " + "- ");
            sb.Append(message);
            
            if (this.destinations.File)
                Task.Run(() => this.Log(sb.ToString()));

            if (this.destinations.Console)
                Console.WriteLine(sb.ToString());
        }

        private async Task Log(string message)
        {
            this.LogToFile(message);
        }

        private void LogToFile(string message)
        {
            try
            {
                var ok = false;
                var start = DateTime.Now;
                var path = Path.Combine(this.outputPath, DateTime.Today.ToString(this.DatePattern));

                while (!ok && DateTime.Now < start.AddMilliseconds(GiveUpWaitingForReleaseInMilliseconds))
                {
                    try
                    {
                        using (var sw = new StreamWriter(path, true))
                            sw.WriteLine(message);

                        ok = true;
                    }
                    catch (IOException ex)
                    {
                        // might be locked, try again
                        var ignore = ex;
                    }
                }
            }
            catch (Exception ex)
            {
                // pokemon catch and be annoyingly silent like log4net
                var ignore = ex;
            }
        }
    }
}
