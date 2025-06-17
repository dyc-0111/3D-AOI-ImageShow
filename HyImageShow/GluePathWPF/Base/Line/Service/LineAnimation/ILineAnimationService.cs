using System.Collections.Generic;
using System.Windows.Shapes;

namespace HyImageShow
{
    public interface ILineAnimationService
    {
        void Start(IEnumerable<LinePoint> lines, Dictionary<LinePoint, Line> lineDictionary);
        void Seek(double progress, IEnumerable<LinePoint> lines, Dictionary<LinePoint, Line> lineDictionary);

        void HoldStop();
        void ResumePause();
        bool IsPaused { get; }
        double Speed { get; set; }
        double CurrentProgress { get; set; }

    }
}
