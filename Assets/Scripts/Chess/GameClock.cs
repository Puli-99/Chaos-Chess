public class GameClock
{
    public float Now { get; private set; }
    public void Advance(float deltaTime) => Now += deltaTime;
}
