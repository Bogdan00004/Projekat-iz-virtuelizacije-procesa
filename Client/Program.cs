using Common;
using Common.Faults;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string csvPath = ConfigurationManager.AppSettings["csvPath"];

            List<WeatherSample> samples = new List<WeatherSample>();
            List<string> invalidRows = new List<string>();

            // Citanje CSV-a
            using (CsvReader csvReader = new CsvReader(csvPath))
            {
                // Preskoci header
                string header = csvReader.ReadLine();
                int rowIndex = 0;
                int validCount = 0;

                while (!csvReader.EndOfFile() && validCount < 113)
                {
                    rowIndex++;
                    string line = csvReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    WeatherSample sample = ParseLine(line, rowIndex, invalidRows);

                    if (sample != null)
                    {
                        samples.Add(sample);
                        validCount++;
                    }
                }

                Console.WriteLine($"Ucitano validnih redova: {samples.Count}");
                Console.WriteLine($"Nevalidnih redova: {invalidRows.Count}");
            }

            // Upisivanje nevalidnih redova u log
            WriteInvalidLog(invalidRows);

            // Slanje podataka ka servisu
            SendToServer(samples);

            Console.ReadLine();
        }

        static WeatherSample ParseLine(string line, int rowIndex, List<string> invalidRows)
        {
            try
            {
                string[] parts = line.Split(',');

                if (parts.Length < 7)
                {
                    invalidRows.Add($"Red {rowIndex}: nedovoljan broj kolona -> {line}");
                    return null;
                }

                string date = parts[0].Trim();

                // Preskoci redove sa nan vrednostima
                for (int i = 1; i < 7; i++)
                {
                    if (parts[i].Trim().ToLower() == "nan")
                    {
                        invalidRows.Add($"Red {rowIndex}: sadrzi nan vrednost -> {line}");
                        return null;
                    }
                }

                // indeksi kolona
                double T = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                double Tpot = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                double Tdew = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
                double Rh = double.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
                double Sh = double.Parse(parts[6].Trim(), CultureInfo.InvariantCulture);

                // Validacija opsega
                if (Sh <= 0)
                {
                    invalidRows.Add($"Red {rowIndex}: Sh mora biti > 0 -> {line}");
                    return null;
                }

                if (Rh < 0 || Rh > 100)
                {
                    invalidRows.Add($"Red {rowIndex}: Rh mora biti izmedju 0 i 100 -> {line}");
                    return null;
                }

                return new WeatherSample
                {
                    Date = date,
                    T = T,
                    Tpot = Tpot,
                    Tdew = Tdew,
                    Rh = Rh,
                    Sh = Sh
                };
            }
            catch (Exception ex)
            {
                invalidRows.Add($"Red {rowIndex}: greska parsiranja ({ex.Message}) -> {line}");
                return null;
            }
        }

        static void WriteInvalidLog(List<string> invalidRows)
        {
            string logPath = "Data\\invalid_rows.log";

            using (StreamWriter writer = new StreamWriter(logPath, append: false))
            {
                writer.WriteLine($"Log nevalidnih redova - {DateTime.Now}");
                writer.WriteLine("==========================================");

                if (invalidRows.Count == 0)
                {
                    writer.WriteLine("Nema nevalidnih redova.");
                }
                else
                {
                    foreach (string row in invalidRows)
                    {
                        writer.WriteLine(row);
                    }
                }
            }

            Console.WriteLine($"Log nevalidnih redova upisan: {logPath}");
        }

        static void SendToServer(List<WeatherSample> samples)
        {
            ChannelFactory<IWeatherService> factory =
                new ChannelFactory<IWeatherService>("WeatherService");
            IWeatherService proxy = factory.CreateChannel();

            try
            {
                // StartSession
                WeatherResponse r1 = proxy.StartSession(new SessionMeta
                {
                    StationName = "MeteoStanica1",
                    Description = "Simulacija merenja iz CSV fajla"
                });
                Console.WriteLine($"StartSession: {r1.Status} - {r1.Message}");

                // PushSample za svaki red
                for (int i = 0; i < samples.Count; i++)
                {
                    WeatherResponse r2 = proxy.PushSample(samples[i]);
                    Console.WriteLine($"[{i + 1}/{samples.Count}] PushSample: {r2.Status}");
                }

                // EndSession
                WeatherResponse r3 = proxy.EndSession();
                Console.WriteLine($"EndSession: {r3.Status} - {r3.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message}");
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }
            finally
            {
                factory.Close();
            }
        }
    }
}