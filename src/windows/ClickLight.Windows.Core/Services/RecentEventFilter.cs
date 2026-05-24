using ClickLight.Windows.Core.Models;

namespace ClickLight.Windows.Core.Services;

public sealed class RecentEventFilter
{
    private readonly TimeSpan _window;
    private readonly double _positionTolerance;
    private readonly List<ClickEvent> _recentEvents = [];
    private readonly object _gate = new();

    public RecentEventFilter(TimeSpan? window = null, double positionTolerance = 3)
    {
        _window = window ?? TimeSpan.FromMilliseconds(100);
        _positionTolerance = positionTolerance;
    }

    public bool ShouldAccept(ClickEvent clickEvent)
    {
        lock (_gate)
        {
            _recentEvents.RemoveAll(existing => clickEvent.Timestamp - existing.Timestamp >= _window.TotalSeconds);

            var isDuplicate = _recentEvents.Any(existing =>
                existing.Kind == clickEvent.Kind &&
                Math.Abs(existing.X - clickEvent.X) < _positionTolerance &&
                Math.Abs(existing.Y - clickEvent.Y) < _positionTolerance);

            if (!isDuplicate)
            {
                _recentEvents.Add(clickEvent);
            }

            return !isDuplicate;
        }
    }
}
