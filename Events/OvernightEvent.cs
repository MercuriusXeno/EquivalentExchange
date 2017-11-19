using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EquivalentExchange.Events;

namespace EquivalentExchange
{
    class OvernightEvent
    {

        public static event EventHandler<EventArgsOvernightEvent> ShowOvernightEventMenu;

        internal static void InvokeShowNightEndMenus(EventArgsOvernightEvent args)
        {            
            if (ShowOvernightEventMenu == null)
                return;
            Util.InvokeEvent("EquivalentExchangeEvents.ShowOvernightEventMenu", ShowOvernightEventMenu.GetInvocationList(), null, args);
        }
    }
}
