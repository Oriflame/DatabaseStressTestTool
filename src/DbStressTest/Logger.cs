using System;

namespace DbStressTest
{
    /// <summary>
    /// thread safe logging/output
    /// </summary>
    internal static class Logger
    {
        private static readonly object LckObj = new object();

        /// <summary>
        /// thread safe write message
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        internal static void WriteInfo(string format, params object[] args)
        {
            lock (LckObj)
            {
                //this was sometimes a place where we get deadlocks; so we need to mutex this..
                Console.WriteLine(format, args);
            }
        }

        internal static void WriteInfo(object o)
        {
            if (o == null)
                WriteInfo("");
            else
                WriteInfo("{0}", o);
        }

        internal static void WriteInfo(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                WriteInfo("", null);
            else 
                WriteInfo("{0}", msg);
        }
    }
}
