using System.Runtime.Serialization;

namespace Common
{
    public enum ResponseStatus
    {
        ACK,
        NACK,
        IN_PROGRESS,
        COMPLETED
    }

    [DataContract]
    public class WeatherResponse
    {
        [DataMember]
        public ResponseStatus Status { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}