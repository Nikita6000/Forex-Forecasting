using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Forex_Forecasting
{
    public partial class TestRun : Form
    {
        public TestRun()
        {
            InitializeComponent();
        }

        private void TestRun_Load(object sender, EventArgs e)
        {

        }

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
                data.chart.Series[0].Points.AddY(data.account);
                data.chart.Update();
            }
        }

        public void SetChart(double account)
        {
            InputThreadData data = new InputThreadData(chart1, account);

            UpdateChart(data);
        }

    }
}
