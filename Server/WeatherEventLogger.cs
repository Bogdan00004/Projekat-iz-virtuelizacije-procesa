using System;

namespace Server
{
    public class WeatherEventLogger
    {
        public void Subscribe(WeatherService service)
        {
            service.OnTransferStarted += Service_OnTransferStarted;
            service.OnSampleReceived += Service_OnSampleReceived;
            service.OnTransferCompleted += Service_OnTransferCompleted;
            service.OnWarningRaised += Service_OnWarningRaised;
        }

        private void Service_OnTransferStarted(object sender, EventArgs e)
        {
            Console.WriteLine("[EVENT] OnTransferStarted -> Prenos je pokrenut.");
        }

        private void Service_OnSampleReceived(object sender, WeatherSampleEventArgs e)
        {
            Console.WriteLine($"[EVENT] OnSampleReceived -> Primljen uzorak: {e.Sample.Date}");
        }

        private void Service_OnTransferCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("[EVENT] OnTransferCompleted -> Prenos je zavrsen.");
        }

        private void Service_OnWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"[EVENT] OnWarningRaised -> {e.WarningType} | {e.Direction} | {e.Message}");
        }
    }
}