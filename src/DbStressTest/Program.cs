// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace DbStressTest
{
    class Program
    {
        private static readonly Options Options = new Options();
        private static Task _monitoringTask;
        private static readonly List<Task> StressDbTasks = new List<Task>();
        private static readonly List<Thread> StressDbThreads = new List<Thread>();
        private static readonly CsvResult Output = new CsvResult();
        private static readonly QueryRunStatistics QueryRunStatistics = new QueryRunStatistics(); //ConcurrentBag<QueryRunStatistics> QueryRunsMeasures = new ConcurrentBag<QueryRunStatistics>();
        private static bool _runTests = true;
        private static bool _startTests;
        private static Stopwatch _stopwatch;
        private static Random rnd = new Random();
        private static DbMonitoring _dbMonitoring;

        static int Main(string[] args)
        {
            if (Parser.Default.ParseArguments(args, Options))
            {
                QueryRunStatistics.Options = Options;
                return MainInternal();
            }
            return -1;
        }

        private static int MainInternal()
        {
            Logger.WriteInfo(@"DbStressTool {0} initialization", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            PerformanceCounters.SetUpPerformanceCounters();
            Output.Init(Options);
            if (Options.ThreadPoolWorkerThreads > 0 || Options.ThreadPoolCompletionPortThreads > 0)
            {
                ThreadPool.SetMinThreads(Options.ThreadPoolWorkerThreads, Options.ThreadPoolCompletionPortThreads);
                ThreadPool.SetMaxThreads(Options.ThreadPoolWorkerThreads, Options.ThreadPoolCompletionPortThreads);
            }
            int wtmin, ctmin, wtmax, ctmax;
            ThreadPool.GetMinThreads(out wtmin, out ctmin);
            ThreadPool.GetMaxThreads(out wtmax, out ctmax);
            Logger.WriteInfo(@"Running {0} {5} (min {1}/{2}, max {3}/{4})...", Options.ThreadsCount,
                wtmin, ctmin, wtmax, ctmax, Options.TaskOrThreadsWordPlural);
            if (!Options.Synchronously)
            {
                if (Options.ThreadsCount > wtmax)
                    Logger.WriteInfo(@"Warning: Task count > ThreadPool.MaxThreads (workerThreads), which is {0}...",
                        wtmax);
                if (Options.ThreadsCount > ctmax)
                    Logger.WriteInfo(@"Warning: Task count > ThreadPool.MaxThreads (completionThreads), which is {0}...",
                        ctmax);
            }
            _stopwatch = Stopwatch.StartNew();


            InitializeStressDbThreadsOrTasks();
            StartAllWorkers();

            _monitoringTask = MonitorQueue();


            var isInteractive = false;
            if (!Options.OneTimePass)
            {
                if (Options.TestDurationSeconds <= 0)
                {
                    Logger.WriteInfo(@"Press ENTER to end database stress");
                    Console.ReadLine();
                    isInteractive = true;
                }
                else
                {
                    Thread.Sleep(Options.TestDurationSeconds * 1000);
                }
            }
            else
            {
                // in case of one time pass..just wait till every thread/task ends...
                StopAllTasksAndThreads();
            }
            _stopwatch.Stop();
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.GetStatsBeforeStop(!Options.OneTimePass);
            }
            _runTests = false;
            WriteFinalInfoHeader();
            StopAllTasksAndThreads();
            _dbMonitoring.End();
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.Stop();
            }
            Logger.WriteInfo(QueryRunStatistics);
            if (isInteractive)
            {
                Logger.WriteInfo(@"Press ENTER to exit.");
                Console.ReadLine();
            }
            return 0;
        }

        private static void StartAllWorkers()
        {
            if (Options.OneTimePass)
                Logger.WriteInfo(@"One time pass measurement selected...the fun allready begun in init part!");
            else
                Logger.WriteInfo(@"Let's the fun begin!");
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.Start();
            }
            _startTests = true;
        }

        private static void WriteFinalInfoHeader()
        {
            Logger.WriteInfo(@"Total run time: {0:# ###.##} s (process time: {1:### ##0.#} s). Stopping {2}, please wait.",
                _stopwatch.ElapsedMilliseconds / 1000.0, Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds, Options.TaskOrThreadsWordPlural);
        }

        private static void StopAllTasksAndThreads()
        {
            try
            {
                var stopSw = Stopwatch.StartNew();
                foreach (var stressDbThread in StressDbThreads)
                {
                    while (stressDbThread.IsAlive)
                        Thread.Sleep(250);
                }
                Task.WaitAll(StressDbTasks.ToArray());
                stopSw.Stop();
                _runTests = false;
                _monitoringTask.Wait();
                Logger.WriteInfo(@"All {0} stopped in {1:### ##0.#} s.", Options.TaskOrThreadsWordPlural, stopSw.ElapsedMilliseconds / 1000d);
            }
            catch (AggregateException ex)
            {
                Logger.WriteInfo(@"ERROR: {0}", ex);
            }
        }

        private static void InitializeStressDbThreadsOrTasks()
        {
            _startTests = Options.OneTimePass;
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.InitStarted();
            }
            for (var i = 0; i < Options.ThreadsCount; i++)
            {
                if (Options.Synchronously)
                {
                    var t = new Thread(StressDb);
                    t.Start();
                    StressDbThreads.Add(t);
                }
                else
                {
                    var t = StressDbAsync();
                    StressDbTasks.Add(t);
                }
            }
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.InitFinished(Options.OneTimePass);
            }
            _dbMonitoring = new DbMonitoring(Options);
            _dbMonitoring.StartAsync();
            Logger.WriteInfo(@"Initialized in {0}", QueryRunStatistics.StatsAfterInit);
        }

        private static async Task MonitorQueue()
        {
            while (_runTests)
            {
                await Task.Delay(5000);
                lock (QueryRunStatistics)
                {
                    Logger.WriteInfo(@"Total errors so far {0:### ##0}...", QueryRunStatistics.TotalErrors);
                    Logger.WriteInfo(@"Total samples so far {0:### ### ##0} after {1:### ##0.#} s ({2:### ##0.#}/s)...", QueryRunStatistics.TotalSamples, QueryRunStatistics.TotalSeconds, QueryRunStatistics.CalculateSamplesPerSecond());
                    Logger.WriteInfo(@"Processor time so far {0:### ##0.#} s", Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds);
                    PerformanceCounters.WritePerformanceCounters();
                }
            }
        }

        private static async Task StressDbAsync()
        {
            long workDoneByUnit = 0;
            long workStartedByUnit = 0;
            while (!_startTests)
                await Task.Yield();
            while (_runTests)
            {
                int randomWaitTimeOption;
                var sql2ExecuteOption = GetSql2ExecuteOption(out randomWaitTimeOption);

                if (randomWaitTimeOption > 0)
                    await Task.Delay(rnd.Next(0, randomWaitTimeOption));

                if (sql2ExecuteOption == BatchType.NoSqlAction)
                    continue;

                workStartedByUnit++;
                var wasError = false;
                try
                {
                    var stopWatch = Stopwatch.StartNew();
                    long measure01NewConnTicks;
                    long measure02ConnOpenTicks;
                    long measure03CreateCommandTicks;
                    long measure04ExecuteReaderTicks;
                    long measure05ReaderReadAsyncEndTicks;
                    long measure06ReaderClosedTicks;
                    using (var conn = new SqlConnection(GetConnectionString()))
                    {
                        measure01NewConnTicks = stopWatch.ElapsedTicks;
                        await conn.OpenAsync();
                        measure02ConnOpenTicks = stopWatch.ElapsedTicks - measure01NewConnTicks;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = GetSql2Execute(sql2ExecuteOption);
                            //cmd.CommandTimeout 
                            measure03CreateCommandTicks = stopWatch.ElapsedTicks - measure02ConnOpenTicks;
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                measure04ExecuteReaderTicks = stopWatch.ElapsedTicks - measure03CreateCommandTicks;
                                await reader.ReadAsync();
                                measure05ReaderReadAsyncEndTicks = stopWatch.ElapsedTicks - measure04ExecuteReaderTicks;
                            }
                            measure06ReaderClosedTicks = stopWatch.ElapsedTicks - measure05ReaderReadAsyncEndTicks;
                        }
                    }
                    var measure07ConnectionClosedTicks = stopWatch.ElapsedTicks - measure06ReaderClosedTicks;
                    stopWatch.Stop();
                    workDoneByUnit++;
                    WriteStatistics(measure01NewConnTicks, measure02ConnOpenTicks, measure03CreateCommandTicks, measure04ExecuteReaderTicks, measure05ReaderReadAsyncEndTicks, measure06ReaderClosedTicks, measure07ConnectionClosedTicks);
                }
                catch (Exception ex)
                {
                    lock (QueryRunStatistics)
                    {
                        QueryRunStatistics.WasErrors(ex);
                    }
                    wasError = true;
                }
                if (wasError)
                    await Task.Yield();
                if (Options.OneTimePass)
                    break;
            }
            FinishTheWorker(workStartedByUnit, workDoneByUnit);
        }
        private static string GetConnectionString()
        {
            if (Options.ConnectionString.Contains("{"))
                return Options.ConnectionString.Replace("{nr}", rnd.Next(1, Options.MaxNumberOfDatabases).ToString());
            return Options.ConnectionString;
        }

        private static void StressDb()
        {
            long workDoneByUnit = 0;
            long workStartedByUnit = 0;
            while (!_startTests)
                Thread.Yield();
            while (_runTests)
            {
                int randomWaitTimeOption;
                var sql2ExecuteOption = GetSql2ExecuteOption(out randomWaitTimeOption);

                if (randomWaitTimeOption > 0)
                    Thread.Sleep(rnd.Next(0, randomWaitTimeOption));



                if (sql2ExecuteOption == BatchType.NoSqlAction)
                    continue;

                workStartedByUnit++;
                try
                {
                    var stopWatch = Stopwatch.StartNew();
                    long measure01NewConnTicks;
                    long measure02ConnOpenTicks;
                    long measure03CreateCommandTicks;
                    long measure04ExecuteReaderTicks;
                    long measure05ReaderReadAsyncEndTicks;
                    long measure06ReaderClosedTicks;
                    using (var conn = new SqlConnection(GetConnectionString()))
                    {
                        measure01NewConnTicks = stopWatch.ElapsedTicks;
                        conn.Open();
                        measure02ConnOpenTicks = stopWatch.ElapsedTicks - measure01NewConnTicks;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = GetSql2Execute(sql2ExecuteOption);
                            //cmd.CommandTimeout 
                            measure03CreateCommandTicks = stopWatch.ElapsedTicks - measure02ConnOpenTicks;
                            using (var reader = cmd.ExecuteReader())
                            {
                                measure04ExecuteReaderTicks = stopWatch.ElapsedTicks - measure03CreateCommandTicks;
                                reader.Read();
                                measure05ReaderReadAsyncEndTicks = stopWatch.ElapsedTicks - measure04ExecuteReaderTicks;
                            }
                            measure06ReaderClosedTicks = stopWatch.ElapsedTicks - measure05ReaderReadAsyncEndTicks;
                        }
                    }
                    var measure07ConnectionClosedTicks = stopWatch.ElapsedTicks - measure06ReaderClosedTicks;
                    stopWatch.Stop();
                    workDoneByUnit++;
                    WriteStatistics(measure01NewConnTicks, measure02ConnOpenTicks, measure03CreateCommandTicks,
                        measure04ExecuteReaderTicks, measure05ReaderReadAsyncEndTicks, measure06ReaderClosedTicks,
                        measure07ConnectionClosedTicks);
                }
                catch (StopExecutionException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    lock (QueryRunStatistics)
                    {
                        QueryRunStatistics.WasErrors(ex);
                    }
                }
                if (Options.OneTimePass)
                    break;
            }
            FinishTheWorker(workStartedByUnit, workDoneByUnit);
        }

        private static BatchType GetSql2ExecuteOption(out int randomWaitTimeOption)
        {
            var sql2ExecuteOption = Options.Sql2Execute;
            if (sql2ExecuteOption == BatchType.RandomMix)
            {
                sql2ExecuteOption = (BatchType)rnd.Next(3);
            }
            if (sql2ExecuteOption == BatchType.NoSqlAction)
                randomWaitTimeOption = Options.LongRunDelaySeconds * 1000;
            else
                randomWaitTimeOption = Options.RandomWaitTimeMs;
            return sql2ExecuteOption;
        }

        private static void WriteStatistics(long measure01NewConnTicks, long measure02ConnOpenTicks,
            long measure03CreateCommandTicks, long measure04ExecuteReaderTicks, long measure05ReaderReadAsyncEndTicks,
            long measure06ReaderClosedTicks, long measure07ConnectionClosedTicks)
        {
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.Measure01NewConnStats.Update(measure01NewConnTicks);
                QueryRunStatistics.Measure02ConnOpenStats.Update(measure02ConnOpenTicks);
                QueryRunStatistics.Measure03CreateCommandStats.Update(measure03CreateCommandTicks);
                QueryRunStatistics.Measure04ExecuteReaderStats.Update(measure04ExecuteReaderTicks);
                QueryRunStatistics.Measure05ReaderReadAsyncEndStats.Update(measure05ReaderReadAsyncEndTicks);
                QueryRunStatistics.Measure06ReaderClosedStats.Update(measure06ReaderClosedTicks);
                QueryRunStatistics.Measure07ConnectionClosedStats.Update(measure07ConnectionClosedTicks);
                QueryRunStatistics.TotalSamples++;
            }
        }

        private static void FinishTheWorker(long workStartedByUnit, long workDoneByUnit)
        {
            lock (QueryRunStatistics)
            {
                QueryRunStatistics.ProperlyFinishedWorkers++;
                if (workStartedByUnit > 0)
                    QueryRunStatistics.WorkStartedByUnit.Update(workStartedByUnit);
                else
                    QueryRunStatistics.NotWorkingInstances++;
                QueryRunStatistics.WorkDoneByUnit.Update(workDoneByUnit);
                if (workDoneByUnit != workStartedByUnit)
                    QueryRunStatistics.NotFinishingInstances++;
            }
        }

        private static volatile bool _getSql2ExecuteWritesToConsole;
        private static string GetSql2Execute(BatchType sql2Execute)
        {
            switch (sql2Execute)
            {
                case BatchType.GetNewId:
                    return "SELECT TOP 1 NEWID() AS StressDbToolResult FROM sys.objects";
                case BatchType.LongRun:
                    return string.Format("WAITFOR DELAY '00:00:{0}';", Options.LongRunDelaySeconds);
                default:
                    if (_getSql2ExecuteWritesToConsole)
                        throw new StopExecutionException();
                    _getSql2ExecuteWritesToConsole = true;
                    Logger.WriteInfo(@"ERROR: Unknown Sql2Execute option {0}", sql2Execute);
                    throw new StopExecutionException();
            }
        }
    }
}
