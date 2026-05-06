using Common;
using System;
using System.ServiceModel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(WeatherService));

            try
            {
                host.Open();
                Console.WriteLine("Servis je pokrenut...");
                Console.WriteLine("Pritisnite Enter za zaustavljanje.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
            finally
            {
                host.Close();
                Console.WriteLine("Servis je zaustavljen.");
            }
        }
    }
}