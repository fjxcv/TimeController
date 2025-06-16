
﻿
using System.Windows.Controls;

namespace TimeController.Services
{
    public interface INavigationService
    {
        void NavigateTo(Frame frame, string viewKey);
    }
}
