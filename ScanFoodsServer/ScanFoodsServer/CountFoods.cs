using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CountFoodsThreads
{
    public class CountFoods
    {
        private const string PipeName = "ScanFoods";

        public class Weight
        {
            public double LightWeight { get; set; }
            public double MediumWeight { get; set; }
            public double HeavyWeight { get; set; }

            public Weight(double lightWeight, double mediumWeight, double heavyWeight)
            {
                LightWeight = lightWeight;
                MediumWeight = mediumWeight;
                HeavyWeight = heavyWeight;
            }
        }

        public class CountData
        {
            private readonly object _lock = new object(); //Objeto de bloqueio para sincronização
            private readonly object _pauseLock = new object(); //Objeto de bloqueio para pausar/retomar
            public const int MaxFoodsToScan = 1500;

            public int BeltOne { get; private set; }
            public int BeltTwo { get; private set; }
            public int BeltThree { get; private set; }
            public int TotalScannedFoods { get; private set; }
            public int TotalScannedFoodsToShow { get; private set; }
            public double TotalWeightScannedFoods { get; private set; }
            public double[] FoodWeights { get; private set; }
            public Weight Weight { get; }
            public bool CountFoods { get; private set; }
            public bool RunBeltOne { get; private set; }
            public bool RunBeltTwo { get; private set; }
            public bool RunBeltThree { get; private set; }
            public bool PauseCounting { get; private set; }
            private bool LastRunBeltOne { get; set; }
            private bool LastRunBeltTwo { get; set; }
            private bool LastRunBeltThree { get; set; }

            public CountData()
            {
                BeltOne = 0;
                BeltTwo = 0;
                BeltThree = 0;
                TotalScannedFoods = 0;
                TotalScannedFoodsToShow = 0;
                TotalWeightScannedFoods = 0;
                FoodWeights = new double[MaxFoodsToScan];
                Weight = new Weight(0.5, 2, 5);
                CountFoods = true;
                RunBeltOne = true;
                RunBeltTwo = true;
                RunBeltThree = true;
                PauseCounting = false;
                LastRunBeltOne = true;
                LastRunBeltTwo = true;
                LastRunBeltThree = true;
            }

            public void IncrementBeltOne()
            {
                lock (_lock)
                {
                    BeltOne++;
                }
            }

            public void IncrementBeltTwo()
            {
                lock (_lock)
                {
                    BeltTwo++;
                }
            }

            public void IncrementBeltThree()
            {
                lock (_lock)
                {
                    BeltThree++;
                }
            }

            public void IncrementTotalScannedFoods()
            {
                lock (_lock)
                {
                    TotalScannedFoods++;
                    TotalScannedFoodsToShow++;
                }
            }

            public void UpdateTotalScannedFoods(int value)
            {
                TotalScannedFoods = value;
            }

            public void UpdateTotalWeight(double weight)
            {
                lock (_lock)
                {
                    TotalWeightScannedFoods += weight;
                }
            }

            //Verifica se o número máximo de alimentos foi escaneado (Máximo = 1500)
            public bool IsMaxFoodsScanned()
            {
                lock (_lock)
                {
                    return TotalScannedFoods % MaxFoodsToScan == 0;
                }
            }

            public void StopCounting()
            {
                lock (_lock)
                {
                    CountFoods = false;
                }
            }

            public void PauseCount()
            {
                lock (_pauseLock)
                {
                    PauseCounting = true;
                    LastRunBeltOne = RunBeltOne;
                    LastRunBeltTwo = RunBeltTwo;
                    LastRunBeltThree = RunBeltThree;
                    RunBeltOne = false;
                    RunBeltTwo = false;
                    RunBeltThree = false;
                }
            }

            public void ResumeCount()
            {
                lock (_pauseLock)
                {
                    PauseCounting = false;
                    RunBeltOne = LastRunBeltOne;
                    RunBeltTwo = LastRunBeltTwo;
                    RunBeltThree = LastRunBeltThree;
                    Monitor.PulseAll(_pauseLock);
                }
            }

            //Verifica se a contagem está pausada
            public bool IsPaused()
            {
                lock (_pauseLock)
                {
                    return PauseCounting;
                }
            }

            //Alterna entre pausar/retomar
            public void TogglePause()
            {
                lock (_pauseLock)
                {
                    if (PauseCounting)
                        ResumeCount();
                    else
                        PauseCount();
                }
            }
        }

        private void CountBeltOne(CountData countData)
        {
            var random = new Random();
            while (countData.CountFoods)
            {
                lock (countData)
                {
                    while (countData.IsPaused())
                    {
                        Monitor.Wait(countData);
                    }

                    if (countData.TotalScannedFoods >= CountData.MaxFoodsToScan && countData.IsMaxFoodsScanned())
                    {
                        CalculateTotalWeight(countData);
                    }

                    if (countData.RunBeltOne && random.Next(0, 2) == 1)
                    {
                        countData.IncrementBeltOne();
                        countData.FoodWeights[countData.TotalScannedFoods] = countData.Weight.HeavyWeight;
                        countData.IncrementTotalScannedFoods();
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void CountBeltTwo(CountData countData)
        {
            var random = new Random();
            while (countData.CountFoods)
            {
                lock (countData)
                {
                    while (countData.IsPaused())
                    {
                        Monitor.Wait(countData);
                    }

                    if (countData.TotalScannedFoods >= CountData.MaxFoodsToScan && countData.IsMaxFoodsScanned())
                    {
                        CalculateTotalWeight(countData);
                    }

                    if (countData.RunBeltTwo && random.Next(0, 2) == 1)
                    {
                        countData.IncrementBeltTwo();
                        countData.FoodWeights[countData.TotalScannedFoods] = countData.Weight.MediumWeight;
                        countData.IncrementTotalScannedFoods();
                    }
                }
                Thread.Sleep(500);
            }
        }

        private void CountBeltThree(CountData countData)
        {
            var random = new Random();
            while (countData.CountFoods)
            {
                lock (countData)
                {
                    while (countData.IsPaused())
                    {
                        Monitor.Wait(countData);
                    }

                    if (countData.TotalScannedFoods >= CountData.MaxFoodsToScan && countData.IsMaxFoodsScanned())
                    {
                        CalculateTotalWeight(countData);
                    }

                    if (countData.RunBeltThree && random.Next(0, 2) == 1)
                    {
                        countData.IncrementBeltThree();
                        countData.FoodWeights[countData.TotalScannedFoods] = countData.Weight.LightWeight;
                        countData.IncrementTotalScannedFoods();
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void CalculateTotalWeight(CountData countData)
        {
            lock (countData)
            {
                double totalWeight = countData.FoodWeights.AsParallel().Sum();
                countData.UpdateTotalWeight(totalWeight);

                Array.Clear(countData.FoodWeights, 0, countData.FoodWeights.Length);
                countData.UpdateTotalScannedFoods(0);
            }
        }

        public void ScanElements()
        {
            var countData = new CountData();

            try
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.Out))
                {
                    Console.WriteLine("Aguardando conexão do cliente...");
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Cliente conectado.");

                    using (StreamWriter writer = new StreamWriter(pipeServer))
                    {
                        Thread beltOneThread = new Thread(() => CountBeltOne(countData));
                        Thread beltTwoThread = new Thread(() => CountBeltTwo(countData));
                        Thread beltThreeThread = new Thread(() => CountBeltThree(countData));
                        Thread userInputThread = new Thread(() => UserInputThread(countData));

                        beltOneThread.Start();
                        beltTwoThread.Start();
                        beltThreeThread.Start();
                        userInputThread.Start();

                        while (countData.CountFoods)
                        {
                            //Envia os dados de contagem para o cliente através do pipe
                            string dataToSend = GetCountDataAsString(countData);
                            writer.WriteLine(dataToSend);
                            writer.Flush();

                            Thread.Sleep(2000); //Tempo de atualização dos dados para o cliente
                        }

                        //Aguarda a conclusão das threads
                        beltOneThread.Join();
                        beltTwoThread.Join();
                        beltThreeThread.Join();
                        userInputThread.Join();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no servidor: {ex.Message}");
            }
        }

        private string GetCountDataAsString(CountData countData)
        {
            string data = $"Esteira 1 - Alimentos Escaneados: {countData.BeltOne}\n" +
                          $"Esteira 2 - Alimentos Escaneados: {countData.BeltTwo}\n" +
                          $"Esteira 3 - Alimentos Escaneados: {countData.BeltThree}\n" +
                          $"Total de alimentos escaneados: {countData.TotalScannedFoodsToShow}\n" +
                          $"Peso total escaneado: {countData.TotalWeightScannedFoods}\n\n" +
                          $"Pressione espaço para pausar ou retomar a contagem\n";
            return data;
        }

        public void UserInputThread(CountData countData)
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Spacebar)
                    {
                        countData.TogglePause();
                        Console.WriteLine(countData.IsPaused() ? "Escaneamento Pausado" : "Escaneamento retomado.");
                    }
                }
                Thread.Sleep(10);
            }
        }
    }
}
