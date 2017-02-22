// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using DbStressTest.Properties;

namespace DbStressTest
{
    internal sealed class DbMonitoring
    {
        private Task _currentJob;
        private volatile bool _cancelExecution;
        private readonly Options _options;

        public DbMonitoring(Options options)
        {
            _options = options;
        }

        public void StartAsync()
        {
            if (!_options.MonitorDb)
                return;
            if (_currentJob!=null)
                throw new ApplicationException("Can not start twice!");
            _cancelExecution = false;
            _currentJob = MonitorDbInternal();
            //_currentJob.Start();
        }

        public void End()
        {
            if (!_options.MonitorDb)
                return;
            if (_cancelExecution)
                return;
            _cancelExecution = true;
            Logger.WriteInfo(@"DbMonitoring: stopping...");
            _currentJob.Wait();
            Logger.WriteInfo(@"DbMonitoring: stopped!");
        }

        private async Task MonitorDbInternal()
        {
            await Task.Yield();

            if (_options.ConnectionString.Contains("{"))
            {
                Logger.WriteInfo(@"DbMonitoring: unable to start monitoring on multiple databases at once (there is { in the connection string = we are using multiple DB targets)");
                _cancelExecution = true;
                return;
            }
            while (!_cancelExecution)
            {
                try
                {
                    Logger.WriteInfo(@"DbMonitoring: starting&openning connection...");
                    using (var conn = new SqlConnection(_options.ConnectionString))
                    {
                        await conn.OpenAsync();
                        while (!_cancelExecution)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = Resources.ActiveSessionsCheck;
                                var sb = new StringBuilder();
                                using (var rd = await cmd.ExecuteReaderAsync())
                                {
                                    sb.Append("DbMonitoring: ");
                                    while (rd.Read())
                                    {
                                        var status = rd.GetString(0);
                                        var count = rd.GetInt32(1);
                                        sb.AppendFormat("\"{0}\": {1};", status, count);
                                    }
                                }
                                Logger.WriteInfo(sb.ToString());
                            }
                            await Task.Delay(Settings.Default.DefaultDbMonitoringSampleTimeMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteInfo(@"DbMonitoring: unable to start monitoring because of error (will retry in 20s): {0}.", ex.Message);
                }
                if (!_cancelExecution)
                    await Task.Delay(20 * 1000);
            }
        }
    }
}
