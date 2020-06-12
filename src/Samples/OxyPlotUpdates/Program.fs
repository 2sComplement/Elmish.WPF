﻿module Elmish.WPF.Samples.OxyPlot.Program

open System
open Elmish
open Elmish.WPF

open OxyPlot
open OxyPlot.Series

let private buttonLabelStart = "Start Updates"
let private buttonLabelStop = "Stop Updates"
  
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

  let pm = PlotModel(Title = "Test Plot")
  let series = LineSeries(Title = "Test Series", MarkerType = MarkerType.Circle)
  pm.Series.Add series

  { LineSeriesData = [||]
    IsTimerRunning = false
    Count = 0.0
    LinePlot = pm
    BoxPlot = PlotModel()
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
