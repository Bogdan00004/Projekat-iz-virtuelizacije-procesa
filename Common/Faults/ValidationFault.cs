using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        public ValidationFault(string message)
        {
            Message = message;
        }
    }
}