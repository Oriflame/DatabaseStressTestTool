// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using CommandLine;
using CommandLine.Text;

namespace DbStressTest
{
    internal sealed class Options
    {
        private int _threadsCount;

        [Option('c', "connection-string", DefaultValue = "Data Source=.\\SQLEXPRESS;Trusted_Connection=True;",
            HelpText = "Connection to the database you want to stress...")]
        public string ConnectionString { get; set; }

        [Option('t', "threads-count", DefaultValue = 0,
            HelpText = "Number of threads to use. Default 0 = number of cores * 2")]
        public int ThreadsCount
        {
            get
            {
                if (_threadsCount <= 0)
                {
                    _threadsCount = (int)(Environment.ProcessorCount * 12.5); //this seems to return logic processors (8 on my 4 cores * 2 hyper threaded = 8 graphs in task manager)
                    //_threadsCount = 0;
                    //foreach (var item in new ManagementObjectSearcher("Select NumberOfCores  from Win32_Processor").Get())
                    //{
                    //    _threadsCount += int.Parse(item["NumberOfCores"].ToString())*2;
                    //}
                }

                return _threadsCount;
            }
            set { _threadsCount = value; }
        }

        [Option('d', "test-duration", DefaultValue = 0, MutuallyExclusiveSet = "DURATION",
            HelpText = "How long the test should run (in seconds). Default 0 = infinity :) wait for enter. Do not combine with one-time-pass.")]
        public int TestDurationSeconds { get; set; }

        [Option('o', "one-time-pass", DefaultValue = false, MutuallyExclusiveSet = "1TIMEPASS",
            HelpText = "How the test should run: each worker will do only one pass. Default: unlimited pass (how many work done test). If set to true: winner is the faster (be sure to set threadsCount to some high value). Do not combine with duration.")]
        public bool OneTimePass { get; set; }

        [Option('m', "monitor-db", DefaultValue = false,
            HelpText = "Should we reserve one connection for monitoring the DB?")]
        public bool MonitorDb { get; set; }

        [Option('w', "worker-threads", DefaultValue = 0,
            HelpText = "sets ThreadPool.SetMin/MaxThreads(workerThreads) if above 0.")]
        public int ThreadPoolWorkerThreads { get; set; }


        [Option('p', "completion-threads", DefaultValue = 0,
            HelpText = "sets ThreadPool.SetMin/MaxThreads(completionThreads) if above 0.")]
        public int ThreadPoolCompletionPortThreads { get; set; }

        [Option('s', "synchronously", DefaultValue = false,
            HelpText = "Do stress test synchronously? That is, instead of await OpenAsync use juts Open() (not via Tasks, but via multiple threads)?")]
        public bool Synchronously { get; set; }

        [Option('e', "sql-to-execute", DefaultValue = BatchType.GetNewId,
            HelpText = "sql to execute (enum). [GetNewID]: CPU stress. [LongRun]: simulation of long run. [RandomMix]: random mix of NewId, LongRun and NoSql actions.")]
        public BatchType Sql2Execute { get; set; }

        [Option('x', "export-to-file", DefaultValue = null,
        HelpText = "should we export to file? If so, specify the name of the file")]
        public string Export2File { get; set; }

        [Option('l', "long-run-delay", DefaultValue = 10,
        HelpText = "how long should LongRun wait ins seconds (1-60)")]
        public int LongRunDelaySeconds { get; set; }

        [Option('n', "max-number-of-db", DefaultValue = 50,
        HelpText = "Maximum number of databases for random access")]
        public int MaxNumberOfDatabases { get; set; }

        [Option('r', "random-wait-time-ms", DefaultValue = 0,
            HelpText = "Random wait time between operations (before each operation). If it's 0 (default value), no time waited. In ms.")]
        public int RandomWaitTimeMs { get; set; }

        //public BatchType Sql2ExecuteEnum
        //{
        //    get
        //    {
        //        if (!_sql2ExecuteEnum.HasValue)
        //            _sql2ExecuteEnum = (BatchType)Enum.Parse(typeof(BatchType), Sql2Execute);
        //        return _sql2ExecuteEnum.Value;
        //    }
        //}
        //private BatchType? _sql2ExecuteEnum;

        public string TaskOrThreadsWordPlural
        {
            get { return Synchronously ? "threads" : "tasks"; }
        }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              current => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public override string ToString()
        {
            var b = new SqlConnectionStringBuilder(ConnectionString);
            return string.Format("ADO.NET pool size: {0}; threadCount: {1}; One-time-pass: {2}",
                b.MaxPoolSize,
                ThreadsCount,
                OneTimePass);
        }
    }

    public enum BatchType: short
    {
        /// <summary>
        /// CPU stress
        /// </summary>
        GetNewId = 0,
        /// <summary>
        /// simulation of long run
        /// </summary>
        LongRun = 1,
        /// <summary>
        /// simulation of CPU intensive op = no SQL operation at all.
        /// </summary>
        NoSqlAction = 2,
        /// <summary>
        /// random mix of all action (including NoSql action)
        /// </summary>
        RandomMix = short.MaxValue
    }
}
