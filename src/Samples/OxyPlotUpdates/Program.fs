module Elmish.WPF.Samples.OxyPlot.Program

open System
open Elmish
open Elmish.WPF

open OxyPlot
open OxyPlot.Series

let private buttonLabelStart = "Start Updates"
let private buttonLabelStop = "Stop Updates"

type BoxPoint =
  { Lw: float
    Uw: float
    Bb: float
    Bt: float
    Median: float
    Mean: float }
  static member init =
    [ { Lw = 128.5
        Bb = 10.
        Median = 219.
        Bt = 210.
        Uw = 225.5
        Mean = 117. } 
      { Lw = 221.5
        Bb = 10.
        Median = 327.0
        Bt = 318.5
        Uw = 402.
        Mean = 218.0 } 
      { Lw = 227.5
        Bb = 01.
        Median = 326.0
        Bt = 336.5
        Uw = 349.
        Mean = 351.0 } 
      { Lw = 185.
        Bb = 30.
        Median = 119.5
        Bt = 201.5
        Uw = 215.
        Mean = 516. } ]
  
type Model =
  { LineSeriesData: (float * float) array
    LinePlot: PlotModel
    BoxPlot: PlotModel
    Timer: Timers.Timer
    IsTimerRunning: bool
    Count: float }

type Msg =
  | GotLineSeriesData of (float * float) array
  | ToggleTimer
  | Tick

let init =
  let timer = new Timers.Timer(60.)

  let timerTick dispatch = timer.Elapsed.Add (fun _ -> dispatch Tick)

  let linePm = PlotModel(Title = "Test Plot")
  let lineSeries = LineSeries(Title = "Test Line Series", MarkerType = MarkerType.Circle)
  linePm.Series.Add lineSeries

  let boxPm = PlotModel(Title = "Test Box Plot" )
  let boxSeries = BoxPlotSeries(Title = "Test Box Plot Series")
  BoxPoint.init |> List.mapi (fun i p -> BoxPlotItem(float i, p.Lw, p.Bb, p.Median, p.Bt, p.Uw)) |> List.iter boxSeries.Items.Add
  boxPm.Series.Add boxSeries

  { LineSeriesData = [||]
    IsTimerRunning = false
    Count = 0.0
    LinePlot = linePm
    BoxPlot = boxPm
    Timer = timer }, Cmd.ofSub timerTick

let private generateDataAsync offset =
  let rand = Random()
  async {

    // Simulate some latency
    do! Async.Sleep(rand.NextDouble() * 10.0 |> int)

    return [| 0 .. 99 |] |> Array.map (float >> fun i -> i, sin (i + offset))
  }
  
let update msg m =
  match msg with
  | ToggleTimer ->
    if m.IsTimerRunning then m.Timer.Stop() else m.Timer.Start()
    { m with IsTimerRunning = not m.IsTimerRunning }, Cmd.none
  | Tick ->
    { m with Count = m.Count + 1.0 },
    Cmd.OfAsync.perform generateDataAsync m.Count GotLineSeriesData
  | GotLineSeriesData data ->
    // Update the series and redraw the plot 
    let s = m.LinePlot.Series.[0] :?> LineSeries
    s.Points.Clear()
    data |> Array.map (fun (x, y) -> DataPoint(x, y)) |> s.Points.AddRange
    m.LinePlot.InvalidatePlot(true)

    { m with LineSeriesData = data }, Cmd.none

let bindings () : Binding<Model, Msg> list = [

  "LinePlotModel" |> Binding.oneWay (fun m -> m.LinePlot)
  "BoxPlotModel" |> Binding.oneWay (fun m -> m.BoxPlot)

  "ToggleStartStop" |> Binding.cmd (fun m -> ToggleTimer)
  "ToggleStartStopLabel" |> Binding.oneWay (fun m -> if m.IsTimerRunning then buttonLabelStop else buttonLabelStart)

  ]


[<EntryPoint; STAThread>]
let main _ =
  Program.mkProgramWpf (fun () -> init) update bindings
  //|> Program.withConsoleTrace
  |> Program.runWindowWithConfig
    //{ ElmConfig.Default with LogConsole = true; Measure = true }
    ElmConfig.Default
    (MainWindow())
