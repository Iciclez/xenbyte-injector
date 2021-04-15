using System;
using System.Collections.Generic;
using Xenbyte_Injector.ViewModels;

namespace Xenbyte_Injector.Components
{
    class Configuration
    {
        public int AutomaticInjectionDataType { get; set; }
        public string AutomaticInjectionData { get; set; }
        public bool AutomaticRefreshProcess { get; set; }
        public int AutomaticRefreshProcessDelay { get; set; }
        public bool Freeze { get; set; }
        public IEnumerable<DllObject> DllList { get; set; }
    }
}
