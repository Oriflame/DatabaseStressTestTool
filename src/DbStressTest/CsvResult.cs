// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CsvHelper;

namespace DbStressTest
{
    internal sealed class CsvResult
    {
        public Queue<CsvResultRow> CsvFileWriteQueue = new Queue<CsvResultRow>();
        private bool _initialized;
        private bool _enabled;
        private int _iterationId;

        public bool Enabled
        {
            get { return _enabled; }
        }

        public void Init(Options options)
        {
            if (_initialized)
                throw new InvalidOperationException("Allready initialized");
            _initialized = true;
            if (options.Export2File == null)
                return;
            _enabled = true;
            var dirName = Path.GetDirectoryName(options.Export2File);
            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            if (!File.Exists(options.Export2File))
            {
                using (var writer = File.CreateText(options.Export2File))
                {
                    using (var w = new CsvWriter(writer))
                    {
                        w.WriteHeader(typeof(CsvResultRow));
                    }
                }
            }

            using (var reader = File.OpenText(options.Export2File))
            {
                using (var csvr = new CsvReader(reader))
                {
                    while (csvr.Read() && csvr.Row > 0)
                    {
                        int i;
                        if (csvr.TryGetField(0, out i))
                            _iterationId = i;
                    }
                }
            }
            _iterationId++;
            using (var writer = File.AppendText(options.Export2File))
            {
                using (var w = new CsvWriter(writer))
                {
                    var row = new CsvResultRow(_iterationId, CsvResultRow.ActionType.Init, DateTime.UtcNow, 0, 0, Thread.CurrentThread.ManagedThreadId);
                    w.WriteRecord(row);
                }
            }
        }
    }
}