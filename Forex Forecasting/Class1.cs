using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Forex_Forecasting
{
    public class InputThreadData
    {
        public int NumberOfSteps;
        public int ThreadNumber;

        public System.Windows.Forms.DataVisualization.Charting.Chart chart;
        public int line;
        public int fitness;
        public double account;

        public TextBox txBox;
        public string text;

        public NeuralNetwork network;
        public List<DateTime> dates;
        public List<float> differences;
        public float startingCapital;


        // Input for calculation cycle
        public InputThreadData(int Steps, int thread)
        {
            NumberOfSteps = Steps;
            ThreadNumber = thread;
        }

        // input for chart data
        public InputThreadData(System.Windows.Forms.DataVisualization.Charting.Chart Chart, int Line, int Fitness)
        {
            chart = Chart;
            line = Line;
            fitness = Fitness;
        }

        // input for chart data in test run
        public InputThreadData(System.Windows.Forms.DataVisualization.Charting.Chart Chart, double Account)
        {
            chart = Chart;
            account = Account;
        }

        // input for text data
        public InputThreadData(TextBox TxBox, String Text)
        {
            txBox = TxBox;
            text = Text;
        }

        // input for test run in new window data
        public InputThreadData(NeuralNetwork Nerwork, List<DateTime> Dates, List<float> Differences, float StartingCapital)
        {
            network = Nerwork;
            dates = Dates;
            differences = Differences;
            startingCapital = StartingCapital;
        }

    }

    public class NeuralNetwork
    {
        private float[] NodLine1 = new float[32]; // Not really necessary
        private float[] NodLine2 = new float[16];
        private float[] NodLine3 = new float[8];
        private float[] NodLine4 = new float[4];
        private float[] NodLine5 = new float[2];

        private int[] connections12 = new int[512];
        private int[] connections23 = new int[128];
        private int[] connections34 = new int[32];
        private int[] connections45 = new int[8];
        private int[] connections56 = new int[2];

        public NeuralNetwork()
        {
        }

        public void SetConnectionsRandom(Random connection, int Amplitude)
        {
            for (int i = 0; i < 512; i++)
                this.connections12[i] = (int)(connection.Next(Amplitude) - Amplitude / 2);

            for (int i = 0; i < 128; i++)
                this.connections23[i] = (int)(connection.Next(Amplitude) - Amplitude / 2);

            for (int i = 0; i < 32; i++)
                this.connections34[i] = (int)(connection.Next(Amplitude) - Amplitude / 2);

            for (int i = 0; i < 8; i++)
                this.connections45[i] = (int)(connection.Next(Amplitude) - Amplitude / 2);

            for (int i = 0; i < 2; i++)
                this.connections56[i] = (int)(connection.Next(Amplitude) - Amplitude / 2);
        }

        public float Calculate (float[] Input)
        {
            this.ResetNodes();

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 32; j++)
                    this.NodLine2[i] += Input[j] * this.connections12[i * 32 + j];

                for (int j = 0; j < 8; j++)
                    this.NodLine3[j] += this.NodLine2[i] * this.connections23[j * 16 + i];
            }

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 8; j++)
                    this.NodLine4[i] += this.NodLine3[j] * this.connections34[i * 8 + j];

            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 4; j++)
                    this.NodLine5[i] += this.NodLine4[j] * this.connections45[i * 4 + j];

            return this.NodLine5[0]* this.connections56[0] + this.NodLine5[0] * this.connections56[0];
        }

        public void ChangeConnection(int ConnectionLine, int ConnectionNumber, int NumberToAdd)
        {
            switch(ConnectionLine)
            {
                case 1:
                    this.connections12[ConnectionNumber] += NumberToAdd;
                    break;
                case 2:
                    this.connections23[ConnectionNumber] += NumberToAdd;
                    break;
                case 3:
                    this.connections34[ConnectionNumber] += NumberToAdd;
                    break;
                case 4:
                    this.connections45[ConnectionNumber] += NumberToAdd;
                    break;
                case 5:
                    this.connections56[ConnectionNumber] += NumberToAdd;
                    break;
            }
        }

        public void ChangeConnection(int ConnectionLine, int ConnectionNumber)
        {
            switch (ConnectionLine)
            {
                case 1:
                    this.connections12[ConnectionNumber] = -this.connections12[ConnectionNumber];
                    break;
                case 2:
                    this.connections23[ConnectionNumber] = -this.connections23[ConnectionNumber];
                    break;
                case 3:
                    this.connections34[ConnectionNumber] = -this.connections34[ConnectionNumber];
                    break;
                case 4:
                    this.connections45[ConnectionNumber] = -this.connections45[ConnectionNumber];
                    break;
                case 5:
                    this.connections56[ConnectionNumber] = -this.connections56[ConnectionNumber];
                    break;
            }
        }

        public void ResetNodes()
        {
            for (int j = 0; j < 16; j++)
                this.NodLine2[j] = 0;

            for (int j = 0; j < 8; j++)
                this.NodLine3[j] = 0;

            for (int j = 0; j < 4; j++)
                this.NodLine4[j] = 0;

            for (int j = 0; j < 2; j++)
                this.NodLine5[j] = 0;
        }

        public void SetNeuralNetwork (NeuralNetwork CopyFrom)
        {
            for (int i = 0; i < 512; i++)
                this.connections12[i] = CopyFrom.connections12[i];

            for (int i = 0; i < 128; i++)
                this.connections23[i] = CopyFrom.connections23[i];

            for (int i = 0; i < 32; i++)
                this.connections34[i] = CopyFrom.connections34[i];

            for (int i = 0; i < 8; i++)
                this.connections45[i] = CopyFrom.connections45[i];

            for (int i = 0; i < 2; i++)
                this.connections56[i] = CopyFrom.connections56[i];
        }

        public void OutputNetwork(int fitness)
        {
            string path = @"C:\Users\Никита\Documents\Visual Studio 2017\Projects\Forex Forecasting\Forex Forecasting\SavedNetworks\";

            path += "[" + fitness + " fitness] " + DateTime.Now.ToString("ss-mm-hh_dd-MM-yyyy") + ".txt";

            if (!File.Exists(path))
            {
                using (StreamWriter output = File.CreateText(path))
                {
                    output.WriteLine("First to second layer connections:");
                    foreach (int connection in this.connections12)
                    {
                        output.WriteLine(connection);
                    }

                    output.WriteLine("Second to third layer connections:");
                    foreach (int connection in this.connections23)
                    {
                        output.WriteLine(connection);
                    }

                    output.WriteLine("Third to fourth layer connections:");
                    foreach (int connection in this.connections34)
                    {
                        output.WriteLine(connection);
                    }

                    output.WriteLine("Fourth to fifth layer connections:");
                    foreach (int connection in this.connections45)
                    {
                        output.WriteLine(connection);
                    }

                    output.WriteLine("Fifth to sixth layer connections:");
                    foreach (int connection in this.connections56)
                    {
                        output.WriteLine(connection);
                    }
                }
            }
        }

        public void SetNetworkFromList (List<NeuralNetwork> networks)
        {
            this.connections12.Initialize();
            this.connections23.Initialize();
            this.connections34.Initialize();
            this.connections45.Initialize();
            this.connections56.Initialize();

            foreach (NeuralNetwork Network in networks)
            {
                for (int i = 0; i < 512; i++)
                    this.connections12[i] += Network.connections12[i];

                for (int i = 0; i < 128; i++)
                    this.connections23[i] += Network.connections23[i];

                for (int i = 0; i < 32; i++)
                    this.connections34[i] += Network.connections34[i];

                for (int i = 0; i < 8; i++)
                    this.connections45[i] += Network.connections45[i];

                for (int i = 0; i < 2; i++)
                    this.connections56[i] += Network.connections56[i];
            }
        }
    }

    public class Node<DataType>
    {
        private static int ID;

        private int _NodeID;
        private List<int> _ConnectedTo = new List<int>();
        private List<DataType> _IncomingData = new List<DataType>();

        public int NodeID
        {
            get { return _NodeID; }
            set { _NodeID = value; }
        }

        // List of Nodes ID, this node is connected to
        public List<int> ConnectedTo
        {
            get { return _ConnectedTo; }
            set { _ConnectedTo = value; }
        }

        // Data for this node to process
        public List<DataType> IncomingData
        {
            get { return _IncomingData; }
            set { _IncomingData = value; }
        }
        
        public void AddConnection (int NodeIDToConnect)
        {
            if (NodeIDToConnect < ID)
            {
                _ConnectedTo.Add(NodeIDToConnect);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        // Method to send data to this node for processing
        public void SendData (DataType data)
        {
            _IncomingData.Add(data);
        }
        
        // Static constructor to initialize static ID
        static Node()
        {
            ID = 0;
        }

        // Constructor, wich sets ID for this Node
        public Node ()
        {
            NodeID = GetNewID();
        }
        
        private int GetNewID ()
        {
            return ID++;
        }
    }

    public class NeuralNetworkCore
    {
        private List<Node<double>> Nodes = new List<Node<double>>();


    }
}
