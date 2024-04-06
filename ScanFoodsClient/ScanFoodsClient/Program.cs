using System;
using System.IO;
using System.IO.Pipes;

namespace CountFoodsThreadsClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string pipeName = "ScanFoods";

            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.In))
                {
                    Console.WriteLine("Conectando ao servidor...");
                    pipeClient.Connect();

                    Console.WriteLine("Conectado ao servidor.");

                    Console.Clear();

                    using (StreamReader reader = new StreamReader(pipeClient))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Console.WriteLine(line);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao conectar ao servidor: " + ex.Message);
            }
        }
    }
}
