// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace DbStressTest
{
    internal sealed class OsStats
    {
        public double TotalTimeMs { get; private set; }
        public double ProcessorTimeSeconds { get; private set; }
        public int ThreadsCount { get; private set; }
        public long WorkingSet64 { get; private set; }
        public long GCTotalMemory { get; private set; }
        public object TotalTimeS {
            get { return TotalTimeMs/1000d; }
        }


        public static OsStats Create(long totalTimeMs, double processorTimeSecondsIgnored = 0)
        {
            using (var process = Process.GetCurrentProcess())
            {
                return new OsStats()
                    {
                        TotalTimeMs = totalTimeMs,
                        ThreadsCount = process.Threads.Count,
                        WorkingSet64 = process.WorkingSet64,
                        ProcessorTimeSeconds = process.TotalProcessorTime.TotalSeconds - processorTimeSecondsIgnored,
                        GCTotalMemory = GC.GetTotalMemory(false)
                    };
            }
        }

        public override string ToString()
        {
            return string.Format(
                    "{0:### ##0.0}s;threads: {1:### ##0};workingSet64: {2:### ##0}MB;GC: {3:### ##0}MB;ProcessorTime = {4:### ##0.#}s",
                    TotalTimeS,
                    ThreadsCount,
                    WorkingSet64 / 1024 / 1024,
                    GCTotalMemory / 1024 / 1024,
                    ProcessorTimeSeconds);
        }
    }
}
