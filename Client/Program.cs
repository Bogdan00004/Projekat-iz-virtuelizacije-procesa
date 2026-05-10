using Common;
using Common.Faults;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    class Program
    {
        private const int MAX_VALID_ROWS = 113;

        static void Main(string[] args)
        {
            string csvPath = ConfigurationManager.AppSettings["csvPath"];
            List<string> invalidRows = new List<string>();

            SendCsvSequentially(csvPath, invalidRows);

            WriteInvalidLog(invalidRows);

            Console.WriteLine("Pritisni ENTER za izlaz...");
            Console.ReadLine();
        }

        static void SendCsvSequentially(string csvPath, List<string> invalidRows)
        {
            ChannelFactory<IWeatherService> factory = new ChannelFactory<IWeatherService>("WeatherService");

            IWeatherService proxy = null;
            IClientChannel channel = null;
            bool sessionStarted = false;

            try
            {
                proxy = factory.CreateChannel();
                channel = (IClientChannel)proxy;

                WeatherResponse startResponse = proxy.StartSession(new SessionMeta
                {
                    StationName = "MeteoStanica1",
                    Description = "Simulacija merenja iz CSV fajla - sekvencijalni prenos"
                });

                sessionStarted = true;

                Console.WriteLine($"StartSession: {startResponse.Status} - {startResponse.Message}");
                Console.WriteLine("Pocinje sekvencijalno slanje uzoraka...");

                using (CsvReader csvReader = new CsvReader(csvPath))
                {
                    string header = csvReader.ReadLine();
                    Console.WriteLine("Procitan header:");
                    Console.WriteLine(header);

                    int rowIndex = 0;
                    int validCount = 0;

                    while (!csvReader.EndOfFile() && validCount < MAX_VALID_ROWS)
                    {
                        rowIndex++;

                        string line = csvReader.ReadLine();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        WeatherSample sample = ParseLine(line, rowIndex, invalidRows);

                        if (sample == null)
                        {
                            continue;
                        }

                        validCount++;

                        try
                        {
                            WeatherResponse pushResponse = proxy.PushSample(sample);

                            Console.WriteLine($"[{validCount}/{MAX_VALID_ROWS}] PushSample: {pushResponse.Status} - {pushResponse.Message}");

                            // Mala pauza samo da se vidi da se salje red po red.
                            Thread.Sleep(50);
                        }
                        catch (FaultException<ValidationFault> ex)
                        {
                            string message = $"Red {rowIndex}: server odbio uzorak - {ex.Detail.Message}";
                            invalidRows.Add(message);
                            Console.WriteLine(message);
                        }
                        catch (FaultException<DataFormatFault> ex)
                        {
                            string message = $"Red {rowIndex}: format greska na serveru - {ex.Detail.Message}";
                            invalidRows.Add(message);
                            Console.WriteLine(message);
                        }
                    }

                    Console.WriteLine($"Ukupno poslato validnih redova: {validCount}");
                }
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
                if (sessionStarted && proxy != null)
                {
                    try
                    {
                        WeatherResponse endResponse = proxy.EndSession();
                        Console.WriteLine($"EndSession: {endResponse.Status} - {endResponse.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greska pri EndSession: {ex.Message}");
                    }
                }

                try
                {
                    if (channel != null)
                    {
                        if (channel.State == CommunicationState.Faulted)
                        {
                            channel.Abort();
                        }
                        else
                        {
                            channel.Close();
                        }
                    }

                    if (factory.State == CommunicationState.Faulted)
                    {
                        factory.Abort();
                    }
                    else
                    {
                        factory.Close();
                    }
                }
                catch
                {
                    factory.Abort();
                }
            }
        }

        static WeatherSample ParseLine(string line, int rowIndex, List<string> invalidRows)
        {
            try
            {
                string[] parts = line.Split(',');

                if (parts.Length < 10)
                {
                    invalidRows.Add($"Red {rowIndex}: nedovoljan broj kolona -> {line}");
                    return null;
                }

                string date = parts[0].Trim();

                int[] requiredIndexes = { 2, 3, 4, 5, 9 };

                foreach (int index in requiredIndexes)
                {
                    string value = parts[index].Trim();

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        invalidRows.Add($"Red {rowIndex}: prazna vrednost u koloni {index} -> {line}");
                        return null;
                    }

                    if (value.ToLower() == "nan")
                    {
                        invalidRows.Add($"Red {rowIndex}: sadrzi nan vrednost u koloni {index} -> {line}");
                        return null;
                    }
                }

                double T = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                double Tpot = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                double Tdew = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture);
                double Rh = double.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
                double Sh = double.Parse(parts[9].Trim(), CultureInfo.InvariantCulture);

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
            Directory.CreateDirectory("Data");

            string logPath = Path.Combine("Data", "invalid_rows.log");

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
    }
}