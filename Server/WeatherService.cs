using System;
using System.ServiceModel;
using Common;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WeatherService : IWeatherService
    {
        public WeatherResponse StartSession(SessionMeta meta)
        {
            Console.WriteLine("Sesija pokrenuta.");
            return new WeatherResponse
            {
                Status = ResponseStatus.ACK,
                Message = "Sesija uspešno pokrenuta."
            };
        }

        public WeatherResponse PushSample(WeatherSample sample)
        {
            Console.WriteLine($"Primljen uzorak: T={sample.T}, Sh={sample.Sh}");
            return new WeatherResponse
            {
                Status = ResponseStatus.IN_PROGRESS,
                Message = "Uzorak primljen."
            };
        }

        public WeatherResponse EndSession()
        {
            Console.WriteLine("Sesija završena.");
            return new WeatherResponse
            {
                Status = ResponseStatus.COMPLETED,
                Message = "Sesija uspešno završena."
            };
        }
    }
}