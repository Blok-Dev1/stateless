using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Linq.Expressions.Expression;
using System;
using System.Linq.Dynamic.Core;
using System.Diagnostics;
using System.Xml.Linq;

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

        //for (int i = 0; i <= 5; i++)
        //{
        //    timeevent.Wait(1);
        //    robot.Event(timeevent);
        //}

        var en = "Entity Name";          // Plan CrossCut
        var la = "Location Activity";   // X/C -- SAP Req
        var lt = "Location Type";       // XCT
        var period = "Period";          // 1,2,3
        var aorstate = "AORState";

        var xcut = new ScheduleTask(1);
        xcut.Name = "Xcut";
        xcut.Parent = "";
        xcut.Add(en, "Plan CrossCut")
            .Add(la, "X/C")
            .Add(lt, "XCT")
            .Add(period, "1");

        var raise1  = new ScheduleTask(2);
        raise1.Name = "Raise";
        raise1.Parent = xcut.Name;
        raise1.Add(en, "Plan Raise")
          .Add(la, "RSE")
          .Add(lt, "RSE")
          .Add(period, "2");

        var raise2 = new ScheduleTask(3);
        raise2.Name = "Raise";
        raise2.Parent = xcut.Name;
        raise2.Add(en, "Plan Raise")
          .Add(la, "RSE")
          .Add(lt, "RSE")
          .Add(period, "3");

        var ledging = new ScheduleTask(4);
        ledging.Name = "Ledging";
        ledging.Parent = raise1.Name;
        ledging.Add(en, "Plan Ledge")
          .Add(la, "LED")
          .Add(lt, "LED")
          .Add(period, "4");

        var panel_l1 = new ScheduleTask(5);
        panel_l1.Name = "Panel_L1";
        panel_l1.Parent = raise2.Name;
        panel_l1.Add(en, "Plan Panel")
          .Add(la, "PLN")
          .Add(lt, "PLN")
          .Add(period, "5")
          .Add(aorstate, "");

        var panel_r1 = new ScheduleTask(6);
        panel_r1.Name = "Panel_R1";
        panel_r1.Parent = raise1.Name;
        panel_r1.Add(en, "Plan Panel")
          .Add(la, "PLN")
          .Add(lt, "PLN")
          .Add(period, "5")
          .Add(aorstate, "");

        var schedule = new List<ScheduleTask> { xcut , raise1, raise2, ledging, panel_l1 , panel_r1 };

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

        //  Conditions
        //  SEQ     TriggerColumnName1	    TriggerColumnName2	    TriggerColumnName3	StartorEnd	InfraOrPanel
        //  0       NULL                    NULL                    NULL                NULL        NULL
        //  1       Plan Raise              RSE                     RSE                 START       Infra
        //  2       Plan Raise              RSE                     RSE                 END         Infra
        //  3       NULL                    LED                     NULL                Start       Infra
        //  4       NULL                    LED                     NULL                END         Infra
        //  5       Plan Panel              PNL                     PNL                 START       Panel
        //  6       NULL                    NULL                    PNL                 END         Panel

        // Group By Riase Name!

        // target
        var targetCondition = new Condition(0);
        targetCondition.Add(new Trigger { Name = "EntityName", Value = "Plan Panel" });
        targetCondition.Name = "Stoping IMS State";

        // rule
        var rule0 = new Rule(0);
        rule0.Name = "N/A";
        rule0.TargetState = "01 N/A";
        rule0.IsMandatory = true;

        var rule1 = new Rule(1);
        rule1.Name = "Raise started";
        rule1.TargetState = "02 NIR";
        rule1.IsMandatory = false;

        var rule2 = new Rule(2);
        rule2.Name = "Raise Holed";
        rule2.TargetState = "02 NIR";
        rule2.IsMandatory = true;
        rule2.RuleType = Rule.eRuleType.Last;

        var rule3 = new Rule(3);
        rule3.Name = "Ledge Started";
        rule3.TargetState = "03 Holed";
        rule3.IsMandatory = false;


        // conditions
        var cond0 = new Condition(0);
        cond0.Add(new Trigger{ Name = "EntityName", Value = "NULL"});
        cond0.Add(new Trigger{ Name = "Location Activity", Value = "NULL"});
        cond0.Add(new Trigger{ Name = "Location Type", Value = "NULL"});
        rule0.Condition = cond0;

        var cond1 = new Condition(1);
        cond1.ConditionType = Condition.eExecutedAt.Start;
        cond1.Add(new Trigger{ Name = "EntityName", Value = "Plan Raise"});
        cond1.Add(new Trigger{ Name = "Location Activity", Value = "RSE"});
        cond1.Add(new Trigger{ Name = "Location Type", Value = "RSE"});
        rule1.Condition = cond1;

        var cond2 = new Condition(2);
        cond2.ConditionType = Condition.eExecutedAt.End;
        cond2.Add(new Trigger { Name = "EntityName", Value = "Plan Raise" });
        cond2.Add(new Trigger { Name = "Location Activity", Value = "RSE" });
        cond2.Add(new Trigger { Name = "Location Type", Value = "RSE" });
        rule2.Condition = cond2;

        var cond3 = new Condition(3);
        cond3.ConditionType = Condition.eExecutedAt.Start;
        cond3.Add(new Trigger { Name = "EntityName", Value = "NULL" });
        cond3.Add(new Trigger { Name = "Location Activity", Value = "LED" });
        cond3.Add(new Trigger { Name = "Location Type", Value = "NULL" });
        rule3.Condition = cond3;

        // find Targets
        // WHERE [EntityName] = 'Plan Panel'  ... 
        var findtarget = new Trigger { Name = "EntityName", Value = "Plan Panel" };


        var target1 = schedule.Where(s => s.Attributes.ContainsKey("Entity Name"));
        var target2 = schedule.Where(x => x.Attributes["Entity Name"] == "Plan Panel");
        //var source = schedule.Where(x=> x.Attributes["Entity Name"] = Plan Raise AND[Location Activity] = RSE AND[Location Type] = RSE)


        var qry = TextFilter(schedule.AsQueryable(), "Panel_L1").ToList();

        // Define the dynamic query expression
        //dotnet add package System.Linq.Dynamic.Core
        //  string expression = "x => x > 3";
        string expression = "x => x.Attributes[\"Entity Name\"] == \"Plan Panel\"";

        // Execute the dynamic query using the Dynamic LINQ library
        var results = schedule.AsQueryable().Where(expression).ToList();


        expression = "x => x.Attributes[\"Entity Name\"] == \"Plan Raise\" " +
            "&& x.Attributes[\"Location Activity\"] == \"RSE\" " +
            "&& x.Attributes[\"Location Type\"] == \"RSE\" ";

        // Execute the dynamic query using the Dynamic LINQ library
        var results2 = schedule.AsQueryable().Where(expression).ToList();


        // check rule
        // ALL --- WHERE [Entity Name] = NULL AND [Location Activity] = NULL AND [Location Type] = NULL
        // WHERE [Entity Name] = Plan Raise AND [Location Activity] = RSE AND [Location Type] = RSE

        // Period Count 5
        for (int i = 0; i < 5; i++)
        {

        }
        // Wait for user
        Console.Read();
    }

    // using static System.Linq.Expressions.Expression;

    static IQueryable<T> TextFilter<T>(IQueryable<T> source, string term)
    {
        if (string.IsNullOrEmpty(term)) { return source; }

        // T is a compile-time placeholder for the element type of the query.
        Type elementType = typeof(T);

        // Get all the string properties on this specific type.
        PropertyInfo[] stringProperties = elementType
            .GetProperties()
            .Where(x => x.PropertyType == typeof(string))
            .ToArray();
        if (!stringProperties.Any()) { return source; }

        // Get the right overload of String.Contains
        MethodInfo containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

        // Create a parameter for the expression tree:
        // the 'x' in 'x => x.PropertyName.Contains("term")'
        // The type of this parameter is the query's element type
        ParameterExpression prm = Parameter(elementType);

        // Map each property to an expression tree node
        IEnumerable<Expression> expressions = stringProperties
            .Select(prp =>
                // For each property, we have to construct an expression tree node like x.PropertyName.Contains("term")
                Call(                  // .Contains(...) 
                    Property(          // .PropertyName
                        prm,           // x 
                        prp
                    ),
                    containsMethod,
                    Constant(term)     // "term" 
                )
            );

        // Combine all the resultant expression nodes using ||
        Expression body = expressions
            .Aggregate((prev, current) => Or(prev, current));

        // Wrap the expression body in a compile-time-typed lambda expression
        Expression<Func<T, bool>> lambda = Lambda<Func<T, bool>>(body, prm);

        // Because the lambda is compile-time-typed (albeit with a generic parameter), we can use it with the Where method
        return source.Where(lambda);
    }

    public class Rule
    {
        public enum eRuleType
        {
            None, // Default - Not Specified
            First,
            Last,
        }
        public int  SequenceNo {get; }
        public string Name {get; set;}
        public string TargetState {get; set;}
        public eRuleType RuleType { get; set;}
        public Rule(int sequenceno)
        {
            SequenceNo = sequenceno;
        }

        public bool IsMandatory {get; set;}
        public Condition Condition { get; set;}
    }

    public class Condition: List<Trigger>
    {
        public string Name { get; set;}
        public enum eExecutedAt
        {
            None,
            Start,
            End
        }
        public int  SequenceNo {get; }

        public Condition(int sequenceno)
        {
            SequenceNo = sequenceno;
        }

        public eExecutedAt ConditionType { get; set; }

    }

    public class Trigger
    {
        
        public string Name { get; set; }
        public string Value { get; set; }
        
    }

    public class ScheduleTask
    {
        public int Id { get; set;}
        public string Name { get; set; }
        public string Parent { get; set; }
        public int Start { get; set; }
        public int End { get; set; }

        public Dictionary<string, string> Attributes { get; set; }

        public ScheduleTask(int id)
        {
            Id = id;

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
