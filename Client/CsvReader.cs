using System;
using System.IO;

namespace Client
{
    public class CsvReader : IDisposable
    {
        private StreamReader reader;
        private string path;
        private bool disposed = false;

        public string Path { get { return path; } }

        public CsvReader(string path)
        {
            this.path = path;
            reader = new StreamReader(path);
            Console.WriteLine($"[CsvReader] Otvoren fajl: {path}");
        }

        public string ReadLine()
        {
            if (disposed)
                throw new ObjectDisposedException("CsvReader");

            return reader.ReadLine();
        }

        public bool EndOfFile()
        {
            return reader.EndOfStream;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                        Console.WriteLine("[CsvReader] StreamReader zatvoren i oslobodjen.");
                    }
                }
                disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}