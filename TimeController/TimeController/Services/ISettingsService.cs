using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.Services
{
    public interface ISettingsService
    {
        int LoadWeeklyTarget();
        void SaveWeeklyTarget(int value);
        bool LoadFollowSystemTheme();
        void SaveFollowSystemTheme(bool on);
    }
}
