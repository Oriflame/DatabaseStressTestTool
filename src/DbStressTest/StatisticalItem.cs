// Copyright (c) Oriflame Software. All Rights Reserved. Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0. See License.txt in the project root for license information.

using System.Text;

namespace DbStressTest
{
    internal sealed class StatisticalItemLong
    {
        private readonly string _unit;
        private readonly double _unitConversion;
        private long? _max;
        private long? _min;
        private long _total;
        private long _updates;
        public string Name { get; set; }

        public long Total
        {
            get { return _total; }
        }

        public void Update(long value)
        {
            _updates++;
            _total += value;
            if (_min.HasValue)
            {
                if (value < _min.Value)
                    _min = value;
            }
            else
            {
                _min = value;
            }
            if (_max.HasValue)
            {
                if (value > _max.Value)
                    _max = value;
            }
            else
            {
                _max = value;
            }
        }

        public long Min
        {
            get { return _min ?? 0; }
        }

        public long Max
        {
            get { return _max ?? 0; }
        }

        public StatisticalItemLong(string name, string unit, double unitConversion)
        {
            _unit = unit;
            _unitConversion = unitConversion;
            Name = name;
        }

        public void WriteStatistics(StringBuilder sb)
        {
            if (_updates <= 0)
                sb.AppendFormat("{0}: no data", Name).AppendLine();
            else
                sb.AppendFormat("{0}: {2:### ### ### ##0.0} {1} (min: {3:### ### ### ##0.#} {1}, max: {4:### ### ### ##0.#} {1})", 
                    Name, 
                    _unit,
                    _total / (double)_updates * _unitConversion,
                    Min * _unitConversion,
                    Max * _unitConversion).AppendLine();
        }
    }
}
