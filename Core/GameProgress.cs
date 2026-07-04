using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheLostRobotStory.Core
{
    public static class GameProgress
    {
        public static int LevelsCompleted = 0;

        public static bool PlayerEvolved =>
            LevelsCompleted >= 2;
    }
}
