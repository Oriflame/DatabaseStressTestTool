// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System;

namespace DbStressTest
{
    internal sealed class CsvResultRow
    {
        public enum ActionType
        {
            Init,
            DbAction
        }

        private readonly ActionType _action;
        private readonly long _durationMs;
        private readonly long _durationTicks;
        private readonly int _runId;
        private readonly DateTime _startedDate;
        private readonly int _threadId;

        public CsvResultRow(int runId, ActionType action, DateTime startedDate, long durationMs, long durationTicks,
            int threadId)
        {
            _runId = runId;
            _action = action;
            _startedDate = startedDate.Date;
            _durationMs = durationMs;
            _durationTicks = durationTicks;
            _threadId = threadId;
        }

        public ActionType Action
        {
            get { return _action; }
        }

        public int RunId
        {
            get { return _runId; }
        }

        public DateTime StartedDate
        {
            get { return _startedDate; }
        }

        public long DurationMs
        {
            get { return _durationMs; }
        }

        public int ThreadId
        {
            get { return _threadId; }
        }

        public long DurationTicks
        {
            get { return _durationTicks; }
        }
    }
}