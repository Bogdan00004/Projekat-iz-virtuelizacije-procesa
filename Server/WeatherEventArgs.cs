using System;
using Common;

namespace Server
{
    public class WeatherSampleEventArgs : EventArgs
    {
        public WeatherSample Sample { get; private set; }

        public WeatherSampleEventArgs(WeatherSample sample)
        {
            Sample = sample;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public string WarningType { get; private set; }
        public string Direction { get; private set; }
        public string Message { get; private set; }
        public WeatherSample Sample { get; private set; }
        public double Value { get; private set; }
        public double Threshold { get; private set; }

        public WarningEventArgs(
            string warningType,
            string direction,
            string message,
            WeatherSample sample,
            double value,
            double threshold)
        {
            WarningType = warningType;
            Direction = direction;
            Message = message;
            Sample = sample;
            Value = value;
            Threshold = threshold;
        }
    }
}