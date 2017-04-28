using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace Forex_Forecasting
{
    public partial class Form1 : Form
    {
        // Input data
        private List<DateTime> Date;
        private List<float> StartPrice;
        private List<float> MaxPrice;
        private List<float> MinPrice;
        private List<float> EndPrice;
        private List<int> Volume;
        private List<float> Difference;

        // For neural networks
        private List<NeuralNetwork> BestCurrentNetwork;
        private List<int> BestCurrentFitness;


        // For threads
        private static object gate;
        private static bool CancelThread;

        // Constants
        private static int NumberOfGeneration;
        private static int NumberOfForcasters;
        private static int BackToRandomIfStuckForGenerations;
        private static int GenerationsBetwenAveragingBest;
        private static int Amplitude;

        public Form1()
        {
            // Windows form creation
            InitializeComponent();

            // Creating object lists
            BestCurrentFitness = new List<int>();
            BestCurrentNetwork = new List<NeuralNetwork>();

            Date = new List<DateTime>();
            StartPrice = new List<float>();
            MaxPrice = new List<float>();
            MinPrice = new List<float>();
            EndPrice = new List<float>();
            Volume = new List<int>();
            Difference = new List<float>();
            
            // Threads comunication tools
            gate = new object(); // we lock that object only to let threads now, who is working with CancelThread flag
            CancelThread = false;

            // Setting constants
            NumberOfForcasters = (512 + 128 + 32 + 8 + 2) * 3;
            BackToRandomIfStuckForGenerations = 5;
            GenerationsBetwenAveragingBest = 200;
            Amplitude = 5;
            NumberOfGeneration = 6000;
        }

        // It is for sending text to a textarea 
        delegate void SetTextCallback(Object ThreadInput);
        private void SetText(Object ThreadInput)
        {
            InputThreadData data = (InputThreadData)ThreadInput;

            if (data.txBox.InvokeRequired)
            {
                try
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke(d, new object[] { ThreadInput });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SetText Exception: {0}", ex.Message);
                }
            }
            else if (!data.txBox.IsDisposed)
            {
                data.txBox.AppendText(data.text + "\r\n");
                data.txBox.Select(data.txBox.Text.Length, data.txBox.Text.Length);
                data.txBox.Focus();

            }
        }

        // Adding one point to the end of specified line in given chart
        delegate void UpdateChartCallback(Object ThreadInput);
        private void UpdateChart(Object ThreadInput)
        {
            InputThreadData data = (InputThreadData)ThreadInput;

            if (data.chart.InvokeRequired)
            {
                try
                {
                    UpdateChartCallback d = new UpdateChartCallback(UpdateChart);
                    this.Invoke(d, new object[] { ThreadInput });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SetText Exception: {0}", ex.Message);
                }
            }
            else if (!data.chart.IsDisposed)
            {
                data.chart.Series[data.line].Points.AddY(data.fitness);
                data.chart.Update();
            }
        }

        // Get data from input file
        private void button1_Click(object sender, EventArgs e)
        {
            StreamReader reader = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = @"C:\Users\Никита\Documents\Visual Studio 2017\Projects\Forex Forecasting\Forex Forecasting\";
            openFileDialog1.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (reader = new StreamReader(openFileDialog1.OpenFile()))
                    {
                        Date.Clear();
                        StartPrice.Clear();
                        MaxPrice.Clear();
                        MinPrice.Clear();
                        EndPrice.Clear();
                        Volume.Clear();

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            var values = line.Split(',');

                            Date.Add(DateTime.Parse(values[0]));
                            StartPrice.Add(Convert.ToSingle(values[1]));
                            MaxPrice.Add(Convert.ToSingle(values[2]));
                            MinPrice.Add(Convert.ToSingle(values[3]));
                            EndPrice.Add(Convert.ToSingle(values[4]));
                            Volume.Add(Convert.ToInt32(values[5]));
                        }

                        reader.Close();

                        for (int i = 0; i < Date.Count; i++)
                        {
                            Difference.Add(EndPrice[i] - StartPrice[i]);
                        }
                    }

                    InputThreadData TextData = new InputThreadData(textBox1, "File loaded \r\n");

                    ThreadPool.QueueUserWorkItem(SetText, TextData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        // Showcase the best result
        private void button2_Click(object sender, EventArgs e)
        {
            lock (gate)
            {
                InputThreadData data = new InputThreadData(BestCurrentNetwork[BestCurrentFitness.IndexOf(BestCurrentFitness.Max())], Date, Difference, 5000);

                MakeTestRun(data);
            }
        }

        // Show a test run in separate thread and in separate window
        private void MakeTestRun (Object ThreadInput)
        {
            InputThreadData data = (InputThreadData)ThreadInput;

            TestRun form = new TestRun();
            form.Show();
            form.TopMost = true;

            float[] input;
            float prediction;
            double account = data.startingCapital;
            bool EUR = true; // Первоначально счет в евро

            // Calculate predictions for each day in dataset
            for (int i = 32; i < Date.Count - 1; i++)
            {
                // Get the last 32 days in the input array
                input = Difference.GetRange(i - 32, 32).ToArray();
                
                prediction = data.network.Calculate(input);

                if (prediction > 0 && !EUR)
                {
                    account += (Difference[i] - 0.0001) * account;
                    EUR = true;
                }
                else if (prediction < 0 && EUR)
                {
                    account -= (Difference[i] + 0.0001) * account;
                    EUR = false;
                }

                form.SetChart(account);
            }
        }

        // Neural Network training
        private void button3_Click(object sender, EventArgs e)
        {
            int AvailableThreads = 1;
            int IO = 0;

            ThreadPool.GetAvailableThreads(out AvailableThreads, out IO);

            if (AvailableThreads > 3)
                AvailableThreads = 3;

            for (int i = 0; i < AvailableThreads; i++)
            {
                if(i > 0)
                    chart1.Series.Add(new System.Windows.Forms.DataVisualization.Charting.Series());

                chart1.Series[i].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                chart1.Series[i].Name = "Thread " + Convert.ToString(i);

                InputThreadData input = new InputThreadData(NumberOfGeneration / AvailableThreads, i);

                ThreadPool.QueueUserWorkItem(EvolutionThread, input);
            }
        }

        private void SysData_TextChanged(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        // main method for neural network calculations and evolution
        private void EvolutionThread (Object ThreadInput)
        {
            NeuralNetwork[] forecasters = new NeuralNetwork[NumberOfForcasters];
            int[] fitness = new int[NumberOfForcasters];
            float prediction; 
            float[] input;  // array of price differences (start day price - end day price)
            NeuralNetwork best = new NeuralNetwork();
            bool RandomFlag = false;
            int[] LatestFitness = new int[BackToRandomIfStuckForGenerations];
            int NumberOfSteps, ThreadNumber;

            // Random number generator
            Random connection;

            // Checking if input object can be used as number of steps
            try
            {
                InputThreadData data = (InputThreadData)ThreadInput;
                NumberOfSteps = data.NumberOfSteps;
                ThreadNumber = data.ThreadNumber;
            }
            catch (InvalidCastException)
            {
                NumberOfSteps = 2000;
                ThreadNumber = 0;
            }

            lock (gate)
            {
                connection = new Random();  // Random generator, used in creating random connections

                for (int i = 0; i < NumberOfForcasters; i++)
                {
                    forecasters[i] = new NeuralNetwork();
                    forecasters[i].SetConnectionsRandom(connection, Amplitude);
                }
            }

            // Start "evolution cycle"
            for (int k = 0; k < NumberOfSteps; k++)
            {
                // Check if form is closed and thread needs to be terminated
                lock (gate)
                {
                    if(CancelThread)
                    {
                        Thread.CurrentThread.Abort();
                    }
                }

                // run simulation for each neural network in set
                for (int j = 0; j < NumberOfForcasters; j++)
                {
                    // Dont forget to set curent fitness back to zero
                    fitness[j] = 0;

                    // Calculate predictions for each day in dataset
                    for (int i = 32; i < Date.Count - 1; i++)
                    {
                        lock (gate)
                        {
                            // Get the last 32 days in the input array
                            input = Difference.GetRange(i - 32, 32).ToArray();
                        }
                        prediction = forecasters[j].Calculate(input);

                        lock (gate)
                        {
                            // If the "direction" of prediction is correct, than increase fitness
                            if ((Difference[i] > 0 && prediction > 0) || (Difference[i] < 0 && prediction < 0))
                                fitness[j]++;
                            else
                                fitness[j]--;
                        }
                    }
                }

                // Update charts and textbox
                InputThreadData TextData = new InputThreadData(textBox1, "[" + Convert.ToString(ThreadNumber) + "] Max fitness: " + Convert.ToString(fitness.Max()) + "\n");
                InputThreadData ChartData = new InputThreadData(chart1, ThreadNumber, fitness.Max());

                ThreadPool.QueueUserWorkItem(SetText, TextData);
                ThreadPool.QueueUserWorkItem(UpdateChart, ChartData);

                // Update running average fitness
                LatestFitness[k % BackToRandomIfStuckForGenerations] = fitness.Max();

                // If there is no improvements in [BackToRandomIfStuckForGenerations] generations, get back to generating networks randomly 
                if (fitness.Max() <= LatestFitness.Average() && !RandomFlag)
                {
                    RandomFlag = true;

                    InputThreadData TextData1 = new InputThreadData(textBox1, "\r\n [" + Convert.ToString(ThreadNumber) + "] We got stuck. New random set generated \r\n");

                    ThreadPool.QueueUserWorkItem(SetText, TextData1);
                }

                // Find and save the best result
                best.SetNeuralNetwork(forecasters[fitness.ToList().IndexOf(fitness.Max())]);

                // If we got stuck, save current network 
                if (RandomFlag)
                {
                    lock (gate)
                    {
                        // Save the best network ...
                        BestCurrentNetwork.Add(new NeuralNetwork());
                        BestCurrentNetwork.Last().SetNeuralNetwork(best);

                        // ... and its fitness
                        BestCurrentFitness.Add(fitness.Max());

                        // Save in file
                        BestCurrentNetwork.Last().OutputNetwork(BestCurrentFitness.Last());
                    }                    
                }
                
                if (k % GenerationsBetwenAveragingBest == 0)
                {
                    lock (gate)
                    {
                        // Every [GenerationsBetwenAveragingBest] generations we asume best network is the sum of all previos best
                        best.SetNetworkFromList(BestCurrentNetwork);
                        RandomFlag = false;
                    }         
                }

                if (RandomFlag)
                {
                    // generate new random set
                    lock (gate)
                    {
                        for (int i = 0; i < NumberOfForcasters; i++)
                        {
                            forecasters[i].SetConnectionsRandom(connection, Amplitude);
                        }
                    }
                    RandomFlag = false;

                    for (int i = 0; i < BackToRandomIfStuckForGenerations; i++)
                    {
                        LatestFitness[i] = 0;
                    }
                }
                else
                {
                    // Create a new set by tweaking the best one
                    for (int i = 0; i < NumberOfForcasters; i += 3)
                    {
                        forecasters[i].SetNeuralNetwork(best);
                        forecasters[i].ResetNodes();

                        forecasters[i + 1].SetNeuralNetwork(best);
                        forecasters[i + 1].ResetNodes();

                        forecasters[i + 2].SetNeuralNetwork(best);
                        forecasters[i + 2].ResetNodes();

                        if (i < 512 * 3)
                        {
                            forecasters[i].ChangeConnection(1, i / 3, 1);
                            forecasters[i + 1].ChangeConnection(1, i / 3, -1);
                            forecasters[i + 2].ChangeConnection(1, i / 3);
                        }
                        else if (i < (512 + 128) * 3)
                        {
                            forecasters[i].ChangeConnection(2, i / 3 - 512, 1);
                            forecasters[i + 1].ChangeConnection(2, i / 3 - 512, -1);
                            forecasters[i + 2].ChangeConnection(2, i / 3 - 512);
                        }
                        else if (i < (512 + 128 + 32) * 3)
                        {
                            forecasters[i].ChangeConnection(3, i / 3 - 640, 1);
                            forecasters[i + 1].ChangeConnection(3, i / 3 - 640, -1);
                            forecasters[i + 2].ChangeConnection(3, i / 3 - 640);
                        }
                        else if (i < (512 + 128 + 32 + 8) * 3)
                        {
                            forecasters[i].ChangeConnection(4, i / 3 - 672, 1);
                            forecasters[i + 1].ChangeConnection(4, i / 3 - 672, -1);
                            forecasters[i + 2].ChangeConnection(4, i / 3 - 672);
                        }
                        else
                        {
                            forecasters[i].ChangeConnection(5, i / 3 - 680, 1);
                            forecasters[i + 1].ChangeConnection(5, i / 3 - 680, -1);
                            forecasters[i + 2].ChangeConnection(5, i / 3 - 680);
                        }
                    }
                }

            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
