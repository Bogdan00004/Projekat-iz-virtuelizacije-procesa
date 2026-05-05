using System.ServiceModel;
using Common.Faults;
namespace Common
{
    [ServiceContract]
    public interface IWeatherService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        WeatherResponse StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        WeatherResponse PushSample(WeatherSample sample);

        [OperationContract]
        WeatherResponse EndSession();
    }
}