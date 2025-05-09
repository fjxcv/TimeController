using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.Services
{
    public interface INavigationService
    {
        void NavigateTo(string viewName);
    }

}
