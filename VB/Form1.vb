Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports DevExpress.XtraCharts

Namespace ChartApproximation
	Partial Public Class Form1
		Inherits Form
		Private editedSeriesPoint As SeriesPoint
		Private selectedSeriesPoint As SeriesPoint

		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub Form1_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
			InitializeChartDiagram()
			InitializeSourceSeires()
			DrawApproximatedSeries()
		End Sub

		Private Sub InitializeChartDiagram()
			Dim diagram As XYDiagram = CType(chartControl1.Diagram, XYDiagram)

			' Configure diagram settings
			diagram.Margins.Top = 20
			diagram.Margins.Right = 25
			diagram.AxisX.Range.SetMinMaxValues(0, 11)
			diagram.AxisY.Range.SetMinMaxValues(0, 10)
		End Sub

		Private Sub InitializeSourceSeires()
			Dim scatteredSeries As New Series("Scattered", ViewType.Point)

			' Cpecify ScaleTypes
			scatteredSeries.ArgumentScaleType = ScaleType.Numerical
			scatteredSeries.ValueScaleType = ScaleType.Numerical

			' Fill series by points
			Dim rnd As New Random()

			For i As Integer = 0 To 9 Step 3
				scatteredSeries.Points.Add(New SeriesPoint(i + 1, Math.Round(rnd.NextDouble() * 10, 2)))
			Next i

			' Configure series label and marker options
			scatteredSeries.Label.PointOptions.ValueNumericOptions.Format = NumericFormat.FixedPoint
			scatteredSeries.Label.PointOptions.ValueNumericOptions.Precision = 2
			CType(scatteredSeries.View, PointSeriesView).PointMarkerOptions.Size = 15
			CType(scatteredSeries.View, PointSeriesView).PointMarkerOptions.Kind= MarkerKind.Square

			chartControl1.Series.Clear()
			chartControl1.Series.Add(scatteredSeries)
		End Sub

		Private Sub DrawApproximatedSeries()
			Dim x0 As Double = Double.MaxValue
			Dim xn As Double = Double.MinValue

			For Each seriesPoint As SeriesPoint In chartControl1.Series("Scattered").Points
				If Convert.ToDouble(seriesPoint.Argument) < x0 Then
					x0 = Convert.ToDouble(seriesPoint.Argument)
				End If
				If Convert.ToDouble(seriesPoint.Argument) > xn Then
					xn = Convert.ToDouble(seriesPoint.Argument)
				End If
			Next seriesPoint

			If chartControl1.Series("Approximated") Is Nothing Then
				Dim approximatedSeries As New Series("Approximated", ViewType.Line)

				approximatedSeries.ArgumentScaleType = ScaleType.Numerical
				approximatedSeries.ValueScaleType = ScaleType.Numerical

				approximatedSeries.LabelsVisibility = DevExpress.Utils.DefaultBoolean.False
				'((LineSeriesView)approximatedSeries.View).PointMarkerOptions.Size = 5;
				CType(approximatedSeries.View, LineSeriesView).MarkerVisibility = DevExpress.Utils.DefaultBoolean.False

				chartControl1.Series.Add(approximatedSeries)
			End If

			chartControl1.Series("Approximated").Points.Clear()
			For x As Double = x0 To xn - 1 Step 0.2
				chartControl1.Series("Approximated").Points.Add(New SeriesPoint(x, New Double() { Approximate(x) }))
			Next x

		End Sub

		Private Sub chartControl1_ObjectHotTracked(ByVal sender As Object, ByVal e As HotTrackEventArgs) Handles chartControl1.ObjectHotTracked
			If e.HitInfo.InSeries AndAlso (CType(e.HitInfo.Series, Series)).Name = "Scattered" Then
				If e.HitInfo.SeriesPoint IsNot Nothing Then
					selectedSeriesPoint = e.HitInfo.SeriesPoint

					If (Form.MouseButtons And MouseButtons.Left) = MouseButtons.Left Then
						editedSeriesPoint = e.HitInfo.SeriesPoint
					Else
						editedSeriesPoint = Nothing
					End If
				End If
			End If

			If editedSeriesPoint IsNot Nothing Then
				Dim diagramCoordinates As DiagramCoordinates = (CType(chartControl1.Diagram, XYDiagram)).PointToDiagram(chartControl1.PointToClient(Form.MousePosition))

				If diagramCoordinates.IsEmpty Then
					Return
				End If

				editedSeriesPoint.Argument = diagramCoordinates.NumericalArgument.ToString()
				editedSeriesPoint.Values(0) = diagramCoordinates.NumericalValue

				DrawApproximatedSeries()
			End If

			chartControl1.Cursor = (If(editedSeriesPoint IsNot Nothing OrElse (e.HitInfo.InSeries AndAlso (CType(e.HitInfo.Series, Series)).Name = "Scattered"), Cursors.Hand, Cursors.Default))
		End Sub

		Private Sub chartControl1_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles chartControl1.KeyDown
			If e.KeyCode = Keys.Delete Then
				If selectedSeriesPoint IsNot Nothing Then
					chartControl1.Series("Scattered").Points.Remove(selectedSeriesPoint)
					DrawApproximatedSeries()
				End If
			ElseIf e.KeyCode = Keys.Insert Then
				Dim diagramCoordinates As DiagramCoordinates = (CType(chartControl1.Diagram, XYDiagram)).PointToDiagram(chartControl1.PointToClient(Form.MousePosition))

				If diagramCoordinates.IsEmpty Then
					Return
				End If

				chartControl1.Series("Scattered").Points.Add(New SeriesPoint(diagramCoordinates.NumericalArgument, New Double() { diagramCoordinates.NumericalValue }))
				DrawApproximatedSeries()
			End If
		End Sub

		' Your custom calculation logic goes here (it's a fake implementation):
		Private Function Approximate(ByVal x As Double) As Double
			Dim series As Series = chartControl1.Series("Scattered")

			Dim n As Integer = chartControl1.Series("Scattered").Points.Count
			Dim argSum, valSum, argvalSum, argSqrSum As Double

			argSqrSum = 0.0
			argvalSum = argSqrSum
			valSum = argvalSum
			argSum = valSum

			For i As Integer = 0 To n - 1
				argSum += Convert.ToDouble(series.Points(i).Argument)
				valSum += Math.Log(series.Points(i).Values(0))
				argvalSum += Convert.ToDouble(series.Points(i).Argument) * Math.Log(series.Points(i).Values(0))
				argSqrSum += Convert.ToDouble(series.Points(i).Argument) * Convert.ToDouble(series.Points(i).Argument)
			Next i

			Dim a As Double = (n * argvalSum - argSum * valSum) / (n * argSqrSum - argSum * argSum)
			Dim b As Double = (valSum - a * argSum) / n

			Return Math.Exp(a * x + b)
		End Function

	End Class

End Namespace