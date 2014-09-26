using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class IgniteManager
    {
        public bool HasIgnite;
        public SpellDataInst IgniteSpell = null;

        public IgniteManager()
        {
            this.IgniteSpell = ObjectManager.Player.Spellbook.GetSpell(ObjectManager.Player.GetSpellSlot("SummonerDot"));

            if (this.IgniteSpell != null && this.IgniteSpell.Slot != SpellSlot.Unknown)
                this.HasIgnite = true;
        }

        public bool Cast(Obj_AI_Hero enemy)
        {
            if (!enemy.IsValid || !enemy.IsVisible || !enemy.IsTargetable || enemy.IsDead)
                return false;

            if (HasIgnite && IsReady() && enemy.IsValidTarget(600))
                return ObjectManager.Player.SummonerSpellbook.CastSpell(this.IgniteSpell.Slot, enemy);

            return false;
        }

        public bool IsReady()
        {
            return HasIgnite && this.IgniteSpell.State == SpellState.Ready && ObjectManager.Player.CanCast;
        }

        public bool CanKill(Obj_AI_Hero enemy)
        {
            return HasIgnite && IsReady() && enemy.Health < Damage.GetSummonerSpellDamage(ObjectManager.Player, enemy, Damage.SummonerSpell.Ignite);
        }


    }
}
