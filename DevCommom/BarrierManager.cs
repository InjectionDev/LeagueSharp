using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class BarrierManager
    {
        public bool HasBarrier;
        public SpellDataInst BarrierSpell = null;

        public BarrierManager()
        {
            this.BarrierSpell = ObjectManager.Player.Spellbook.GetSpell(ObjectManager.Player.GetSpellSlot("SummonerDot"));

            if (this.BarrierSpell != null && this.BarrierSpell.Slot != SpellSlot.Unknown)
                this.HasBarrier = true;
        }

        public bool Cast()
        {
            if (HasBarrier && IsReady())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(this.BarrierSpell.Slot);

            return false;
        }

        public bool IsReady()
        {
            return HasBarrier && this.BarrierSpell.State == SpellState.Ready && ObjectManager.Player.CanCast;
        }


    }
}
