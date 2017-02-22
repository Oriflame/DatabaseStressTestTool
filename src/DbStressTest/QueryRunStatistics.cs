// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace DbStressTest
{
    internal sealed class QueryRunStatistics
    {
        public readonly StatisticalItemLong Measure01NewConnStats = new StatisticalItemLong("01NewConn", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure02ConnOpenStats = new StatisticalItemLong("02ConnOpen", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure03CreateCommandStats = new StatisticalItemLong("03CreateCommand", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure04ExecuteReaderStats = new StatisticalItemLong("04ExecuteReader", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure05ReaderReadAsyncEndStats = new StatisticalItemLong("05ReaderReadAsyncEnd", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure06ReaderClosedStats = new StatisticalItemLong("06ReaderClosed", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong Measure07ConnectionClosedStats = new StatisticalItemLong("07ConnectionClosed", "μs", 1000d * 1000 / Stopwatch.Frequency);
        public readonly StatisticalItemLong WorkStartedByUnit = new StatisticalItemLong("WorkStartedByUnit", "connects", 1);
        public readonly StatisticalItemLong WorkDoneByUnit = new StatisticalItemLong("WorkDoneByUnit", "connects", 1);
        /// <summary>
        /// # of instances, that in fact did not start any work
        /// </summary>
        public long NotWorkingInstances { get; set; }
        /// <summary>
        /// # of instances, that did not finish work (probably error occured)
        /// </summary>
        public long NotFinishingInstances { get; set; }
        /// <summary>
        /// If a worker properly finishes, it should increment this...
        /// </summary>
        public long ProperlyFinishedWorkers { get; set; }

        public OsStats StatsAfterInit { get; set; }
        public OsStats StatsBeforeStop { get; set; }

        /// <summary>
        /// options to output in ToString
        /// </summary>
        public Options Options { get; set; }

        /// <summary>
        /// total samples
        /// </summary>
        public long TotalSamples;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        public long TotalErrors { get; private set; }
        public readonly Dictionary<string, int> Errors = new Dictionary<string, int>();

        public double CalculateSamplesPerSecond()
        {
            if (_stopwatch.ElapsedMilliseconds > 0)
                return TotalSamples * 1000d / _stopwatch.ElapsedMilliseconds;
            return 0;
        }

        public double TotalSeconds
        {
            get { return _stopwatch.ElapsedMilliseconds / 1000d; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("##########################################");
            if (StatsBeforeStop != null)
            {
                sb.AppendFormat("Total {0} samples in {1:### ###.#} s, {2:### ###.#}/s", TotalSamples,
                        StatsBeforeStop.TotalTimeS, CalculateSamplesPerSecond()).AppendLine();
            }
            if (StatsAfterInit != null)
                sb.AppendFormat("INIT:{0}", StatsAfterInit).AppendLine();
            if (StatsBeforeStop != null)
                sb.AppendFormat("WORK:{0}", StatsBeforeStop).AppendLine();
            if (Options != null)
                sb.AppendFormat("OPTIONS:{0}", Options).AppendLine();
            sb.AppendLine();
            WorkStartedByUnit.WriteStatistics(sb);
            WorkDoneByUnit.WriteStatistics(sb);
            sb.AppendFormat("Properly finished instances: {0}", ProperlyFinishedWorkers).AppendLine();
            sb.AppendFormat("Instances, that in fact have not done any work: {0}", NotWorkingInstances).AppendLine();
            sb.AppendFormat("Instances, that did not finish work (error occured?): {0}", NotFinishingInstances).AppendLine();
            sb.AppendLine();
            Measure01NewConnStats.WriteStatistics(sb);
            Measure02ConnOpenStats.WriteStatistics(sb);
            Measure03CreateCommandStats.WriteStatistics(sb);
            Measure04ExecuteReaderStats.WriteStatistics(sb);
            Measure05ReaderReadAsyncEndStats.WriteStatistics(sb);
            Measure06ReaderClosedStats.WriteStatistics(sb);
            Measure07ConnectionClosedStats.WriteStatistics(sb);
            sb.AppendLine("##########################################");
            if (TotalErrors > 0)
            {
                sb.AppendLine();
                sb.AppendLine("------ ERRORS ------");
                foreach (var error in Errors)
                {
                    sb.AppendFormat("{0}: {1}", error.Key, error.Value).AppendLine();
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void WasErrors(Exception exception)
        {
            TotalErrors++;
            var sEx = exception as SqlException;
            var errorType = sEx == null ? exception.Message : String.Format("[{0}] {1}", sEx.Number, sEx.Message);
            if (Errors.ContainsKey(errorType))
                Errors[errorType]++;
            else
                Errors.Add(errorType, 1);
        }

        public void InitStarted()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }
        public void InitFinished(bool continueInTimeMeasurement)
        {
            StatsAfterInit = OsStats.Create(_stopwatch.ElapsedMilliseconds);
            if (!continueInTimeMeasurement)
                _stopwatch.Stop();
        }

        public void Start()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void GetStatsBeforeStop(bool ignoreInitPart)
        {
            _stopwatch.Stop();
            StatsBeforeStop = OsStats.Create(_stopwatch.ElapsedMilliseconds, ignoreInitPart ? StatsAfterInit.ProcessorTimeSeconds : 0);
        }
    }
}
