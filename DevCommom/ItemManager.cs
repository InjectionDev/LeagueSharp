using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class ItemManager
    {
        public Items.Item DFG;

        public ItemManager()
        {
            DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);
        }

        public bool IsReadyDFG()
        {
            return DFG.IsReady();
        }

        public void CastDFG(Obj_AI_Hero target)
        {
            if (IsReadyDFG())
                DFG.Cast(target);
        }

    }
}
