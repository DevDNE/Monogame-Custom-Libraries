using System;
using Microsoft.Xna.Framework;

namespace Debugging;
public class PerformanceMonitor : GameComponent
{
    private TimeSpan _oneSecond = TimeSpan.FromSeconds(1);
    private TimeSpan _timer = TimeSpan.Zero;
    private int _framesCounter = 0;
    private float _fps = 0;

    public PerformanceMonitor(Game game) : base(game)
    {
    }

    public override void Update(GameTime gameTime)
    {
        _framesCounter++;

        _timer += gameTime.ElapsedGameTime;
        if (_timer > _oneSecond)
        {
            _fps = _framesCounter;
            _framesCounter = 0;
            _timer -= _oneSecond;

            float memoryUsage = GC.GetTotalMemory(false) / 1024f / 1024f;
            //Logging.LogMessage("Hello World", LogLevel.Info);
            //Logging.LogPerformanceMetrics(_fps, memoryUsage);
        }

        base.Update(gameTime);
    }
}
