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
using System.Reflection.Metadata;
using System.Data.Common;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

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

        // source rules
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
        cond0.Add(new Trigger{ Name = en, Value = "NULL"});
        cond0.Add(new Trigger{ Name = "Location Activity", Value = "NULL"});
        cond0.Add(new Trigger{ Name = "Location Type", Value = "NULL"});
        rule0.Condition = cond0;

        var cond1 = new Condition(1);
        cond1.ConditionType = Condition.eExecutedAt.Start;
        cond1.Add(new Trigger{ Name = en, Value = "Plan Raise"});
        cond1.Add(new Trigger{ Name = "Location Activity", Value = "RSE"});
        cond1.Add(new Trigger{ Name = "Location Type", Value = "RSE"});
        rule1.Condition = cond1;

        var cond2 = new Condition(2);
        cond2.ConditionType = Condition.eExecutedAt.End;
        cond2.Add(new Trigger { Name = en, Value = "Plan Raise" });
        cond2.Add(new Trigger { Name = "Location Activity", Value = "RSE" });
        cond2.Add(new Trigger { Name = "Location Type", Value = "RSE" });
        rule2.Condition = cond2;

        var cond3 = new Condition(3);
        cond3.ConditionType = Condition.eExecutedAt.Start;
        cond3.Add(new Trigger { Name = en, Value = "NULL" });
        cond3.Add(new Trigger { Name = "Location Activity", Value = "LED" });
        cond3.Add(new Trigger { Name = "Location Type", Value = "NULL" });
        rule3.Condition = cond3;

        // find Targets
        // WHERE [EntityName] = 'Plan Panel'  ... 
        var targetrule = new Rule(0);
        targetrule.Name = "Find Target";

        var tc = new Condition(0);
        var flttarget = new Trigger { Name = "Entity Name", Value = "Plan Panel" };
        tc.Add(flttarget);
        targetrule.Condition = tc;


        // NA
        var target1 = schedule.Where(s => s.Attributes.ContainsKey("Entity Name"));
        var target2 = schedule.Where(x => x.Attributes["Entity Name"] == "Plan Panel");
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
        //expression = "x => x.Attributes[\"{0}\"] == \"{1}\" ";
        //// Generate Target filter
        //var filter = "";
        //foreach (var c in targetrule.Condition)
        //{
        //    filter += string.Format(expression, c.Name, c.Value);

        //    if (targetrule.Condition.Last() != c)
        //        filter += " && ";
        //}
        //var targets = schedule.AsQueryable().Where(filter).ToList();

       

        var filterso = new Dictionary<string, object>();
        filterso.Add("Name", "Panel_R1");

        var findPanelTask = CreateEqualExpression(filterso);
        var panles = schedule.AsQueryable().Where(findPanelTask)
            .ToList();

        //  WHERE [Entity Name] = Plan Raise AND [Location Activity] = RSE AND [Location Type] = RSE
        var cond = new Condition(1);
        cond.Add(new Trigger { Name = "Location Type", Value = "RSE" });
        cond.Add(new Trigger { Name = en, Value = "Plan Raise" });
        cond.Add(new Trigger { Name = "Location Activity", Value = "RSE" });
        
        var rse = FindAll(schedule, cond);


        //var findkeyVlaues = FindKeyValuesExpression("Entity Name", "Plan Panel");
        //var onEntityName = panel_l1.Attributes.AsQueryable().Where(findkeyVlaues)
        //    .ToList();

        TestDictionaryAccess();
        TryDict(panel_l1);

        // Period Count 5
        for (int i = 0; i < 5; i++)
        {

        }
        // Wait for user
        Console.Read();
    }

    private static List<ScheduleTask> FindAll(List<ScheduleTask> schedule, Condition condition)
    {
        var filters = condition.ToDictionary(c => c.Name, c => c.Value);

        var qryResult = new List<ScheduleTask>();

        foreach (var scheduletask in schedule)
        {
            var attributes = scheduletask.Attributes;

            bool found = false;

            foreach (var f in filters)
            {
                if (attributes.TryGetValue(f.Key, out string attributeValue))
                {
                    if (attributeValue != f.Value)
                    {
                        found = false;

                        break;
                    }

                    found = true;
                }
                else
                {
                    found = false;

                    break;
                }
            }

            if (found)
                qryResult.Add(scheduletask);

        }

        return qryResult;
    }

    //https://code-maze.com/dynamic-queries-expression-trees-csharp/#:~:text=In%20C%23%2C%20dynamic%20queries%20refer,or%20can%20be%20dynamically%20changed.
    private static void TryDict(ScheduleTask panel_l1)
    {
        //var param = Expression.Parameter(typeof(ScheduleTask));
        //var left = MemberExpression.Property(param, "Bar[\"entryName\"]");
        //var property = Expression.Property(parameter, typeof(Program).GetProperty("x"));
        //var itemAtPosition0 = Expression.MakeIndex(property, typeof(List<string>).GetProperty("Item"),
        //                     new[] { Expression.Constant(0) });

        //        var expr =
        //    Expression.Lambda<Func<Program, string>>(
        //        Expression.MakeIndex(
        //        Expression.Property(
        //                    parameter,
        //                    typeof(Program).GetProperty("x")),
        //                typeof(List<string>).GetProperty("Item"),
        //                new[] { Expression.Constant(0) }),
        //        parameter);
        //        --And here's how to invoke the expression:

        //var instance = new ProgramZ { x = new List<string> { "a", "b" } };

        //  Console.WriteLine(expr.Compile().Invoke(instance));

        //var param = Expression.Parameter(typeof(ScheduleTask));
        //var bar = MemberExpression.Property(param, "Attributes");

        //Type dictionaryType = typeof(ScheduleTask).GetProperty("Attributes").PropertyType;
        //PropertyInfo indexerProp = dictionaryType.GetProperty("Item");

        //var dictKeyConstant = Expression.Constant("entryName");
        //var dictAccess = Expression.MakeIndex(bar, indexerProp, new[] { dictKeyConstant });

        //var propertyType = indexerProp.PropertyType;
        ////var d = Convert.ChangeType("newValue", propertyType);
        //var right = Expression.Constant(propertyType);
        //var expression = Expression.MakeBinary(ExpressionType.Equal, dictAccess, right);

        //var param = Expression.Parameter(typeof(Person), "p");
        //var member = Expression.Property(param, propertyName);
        //var constant = Expression.Constant(value);
        //var body = Expression.Equal(member, constant);
        //return Expression.Lambda<Func<Person, bool>>(body, param);

        //ParameterExpression attr = Expression.Parameter(typeof(Dictionary<string, string>), "attr");
        //ParameterExpression key = Expression.Parameter(typeof(string), "key");
        //ParameterExpression result = Expression.Parameter(typeof(string), "result");
        //var member = Expression.Property(attr, "Item", key);
        //var constant = Expression.Constant("Entity Name");

        //var param = Expression.Parameter(typeof(Foo));
        //var bar = MemberExpression.Property(param, "Bar");

        //Type dictionaryType = typeof(Foo).GetProperty("Bar").PropertyType;
        //PropertyInfo indexerProp = dictionaryType.GetProperty("Item");
        //var dictKeyConstant = Expression.Constant("entryName");
        //var dictAccess = Expression.MakeIndex(bar, indexerProp, new[] { dictKeyConstant });

        //var propertyType = indexerProp.PropertyType;
        //var right = Expression.Constant(Convert.ChangeType("newValue", propertyType));
        //var expression = Expression.MakeBinary(ExpressionType.Equal, dictAccess, right);

        ParameterExpression dictExpr = Expression.Parameter(typeof(Dictionary<string, string>));
        ParameterExpression keyExpr = Expression.Parameter(typeof(string));
        ParameterExpression valueExpr = Expression.Parameter(typeof(string));

        // Simple and direct. Should normally be enough
        PropertyInfo indexer = dictExpr.Type.GetProperty("Item");

        // Alternative, note that we could even look for the type of parameters, if there are indexer overloads.
        //PropertyInfo indexer = (from p in dictExpr.Type.GetDefaultMembers().OfType<PropertyInfo>()
        //                            // This check is probably useless. You can't overload on return value in C#.
        //                        where p.PropertyType == typeof(int)
        //                        let q = p.GetIndexParameters()
        //                        // Here we can search for the exact overload. Length is the number of "parameters" of the indexer, and then we can check for their type.
        //                        where q.Length == 1 && q[0].ParameterType == typeof(string)
        //                        select p).Single();

        IndexExpression indexExpr = Expression.Property(dictExpr, indexer, keyExpr);

        BinaryExpression assign = Expression.Assign(indexExpr, valueExpr);

        var lambdaSetter = Expression.Lambda<Action<Dictionary<string, string>, string, string>>(assign, dictExpr, keyExpr, valueExpr);
        var lambdaGetter = Expression.Lambda<Func<Dictionary<string, string>, string, string>>(indexExpr, dictExpr, keyExpr);

        var setter = lambdaSetter.Compile();
        var getter = lambdaGetter.Compile();

        var dict = new Dictionary<string, string>();
        setter(dict, "MyKey1", "MyVal1");
        var value = getter(dict, "MyKey1");
        dict.Add("MyKey2", "MyVal2");
        dict.Add("MyKey3", "MyVal3");
        dict.Add("MyKey4", "MyVal4");

        Func<Dictionary<string, string>,bool> allfilts =  filts =>
        {
            bool yes = false;

            foreach (var kvp in filts)
            {
                if (dict.ContainsKey(kvp.Key))
                {
                    yes = getter(dict, kvp.Key) == kvp.Value;

                    if (!yes)
                        return false;
                }
                else
                    return false;
            }

            return yes;
        };
        
        var f = new Dictionary<string, string>();
        f.Add("MyKey2", "MyVal2");
        f.Add("MyKey5", "MyVal5");

        var bb = allfilts(f);
        //value = getter(dict, "MyKey1");

        //var constant = Expression.Constant("MyKey");
        //var member = Expression.Assign(keyExpr, constant);
        //var val = Expression.Constant(2);
        //var body = Expression.Equal(member, val);
        //var list = Enumerable.Range(0, 5000).ToList();
        //var idx = list.GetType().GetProperty("Item");
        //int id = 2;
        //var index = Expression.MakeIndex(Expression.Constant(list), idx, new[] { Expression.Constant(2) });
        //var lb = Expression.Lambda<Func<List<int>, int, bool>>(index);
        //list.Where(lb)


        var list = Enumerable.Range(2, 5000).ToList();

        var param = Expression.Parameter(typeof(List<int>), "p");
        var k = Expression.Parameter(typeof(int));

        var itemAtPosition0 = Expression.MakeIndex(Expression.Constant(list), typeof(List<int>).GetProperty("Item"),
                     new[] { Expression.Constant(0) });
        
        var lb = Expression.Lambda<Func<int, int>>(itemAtPosition0, k).Compile();
        var h = lb(3);
       

        var dictKeyConstant = Expression.Constant("MyKey");
        var dictAccess = Expression.MakeIndex(Expression.Constant(dict), indexer, new[] { dictKeyConstant });

        var propertyType = indexer.PropertyType;
        ////var d = Convert.ChangeType("newValue", propertyType);
        //var right = Expression.Constant(propertyType);
        //var expression = Expression.MakeBinary(ExpressionType.Equal, dictAccess, right);

        //var propertyType = indexerProp.PropertyType;
        ////var d = Convert.ChangeType("newValue", propertyType);
        //var right = Expression.Constant(propertyType);
        //var expression = Expression.MakeBinary(ExpressionType.Equal, dictAccess, right);

        ///var itemAtPosition0 = Expression.MakeIndex(property, typeof(List<string>).GetProperty("Item"),
        ///                     new[] { Expression.Constant(0) });

        //var keyvalueExp = Expression.Lambda<Func<KeyValuePair<string, int>, bool>>(body, dictExpr);
        //var get1 = Expression.Lambda<Func<Dictionary<string, int>, string, int>>(indexExpr, dictExpr, keyExpr);

      
        // MyKey eq 2 and MyOtherKey eq 3
        //var kvp = dict.AsQueryable().Where(keyvalueExp).ToArray();
    }

    static void TestDictionaryAccess()
    {
        //ParameterExpression valueBag = Expression.Parameter(typeof(Dictionary<string, object>), "valueBag");
        //ParameterExpression key = Expression.Parameter(typeof(string), "key");
        //ParameterExpression result = Expression.Parameter(typeof(object), "result");
        //BlockExpression block = Expression.Block(
        //    new[] { result },               //make the result a variable in scope for the block
        //    Expression.Assign(result, key), //How do I assign the Dictionary item to the result ??????
        //    result                          //last value Expression becomes the return of the block
        //);

        ParameterExpression valueBag = Expression.Parameter(typeof(Dictionary<string, object>), "valueBag");

        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        ParameterExpression result = Expression.Parameter(typeof(object), "result");
        BlockExpression block = Expression.Block(new[] { result },   //make the result a variable in scope for the block           
          Expression.Assign(result, Expression.Property(valueBag, "Item", key)),
          result   //last value Expression becomes the return of the block 
        );

        // Lambda Expression taking a Dictionary and a String as parameters and returning an object
        Func<Dictionary<string, object>, string, object> myCompiledRule = (Func<Dictionary<string, object>, string, object>)Expression.Lambda(block, valueBag, key).Compile();

        //-------------- invoke the Lambda Expression ----------------
        Dictionary<string, object> testBag = new Dictionary<string, object>();
        testBag.Add("one", 42);  //Add one item to the Dictionary
        Console.WriteLine(myCompiledRule.DynamicInvoke(testBag, "one")); // I want this to print 42
    }

    public static Expression<Func<KeyValuePair<string, string>, bool>> FindKeyValuesExpression(string Key, string value)
    {
        var param = Expression.Parameter(typeof(Dictionary<string, string>), "p");

        //Expression? body = null;

        //foreach (var pair in filters)
        //{
        //    var member = Expression.Property(param, pair.Key);
        //    var constant = Expression.Constant(pair.Value);
        //    var expression = Expression.Equal(member, constant);
        //    body = body == null ? expression : Expression.AndAlso(body, expression);
        //}

        ParameterExpression key = Expression.Parameter(typeof(string), "key");
        ParameterExpression result = Expression.Parameter(typeof(string), "result");

        //BlockExpression body = Expression.Block(new[] { result },   //make the result a variable in scope for the block           
        // Expression.Assign(result, Expression.Property(param, "Item", key)),
        // result   //last value Expression becomes the return of the block );

        //var body = Expression.Equals(key, result);

        //var b = Expression.Equals(Key, );

        var member = Expression.Property(param, "Item", key);
        var constant = Expression.Constant(Key);
        var expression = Expression.Equal(member, constant);

        //var e = Expression.Block(new[] { result }, , result);

        //var b = Expression.Block(new[] { result }, )

        return null;
        //return Expression.Lambda<Func<KeyValuePair<string, string>, bool>>(body, param);
    }

    //expression = CreateNestedExpression("Address.Country", "USA"); // Address.Country == "USA"
    //query = persons.Where(expression).ToQueryString();
    public static Expression<Func<ScheduleTask, bool>> CreateNestedExpression(string propertyName, object value)
    {
        var param = Expression.Parameter(typeof(ScheduleTask), "p");

        Expression member = param;

        foreach (var namePart in propertyName.Split('.'))
        {
            member = Expression.Property(member, namePart);
        }

        var constant = Expression.Constant(value);

        var body = Expression.Equal(member, constant);

        return Expression.Lambda<Func<ScheduleTask, bool>>(body, param);
    }

    
    // var filters = new Dictionary<string, object>();
    // filters.Add("FirstName", "Manoel");
    // filters.Add("LastName", "Nobrega");
    // expression = CreateEqualExpression(filters);
    // query = persons.Where(expression).ToQueryString();
   
    public static Expression<Func<ScheduleTask, bool>> CreateEqualExpression(IDictionary<string, object> filters)
    {
        var param = Expression.Parameter(typeof(ScheduleTask), "p");
        Expression? body = null;
        foreach (var pair in filters)
        {
            var member = Expression.Property(param, pair.Key);
            var constant = Expression.Constant(pair.Value);
            var expression = Expression.Equal(member, constant);
            body = body == null ? expression : Expression.AndAlso(body, expression);
        }
        return Expression.Lambda<Func<ScheduleTask, bool>>(body, param);
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
