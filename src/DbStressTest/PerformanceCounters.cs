// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DbStressTest
{
    internal sealed class PerformanceCounters
    {
        static PerformanceCounter[] _perfCounters = new PerformanceCounter[10];
        public static void SetUpPerformanceCounters()
        {
            _perfCounters = new PerformanceCounter[10];
            string instanceName = GetInstanceName();
            Type apc = typeof(AdoNetPerformanceCounters);
            int i = 0;
            foreach (string s in Enum.GetNames(apc))
            {
                _perfCounters[i] = new PerformanceCounter
                {
                    CategoryName = ".NET Data Provider for SqlServer",
                    CounterName = s,
                    InstanceName = instanceName
                };
                i++;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetCurrentProcessId();

        private static string GetInstanceName()
        {
            //This works for Winforms apps.
            string instanceName =
                Assembly.GetEntryAssembly().GetName().Name;

            // Must replace special characters like (, ), #, /, \\
            //string instanceName2 =
            //    AppDomain.CurrentDomain.FriendlyName.ToString().Replace('(', '[')
            //    .Replace(')', ']').Replace('#', '_').Replace('/', '_').Replace('\\', '_');

            // For ASP.NET applications your instanceName will be your CurrentDomain's 
            // FriendlyName. Replace the line above that sets the instanceName with this:
            // instanceName = AppDomain.CurrentDomain.FriendlyName.ToString().Replace('(','[')
            // .Replace(')',']').Replace('#','_').Replace('/','_').Replace('\\','_');

            string pid = GetCurrentProcessId().ToString();
            instanceName = instanceName + "[" + pid + "]";
            Logger.WriteInfo(@"Instance Name: {0}", instanceName);
            Logger.WriteInfo(@"---------------------------");
            return instanceName;
        }

        public static void WritePerformanceCounters()
        {
            Logger.WriteInfo(@"---------------------------");
            foreach (var p in _perfCounters)
            {
                Logger.WriteInfo(@"{0} = {1}", p.CounterName, p.NextValue());
            }
            Logger.WriteInfo(@"---------------------------");
        }


        public enum AdoNetPerformanceCounters
        {
            NumberOfActiveConnectionPools,
            NumberOfReclaimedConnections,
            HardConnectsPerSecond,
            HardDisconnectsPerSecond,
            NumberOfActiveConnectionPoolGroups,
            NumberOfInactiveConnectionPoolGroups,
            NumberOfInactiveConnectionPools,
            NumberOfNonPooledConnections,
            NumberOfPooledConnections,
            NumberOfStasisConnections
            // The following performance counters are more expensive to track.
            // Enable ConnectionPoolPerformanceCounterDetail in your config file.
            //     SoftConnectsPerSecond
            //     SoftDisconnectsPerSecond
            //     NumberOfActiveConnections
            //     NumberOfFreeConnections
        }
    }
}
