using System;
using System.Windows.Forms;
using DevExpress.XtraCharts;

namespace ChartApproximation {
    public partial class Form1 : Form {
        private SeriesPoint editedSeriesPoint;
        private SeriesPoint selectedSeriesPoint;

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            InitializeChartDiagram();
            InitializeSourceSeires();
            DrawApproximatedSeries();
        }

        private void InitializeChartDiagram() {
            XYDiagram diagram = (XYDiagram)chartControl1.Diagram;

            // Configure diagram settings
            diagram.Margins.Top = 20;
            diagram.Margins.Right = 25;
            diagram.AxisX.Range.SetMinMaxValues(0, 11);
            diagram.AxisY.Range.SetMinMaxValues(0, 10);
        }

        private void InitializeSourceSeires() {
            Series scatteredSeries = new Series("Scattered", ViewType.Point);

            // Cpecify ScaleTypes
            scatteredSeries.ArgumentScaleType = ScaleType.Numerical;
            scatteredSeries.ValueScaleType = ScaleType.Numerical;

            // Fill series by points
            Random rnd = new Random();

            for(int i = 0; i < 10; i += 3) {
                scatteredSeries.Points.Add(new SeriesPoint(i + 1, Math.Round(rnd.NextDouble() * 10, 2)));
            }

            // Configure series label and marker options
            scatteredSeries.Label.PointOptions.ValueNumericOptions.Format = NumericFormat.FixedPoint;
            scatteredSeries.Label.PointOptions.ValueNumericOptions.Precision = 2;
            ((PointSeriesView)scatteredSeries.View).PointMarkerOptions.Size = 15;
            ((PointSeriesView)scatteredSeries.View).PointMarkerOptions.Kind= MarkerKind.Square;

            chartControl1.Series.Clear();
            chartControl1.Series.Add(scatteredSeries);
        }

        private void DrawApproximatedSeries() {
            double x0 = double.MaxValue;
            double xn = double.MinValue;

            foreach(SeriesPoint seriesPoint in chartControl1.Series["Scattered"].Points) {
                if(Convert.ToDouble(seriesPoint.Argument) < x0)
                    x0 = Convert.ToDouble(seriesPoint.Argument);
                if(Convert.ToDouble(seriesPoint.Argument) > xn)
                    xn = Convert.ToDouble(seriesPoint.Argument);
            }

            if(chartControl1.Series["Approximated"] == null) {
                Series approximatedSeries = new Series("Approximated", ViewType.Line);

                approximatedSeries.ArgumentScaleType = ScaleType.Numerical;
                approximatedSeries.ValueScaleType = ScaleType.Numerical;
                
                approximatedSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False;
                //((LineSeriesView)approximatedSeries.View).PointMarkerOptions.Size = 5;
                ((LineSeriesView)approximatedSeries.View).MarkerVisibility = DevExpress.Utils.DefaultBoolean.False;

                chartControl1.Series.Add(approximatedSeries);
            }

            chartControl1.Series["Approximated"].Points.Clear();
            for(double x = x0; x < xn; x += 0.2) {
                chartControl1.Series["Approximated"].Points.Add(new SeriesPoint(x, new double[] { Approximate(x) }));
            }

        }

        private void chartControl1_ObjectHotTracked(object sender, HotTrackEventArgs e) {
            if(e.HitInfo.InSeries && ((Series)e.HitInfo.Series).Name == "Scattered") {
                if(e.HitInfo.SeriesPoint != null) {
                    selectedSeriesPoint = e.HitInfo.SeriesPoint;

                    if((Form.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
                        editedSeriesPoint = e.HitInfo.SeriesPoint;
                    else
                        editedSeriesPoint = null;
                }
            }

            if(editedSeriesPoint != null) {
                DiagramCoordinates diagramCoordinates = ((XYDiagram)chartControl1.Diagram).PointToDiagram(chartControl1.PointToClient(Form.MousePosition));

                if(diagramCoordinates.IsEmpty)
                    return;

                editedSeriesPoint.Argument = diagramCoordinates.NumericalArgument.ToString();
                editedSeriesPoint.Values[0] = diagramCoordinates.NumericalValue;

                DrawApproximatedSeries();
            }

            chartControl1.Cursor = (editedSeriesPoint != null || (e.HitInfo.InSeries && ((Series)e.HitInfo.Series).Name == "Scattered") ? Cursors.Hand : Cursors.Default);
        }

        private void chartControl1_KeyDown(object sender, KeyEventArgs e) {
            if(e.KeyCode == Keys.Delete) {
                if(selectedSeriesPoint != null) {
                    chartControl1.Series["Scattered"].Points.Remove(selectedSeriesPoint);
                    DrawApproximatedSeries();
                }
            }
            else if(e.KeyCode == Keys.Insert) {
                DiagramCoordinates diagramCoordinates = ((XYDiagram)chartControl1.Diagram).PointToDiagram(chartControl1.PointToClient(Form.MousePosition));

                if(diagramCoordinates.IsEmpty)
                    return;

                chartControl1.Series["Scattered"].Points.Add(new SeriesPoint(diagramCoordinates.NumericalArgument, new double[] { diagramCoordinates.NumericalValue }));
                DrawApproximatedSeries();
            }
        }

        // Your custom calculation logic goes here (it's a fake implementation):
        private double Approximate(double x) {
            Series series = chartControl1.Series["Scattered"];

            int n = chartControl1.Series["Scattered"].Points.Count;
            double argSum, valSum, argvalSum, argSqrSum;

            argSum = valSum = argvalSum = argSqrSum = 0.0;

            for(int i = 0; i < n; i++) {
                argSum += Convert.ToDouble(series.Points[i].Argument);
                valSum += Math.Log(series.Points[i].Values[0]);
                argvalSum += Convert.ToDouble(series.Points[i].Argument) * Math.Log(series.Points[i].Values[0]);
                argSqrSum += Convert.ToDouble(series.Points[i].Argument) * Convert.ToDouble(series.Points[i].Argument);
            }

            double a = (n * argvalSum - argSum * valSum) / (n * argSqrSum - argSum * argSum);
            double b = (valSum - a * argSum) / n;

            return Math.Exp(a * x + b);
        }

    }

}