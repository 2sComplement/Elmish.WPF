namespace Elmish.Samples.Counter

open System
open Elmish
open Elmish.WPF

module Types =

    type MouseAction =
        | Up
        | Down

    type ClockMsg =
        | Tick of DateTime

    type ClockModel =
        { Time: DateTime }

    type Msg =
        | ClockMsg of ClockMsg
        | Increment
        | Decrement
        | SetStepSize of int
        | MouseEvent of MouseAction
        | KeyboardEvent of string

    type Model = 
        { Count: int
          StepSize: int
          Clock: ClockModel }


module State =
    open Types
    open System.Windows.Input
     
    let init() = { Count = 0; StepSize = 1; Clock = { Time = DateTime.Now }}

    let timerTick dispatch =
        let timer = new System.Timers.Timer(1000.)
        timer.Elapsed.Subscribe (fun _ -> dispatch (System.DateTime.Now |> Tick |> ClockMsg)) |> ignore
        timer.Enabled <- true
        timer.Start()

    let subscribe model =
        Cmd.ofSub timerTick

    let clockUpdate (msg:ClockMsg) (model:ClockModel) =
        match msg with
        | Tick t -> { model with Time = t }

    let update (msg:Msg) (model:Model) =
        match msg with
        | Increment -> { model with Count = model.Count + model.StepSize }
        | Decrement -> { model with Count = model.Count - model.StepSize }
        | SetStepSize n -> { model with StepSize = n }
        | ClockMsg m -> { model with Clock = clockUpdate m model.Clock }
        | MouseEvent Up -> model
        | MouseEvent Down -> model
        | KeyboardEvent key -> Console.WriteLine(key); model
        

module App =
    open State
    open Types

    let view _ _ = 
        let clockViewBinding : ViewBindings<ClockModel,ClockMsg> =
            [ "Time" |> Binding.oneWay (fun m -> m.Time) ]

        [ "Increment" |> Binding.cmd (fun _ m -> Increment)
          "Decrement" |> Binding.cmdIf (fun _ m -> Decrement) (fun _ m -> m.StepSize = 1)
          "Count" |> Binding.oneWay (fun m -> m.Count)
          "StepSize" |> Binding.twoWay (fun m -> (double m.StepSize)) (fun v m -> v |> int |> SetStepSize)
          "Clock" |> Binding.model (fun m -> m.Clock) clockViewBinding ClockMsg
          "TestDown" |> Binding.cmd (fun _ m -> MouseEvent Down)
          "Escape" |> Binding.cmd (fun _ m -> KeyboardEvent "esc") ]

    [<EntryPoint;STAThread>]
    let main argv = 
        Program.mkSimple init update view
//        |> Program.withConsoleTrace
        |> Program.withSubscription subscribe
        |> Program.runWindow (Elmish.CounterViews.MainWindow())