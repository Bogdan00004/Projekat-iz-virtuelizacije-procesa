using System;
using System.ServiceModel;
using Common;
using Common.Faults;

namespace Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WeatherService : IWeatherService
    {
        private bool sessionActive = false;

        public WeatherResponse StartSession(SessionMeta meta)
        {
            try
            {
                if (meta == null)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Meta zaglavlje ne sme biti null."));
                }

                if (string.IsNullOrEmpty(meta.StationName))
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Naziv stanice je obavezno polje."));
                }

                sessionActive = true;
                Console.WriteLine($"Sesija pokrenuta za stanicu: {meta.StationName}");

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
                throw new FaultException<DataFormatFault>(new DataFormatFault($"Greska pri pokretanju sesije: {ex.Message}"));
            }
        }

        public WeatherResponse PushSample(WeatherSample sample)
        {
            try
            {
                if (!sessionActive)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Sesija nije pokrenuta. Pozovite StartSession prvo."));
                }

                // Provera null
                if (sample == null)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Uzorak ne sme biti null."));
                }

                // Provera obaveznih polja
                if (string.IsNullOrEmpty(sample.Date))
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Polje Date je obavezno."));
                }

                // Validacija opsega
                if (sample.Sh <= 0)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault($"Specificna vlaga (Sh) mora biti veca od 0. Vrednost: {sample.Sh}"));
                }

                if (sample.Rh < 0 || sample.Rh > 100)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault($"Relativna vlaznost (Rh) mora biti izmedju 0 i 100. Vrednost: {sample.Rh}"));
                }

                if (sample.T < -90 || sample.T > 60)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault($"Temperatura (T) mora biti izmedju -90 i 60. Vrednost: {sample.T}"));
                }

                if (sample.Tpot < -90 || sample.Tpot > 400)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault($"Potencijalna temperatura (Tpot) nije u dozvoljenom opsegu. Vrednost: {sample.Tpot}"));
                }

                if (sample.Tdew < -90 || sample.Tdew > 60)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault($"Temperatura tacke rose (Tdew) nije u dozvoljenom opsegu. Vrednost: {sample.Tdew}"));
                }

                Console.WriteLine($"[PRIMLJEN UZORAK] Datum: {sample.Date} | T={sample.T} | Sh={sample.Sh} | Rh={sample.Rh}");

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
                throw new FaultException<DataFormatFault>(new DataFormatFault($"Greska pri obradi uzorka: {ex.Message}"));
            }
        }

        public WeatherResponse EndSession()
        {
            try
            {
                if (!sessionActive)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Nema aktivne sesije za zavrsavanje."));
                }

                sessionActive = false;
                Console.WriteLine("Sesija zavrsena.");

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
                throw new FaultException<DataFormatFault>(new DataFormatFault($"Greska pri zavrsavanju sesije: {ex.Message}"));
            }
        }
    }
}