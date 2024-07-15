namespace StateEngine;

public class StateMachine<TState, TTrigger>
{
    private readonly TState _initialState;

    public StateMachine(TState state)
    {
        _initialState = state;
    }
    
}
