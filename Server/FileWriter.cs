using System;
using System.IO;

namespace Server
{
    public class FileWriter : IDisposable
    {
        private StreamWriter sessionWriter;
        private StreamWriter rejectsWriter;
        private bool disposed = false;
        private string sessionPath;
        private string rejectsPath;

        public FileWriter(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            sessionPath = System.IO.Path.Combine(directory, "measurements_session.csv");
            rejectsPath = System.IO.Path.Combine(directory, "rejects.csv");

            sessionWriter = new StreamWriter(sessionPath, append: true);
            rejectsWriter = new StreamWriter(rejectsPath, append: true);

            Console.WriteLine("[FileWriter] Fajlovi otvoreni.");
        }

        public void WriteSession(string line)
        {
            if (disposed)
                throw new ObjectDisposedException("FileWriter");

            sessionWriter.WriteLine(line);
            sessionWriter.Flush();
        }

        public void WriteReject(string line)
        {
            if (disposed)
                throw new ObjectDisposedException("FileWriter");

            rejectsWriter.WriteLine(line);
            rejectsWriter.Flush();
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
                    if (sessionWriter != null)
                    {
                        sessionWriter.Close();
                        sessionWriter.Dispose();
                        Console.WriteLine("[FileWriter] SessionWriter zatvoren.");
                    }

                    if (rejectsWriter != null)
                    {
                        rejectsWriter.Close();
                        rejectsWriter.Dispose();
                        Console.WriteLine("[FileWriter] RejectsWriter zatvoren.");
                    }
                }
                disposed = true;
            }
        }

        ~FileWriter()
        {
            Dispose(false);
        }
    }
}