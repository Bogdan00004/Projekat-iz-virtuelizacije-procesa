using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using Common;
using Common.Faults;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WeatherService : IWeatherService
    {
        public event EventHandler OnTransferStarted;
        public event EventHandler<WeatherSampleEventArgs> OnSampleReceived;
        public event EventHandler OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        private bool sessionActive = false;
        private FileWriter fileWriter = null;
        private string filesPath = ConfigurationManager.AppSettings["path"] ?? "Files";

        private WeatherEventLogger eventLogger;

        // Podaci za analitiku specificne vlage - tacka 9
        private bool hasPreviousSh = false;
        private double previousSh = 0.0;
        private double shSum = 0.0;
        private int shCount = 0;

        private double shThreshold = 2.0;
        private double shMeanDeviationPercent = 25.0;

        // Podaci za analitiku indeksa toplote - tacka 10
        private bool hasPreviousHi = false;
        private double previousHi = 0.0;
        private double hiMaxThreshold = 40.0;

        public WeatherService()
        {
            eventLogger = new WeatherEventLogger();
            eventLogger.Subscribe(this);
        }

        public WeatherResponse StartSession(SessionMeta meta)
        {
            try
            {
                if (meta == null)
                {
                    throw new FaultException<DataFormatFault>(
                        new DataFormatFault("Meta zaglavlje ne sme biti null."));
                }

                if (string.IsNullOrEmpty(meta.StationName))
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault("Naziv stanice je obavezno polje."));
                }

                shThreshold = ReadDoubleFromConfig("SH_threshold", 2.0);
                shMeanDeviationPercent = ReadDoubleFromConfig("SH_mean_deviation_percent", 25.0);
                hiMaxThreshold = ReadDoubleFromConfig("HI_max_threshold", 40.0);

                ResetSpecificHumidityAnalytics();
                ResetHeatIndexAnalytics();

                fileWriter = new FileWriter(filesPath);

                fileWriter.WriteSession("Date,T,Tpot,Tdew,Rh,Sh");
                fileWriter.WriteReject("Date,T,Tpot,Tdew,Rh,Sh,Razlog");

                sessionActive = true;

                Console.WriteLine($"Sesija pokrenuta za stanicu: {meta.StationName}");
                Console.WriteLine($"Fajlovi kreirani u: {filesPath}");
                Console.WriteLine($"[CONFIG] SH_threshold={shThreshold}, SH_mean_deviation_percent={shMeanDeviationPercent}%");
                Console.WriteLine($"[CONFIG] HI_max_threshold={hiMaxThreshold}");
                Console.WriteLine("Status: prenos u toku...");

                RaiseTransferStarted();

                return new WeatherResponse
                {
                    Status = ResponseStatus.ACK,
                    Message = "Sesija uspesno pokrenuta."
                };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Greska pri pokretanju sesije: {ex.Message}"));
            }
        }

        public WeatherResponse PushSample(WeatherSample sample)
        {
            try
            {
                if (!sessionActive)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault("Sesija nije pokrenuta."));
                }

                if (sample == null)
                {
                    throw new FaultException<DataFormatFault>(
                        new DataFormatFault("Uzorak ne sme biti null."));
                }

                string validationError = ValidateSample(sample);

                if (validationError != null)
                {
                    fileWriter.WriteReject(
                        $"{sample.Date},{sample.T},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{validationError}");

                    Console.WriteLine($"[ODBIJEN UZORAK] {sample.Date} - {validationError}");

                    throw new FaultException<ValidationFault>(
                        new ValidationFault(validationError));
                }

                fileWriter.WriteSession(
                    $"{sample.Date},{sample.T},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh}");

                Console.WriteLine($"[PRIMLJEN UZORAK] Datum: {sample.Date} | T={sample.T} | Sh={sample.Sh} | Rh={sample.Rh}");
                Console.WriteLine("Status: prenos u toku...");

                RaiseSampleReceived(sample);

                // Tacka 9: analiza specificne vlage
                AnalyzeSpecificHumidity(sample);

                // Tacka 10: analiza indeksa toplote
                AnalyzeHeatIndex(sample);

                return new WeatherResponse
                {
                    Status = ResponseStatus.IN_PROGRESS,
                    Message = "Uzorak uspesno primljen."
                };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Greska pri obradi uzorka: {ex.Message}"));
            }
        }

        public WeatherResponse EndSession()
        {
            try
            {
                if (!sessionActive)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault("Nema aktivne sesije."));
                }

                if (fileWriter != null)
                {
                    fileWriter.Dispose();
                    fileWriter = null;
                }

                sessionActive = false;

                Console.WriteLine("Status: zavrsen prenos.");
                Console.WriteLine("Sesija zavrsena. Fajlovi sacuvani.");

                RaiseTransferCompleted();

                return new WeatherResponse
                {
                    Status = ResponseStatus.COMPLETED,
                    Message = "Sesija uspesno zavrsena."
                };
            }
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Greska pri zavrsavanju sesije: {ex.Message}"));
            }
        }

        private string ValidateSample(WeatherSample sample)
        {
            if (string.IsNullOrEmpty(sample.Date))
                return "Polje Date je obavezno.";

            if (sample.Sh <= 0)
                return $"Sh mora biti > 0. Vrednost: {sample.Sh}";

            if (sample.Rh < 0 || sample.Rh > 100)
                return $"Rh mora biti izmedju 0 i 100. Vrednost: {sample.Rh}";

            if (sample.T < -90 || sample.T > 60)
                return $"T mora biti izmedju -90 i 60. Vrednost: {sample.T}";

            if (sample.Tpot < -90 || sample.Tpot > 400)
                return $"Tpot nije u dozvoljenom opsegu. Vrednost: {sample.Tpot}";

            if (sample.Tdew < -90 || sample.Tdew > 60)
                return $"Tdew nije u dozvoljenom opsegu. Vrednost: {sample.Tdew}";

            return null;
        }

        private void AnalyzeSpecificHumidity(WeatherSample sample)
        {
            // 1. DeltaSH = Sh[n] - Sh[n-1]
            if (hasPreviousSh)
            {
                double deltaSh = sample.Sh - previousSh;

                if (Math.Abs(deltaSh) > shThreshold)
                {
                    string direction;

                    if (deltaSh > 0)
                    {
                        direction = "iznad ocekivanog";
                    }
                    else
                    {
                        direction = "ispod ocekivanog";
                    }

                    string message =
                        $"SHSpike: DeltaSH={deltaSh:F2}, prethodni Sh={previousSh:F2}, trenutni Sh={sample.Sh:F2}, prag={shThreshold:F2}";

                    Console.WriteLine($"[ANALITIKA SH] {message}");

                    RaiseWarning(
                        "SHSpike",
                        direction,
                        message,
                        sample,
                        deltaSh,
                        shThreshold);
                }
            }

            // 2. Provera odstupanja od tekuceg proseka SHmean
            if (shCount > 0)
            {
                double shMean = shSum / shCount;

                double lowerLimit = shMean * (1.0 - shMeanDeviationPercent / 100.0);
                double upperLimit = shMean * (1.0 + shMeanDeviationPercent / 100.0);

                if (sample.Sh < lowerLimit || sample.Sh > upperLimit)
                {
                    string direction;

                    if (sample.Sh > upperLimit)
                    {
                        direction = "iznad ocekivane vrednosti";
                    }
                    else
                    {
                        direction = "ispod ocekivane vrednosti";
                    }

                    string message =
                        $"OutOfBandWarning: Sh={sample.Sh:F2}, SHmean={shMean:F2}, dozvoljeno=[{lowerLimit:F2}, {upperLimit:F2}], odstupanje={shMeanDeviationPercent:F2}%";

                    Console.WriteLine($"[ANALITIKA SH] {message}");

                    RaiseWarning(
                        "OutOfBandWarning",
                        direction,
                        message,
                        sample,
                        sample.Sh,
                        shMeanDeviationPercent);
                }
            }

            // 3. Azuriranje running mean vrednosti posle analize trenutnog uzorka
            shSum += sample.Sh;
            shCount++;

            double newMean = shSum / shCount;

            Console.WriteLine($"[ANALITIKA SH] Trenutni Sh={sample.Sh:F2}, SHmean={newMean:F2}, broj uzoraka={shCount}");

            previousSh = sample.Sh;
            hasPreviousSh = true;
        }

        private void AnalyzeHeatIndex(WeatherSample sample)
        {
            double hi = CalculateHeatIndex(sample.T, sample.Rh);

            // DeltaHI = HI[n] - HI[n-1]
            if (hasPreviousHi)
            {
                double deltaHi = hi - previousHi;

                if (Math.Abs(deltaHi) > hiMaxThreshold)
                {
                    string direction;

                    if (deltaHi > 0)
                    {
                        direction = "iznad ocekivanog";
                    }
                    else
                    {
                        direction = "ispod ocekivanog";
                    }

                    string message =
                        $"HISpike: DeltaHI={deltaHi:F2}, prethodni HI={previousHi:F2}, trenutni HI={hi:F2}, prag={hiMaxThreshold:F2}";

                    Console.WriteLine($"[ANALITIKA HI] {message}");

                    RaiseWarning(
                        "HISpike",
                        direction,
                        message,
                        sample,
                        deltaHi,
                        hiMaxThreshold);
                }
            }

            Console.WriteLine($"[ANALITIKA HI] T={sample.T:F2}, Rh={sample.Rh:F2}, HI={hi:F2}");

            previousHi = hi;
            hasPreviousHi = true;
        }

        private double CalculateHeatIndex(double T, double rh)
        {
            // Formula iz specifikacije:
            // HI = -8.78 + 1.61*T + 2.34*rh - 0.15*T*rh - 0.01*T^2 - 0.02*rh^2
            //      + 0.00*T^2*rh + 0.00*T*rh^2 - 0.00*T^2*rh^2

            double hi =
                -8.78
                + 1.61 * T
                + 2.34 * rh
                - 0.15 * T * rh
                - 0.01 * Math.Pow(T, 2)
                - 0.02 * Math.Pow(rh, 2)
                + 0.00 * Math.Pow(T, 2) * rh
                + 0.00 * T * Math.Pow(rh, 2)
                - 0.00 * Math.Pow(T, 2) * Math.Pow(rh, 2);

            return hi;
        }

        private void ResetSpecificHumidityAnalytics()
        {
            hasPreviousSh = false;
            previousSh = 0.0;
            shSum = 0.0;
            shCount = 0;
        }

        private void ResetHeatIndexAnalytics()
        {
            hasPreviousHi = false;
            previousHi = 0.0;
        }

        private double ReadDoubleFromConfig(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            double result;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return defaultValue;
        }

        private void RaiseTransferStarted()
        {
            OnTransferStarted?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseSampleReceived(WeatherSample sample)
        {
            OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));
        }

        private void RaiseTransferCompleted()
        {
            OnTransferCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseWarning(
            string warningType,
            string direction,
            string message,
            WeatherSample sample,
            double value,
            double threshold)
        {
            OnWarningRaised?.Invoke(
                this,
                new WarningEventArgs(
                    warningType,
                    direction,
                    message,
                    sample,
                    value,
                    threshold));
        }
    }
}