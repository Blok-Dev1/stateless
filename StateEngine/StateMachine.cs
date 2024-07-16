namespace StateEngine;

public class StateMachine<TState, TEvent> where TState : class 
    where TEvent : class
{
    private readonly StateDefinition<TState, TEvent> _initialState;

    private StateDefinition<TState, TEvent> _currentState;

    private readonly ICollection<StateDefinition<TState, TEvent>> _states;

    public StateDefinition<TState, TEvent> Current
    {
        get 
        {
            Console.WriteLine($"State: {_currentState.State.GetType().Name}");

            return _currentState; 
        }
    }

    public StateMachine(TState state)
    {
        _states = new HashSet<StateDefinition<TState, TEvent>>();

        _currentState = _initialState = new StateDefinition<TState, TEvent>(state);

        _states.Add(_initialState);
    }

    public StateDefinition<TState, TEvent> State(TState state) 
    {
        StateDefinition<TState, TEvent> sd = null;

        var existing = _states.FirstOrDefault(s => s.State == state);

        if (existing != null)
            sd = existing;
        else
        {
            sd = new StateDefinition<TState, TEvent>(state);

            _states.Add(sd);
        }

        return sd;
    }

    public TState Event(TEvent trigger)
    {
        // can current state trasition to next allowed state
        if (_currentState.Transitions.TryGetValue(trigger, out TState state))
        {
            var current = _states.FirstOrDefault(s => s.State == state);

            if (current != null)
            {
                _currentState = current;

                _currentState.ExecuteEntry();

                return _currentState.State;
            }
        }
            
        throw new InvalidOperationException($"Transition from {_currentState.GetType().Name} on {trigger} was not defined or allowed.");
    }
    
}

public class StateDefinition<TState, TEvent> where TEvent:class
{
    private readonly TState _state;
    public TState State { get { return _state; } }
    public Dictionary<TEvent, TState> Transitions { get; set; }

    public Action<TState> EntryAction { get; set; }
    public StateDefinition(TState state) 
    { 
        _state = state;

        Transitions = new Dictionary<TEvent, TState>();
    }

    public StateDefinition<TState, TEvent> Allow(TEvent trigger, TState toState)
    {
        if(!Transitions.ContainsKey(trigger)) 
        {
            Transitions.Add(trigger, toState);
        }

        return this;
    }

    public void OnStateEntry(Action<TState> entryAction)
    {
        //entryAction.Invoke(this.State);
        EntryAction = entryAction;
    }

    public void ExecuteEntry()
    {
        if(EntryAction != null) 
        {
            EntryAction.Invoke(this.State);
        }
    }
}
