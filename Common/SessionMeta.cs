using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string StationName { get; set; }
    }
}