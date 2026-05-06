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
                TestDisposePattern();
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
        static void TestDisposePattern()
        {
            Console.WriteLine("\n=== Test Dispose patterna ===");

            try
            {
                using (FileWriter writer = new FileWriter("TestFiles"))
                {
                    writer.WriteSession("T,Tpot,Tdew,Sh,Rh,Date");
                    writer.WriteSession("25.0,298.0,15.0,10.5,60.0,2024-01-01");
                    Console.WriteLine("[Test] Upisani podaci uspesno.");

                    // Simulacija izuzetka usred prenosa
                    throw new Exception("Simulacija prekida veze!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Test] Izuzetak uhvacen: {ex.Message}");
                Console.WriteLine("[Test] Dispose je automatski pozvan kroz using blok.");
            }
        }
    }
}