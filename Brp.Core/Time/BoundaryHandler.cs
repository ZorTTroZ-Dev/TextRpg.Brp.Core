namespace Brp.Core.Time;

/// <summary>
/// One step of boundary processing, such as applying schedule changes or
/// firing overrides. It gets the state as left by the steps before it and the
/// boundary being processed (the clock already set to it).
/// </summary>
public delegate BoundaryResult<TState, TEvent> BoundaryHandler<TState, TEvent>(TState state, Clock boundary);
