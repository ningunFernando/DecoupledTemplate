namespace DecoupledTemplate.Core.State
{
    public class PausedState : IGameState
    {
        public GameState Id => GameState.Paused;

        public void Enter() => Log.Trace("[PausedState] Enter");

        public void Tick() { }

        public void Exit() => Log.Trace("[PausedState] Exit");
    }
}
