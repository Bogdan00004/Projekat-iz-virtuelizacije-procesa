using System;
using System.Configuration;
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

                fileWriter = new FileWriter(filesPath);

                fileWriter.WriteSession("Date,T,Tpot,Tdew,Rh,Sh");
                fileWriter.WriteReject("Date,T,Tpot,Tdew,Rh,Sh,Razlog");

                sessionActive = true;

                Console.WriteLine($"Sesija pokrenuta za stanicu: {meta.StationName}");
                Console.WriteLine($"Fajlovi kreirani u: {filesPath}");
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