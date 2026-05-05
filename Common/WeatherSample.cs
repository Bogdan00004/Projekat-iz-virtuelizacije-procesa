using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class WeatherSample
    {
        [DataMember]
        public double T { get; set; }

        [DataMember]
        public double Tpot { get; set; }

        [DataMember]
        public double Tdew { get; set; }

        [DataMember]
        public double Sh { get; set; }

        [DataMember]
        public double Rh { get; set; }

        [DataMember]
        public string Date { get; set; }
    }
}