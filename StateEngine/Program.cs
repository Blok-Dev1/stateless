using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace StateEngine;

class Program
{
    static void Main(string[] args)
    {
        //// Setup context in a state

        //var context = new Context(new RedState());

        //// Issue requests, which toggles state

        //context.Request();
        //context.Request();
        //context.Request();
        //context.Request();

        string flip = "flip";
        var red = new RedState();
        var green = new GreenState();
        var machine = new StateMachine<State, string>(red);
        machine.State(red).Allow(flip, green);
        machine.State(green).Allow(flip, red);

        var current = machine.Current;
        machine.Event(flip);
        current = machine.Current;
        machine.Event(flip);
        current = machine.Current;
        machine.Event(flip);
        current = machine.Current;

        // consider colour blind
        var redl = new Top(); // Red 
        var orangel = new Middle(); // Orange
        var greenl = new Bottom(); // Green
        var timeevent = new TimedEvent();

        Action<ITrafficLight> light = (l) => { Console.WriteLine($"Robot is: {l.Colour}"); };

        var robot = new StateMachine<ITrafficLight, TimedEvent>(redl);
        robot.State(redl).Allow(timeevent, greenl).OnStateEntry(light);
        robot.State(greenl).Allow(timeevent, orangel).OnStateEntry(light);
        robot.State(orangel).Allow(timeevent, redl).OnStateEntry(light);

        robot.Current.ExecuteEntry();

        for (int i = 0; i <= 5; i++)
        {
            timeevent.Wait(1);
            robot.Event(timeevent);
        }

        var en = "EntityName";          // Plan CrossCut
        var la = "Location Activity";   // X/C -- SAP Req
        var lt = "Location Type";       // XCT
        var period = "Period";          // 1,2,3
        var aorstate = "AORState";

        var xcut = new ScheduleTask();
        xcut.Name = "Xcut";
        xcut.Add(en, "Plan CrossCut")
            .Add(la, "X/C")
            .Add(lt, "XCT")
            .Add(period, "1");

        var raise1  = new ScheduleTask();
        raise1.Name = "Raise";
        raise1.Parent = xcut.Name;
        raise1.Add(en, "Plan Riase")
          .Add(la, "RSE")
          .Add(lt, "RSE")
          .Add(period, "2");

        var raise2 = new ScheduleTask();
        raise2.Name = "Raise";
        raise2.Parent = xcut.Name;
        raise2.Add(en, "Plan Riase")
          .Add(la, "RSE")
          .Add(lt, "RSE")
          .Add(period, "3");

        var ledging = new ScheduleTask();
        ledging.Name = "Ledging";
        ledging.Parent = raise1.Name;
        ledging.Add(en, "Plan Ledge")
          .Add(la, "LED")
          .Add(lt, "LED")
          .Add(period, "4");

        var panel_l1 = new ScheduleTask();
        panel_l1.Name = "Panel_L1";
        panel_l1.Parent = raise2.Name;
        panel_l1.Add(en, "Plan Panel")
          .Add(la, "PLN")
          .Add(lt, "PLN")
          .Add(period, "5")
          .Add(aorstate, "");

        var panel_r1 = new ScheduleTask();
        panel_r1.Name = "Panel_R1";
        panel_r1.Parent = raise1.Name;
        panel_r1.Add(en, "Plan Panel")
          .Add(la, "PLN")
          .Add(lt, "PLN")
          .Add(period, "5")
          .Add(aorstate, "");

        // AORConfiguration_cfg

        //TimeSnapShotColumn	StopingColumn	StopingValue	TriggerColumnName1	TriggerColumnName2	TriggerColumnName3	CumulativeMeasureColumnName1	CumulativeMeasureColumnName2
        //[Period]              [EntityName]    Plan Panel       [EntityName]       [Location Activity]  [Location Type]    [Extraction Quantity]           [Area Ledging]

        //TriggerSequence	TriggerName	    AORState	    AORStateRuleName	AORStateRuleType	    IsMandatory
        //      0	        N/A	            01 N/A	        Default		                                1
        //      1	        Raise started	02 NIR	        NULL		                                0
        //      2	        Raise Holed	    02 NIR	        NIR1	            Last	                1
        //      3	        Ledge Started	03 Holed	    Default		                                0
        //      4 	        Ledge Completed	04 Ledged	    Default		                                1
        //      5	        Stoping Started	05 IMS	        First		                                0
        //      6	        Final PNL Act	06 StopingEnd	Default		                                0


        //  SEQ     TriggerColumnName1	    TriggerColumnName2	    TriggerColumnName3	StartorEnd	InfraOrPanel
        //  0       NULL                    NULL                    NULL                NULL        NULL
        //  1       Plan Raise              RSE                     RSE                 START       Infra
        //  2       Plan Raise              RSE                     RSE                 END         Infra
        //  3       NULL                    LED                     NULL                Start       Infra
        //  4       NULL                    LED                     NULL                END         Infra
        //  5       Plan Panel              PNL                     PNL                 START       Panel
        //  6       NULL                    NULL                    PNL                 END         Panel

        // Group By Riase Name!

        // Wait for user
        Console.ReadKey();
    }

    public class Trigger
    {
        public string Target { get; set; }
        public string TargetValue { get; set; }
    }

    public class ScheduleTask
    {
        public string Name { get; set; }
        public string Parent { get; set; }
        public int Start { get; set; }
        public int End { get; set; }

        public Dictionary<string, string> Attributes { get; set; }

        public ScheduleTask()
        {
            Attributes = new Dictionary<string, string>();
        }

        public ScheduleTask Add(string name, string value)
        {
            Attributes.Add(name, value);

            return this;
        }
    }

    public class TimedEvent
    {
        public void Wait(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }
    }

    public interface ITrafficLight
    {
        string Colour { get; }
    }

    public class Top : ITrafficLight
    {
        public string Colour { get => "Red"; }
    }

    public class Middle : ITrafficLight
    {
        public string Colour { get => "Orange"; }
    }

    public class Bottom : ITrafficLight
    {
        public string Colour { get => "Green"; }
    }

    /// <summary>
    /// A 'ConcreteState' class
    /// </summary>

    public class RedState : State
    {
        public override void Handle(Context context)
        {
            context.State = new GreenState();
        }
    }

    /// <summary>
    /// A 'ConcreteState' class
    /// </summary>

    public class GreenState : State
    {
        public override void Handle(Context context)
        {
            context.State = new RedState();
        }
    }
}
