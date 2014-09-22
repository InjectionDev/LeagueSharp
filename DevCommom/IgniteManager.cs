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

        private SpellDataInst igniteSpell = null;

        public IgniteManager()
        {
            igniteSpell = GetSpell();
        }

        public bool Cast(Obj_AI_Hero enemy)
        {
            if (!enemy.IsValid || !enemy.IsVisible || !enemy.IsTargetable || enemy.IsDead)
            {
                return false;
            }

            if (IsReady())
            {
                ObjectManager.Player.SummonerSpellbook.CastSpell(igniteSpell.Slot, enemy);
                return true;
            }
            return false;
        }

        public bool IsReady()
        {
            SpellDataInst IgniteSpell = GetSpell();
            return (IgniteSpell != null && IgniteSpell.Slot != SpellSlot.Unknown && IgniteSpell.State == SpellState.Ready && ObjectManager.Player.CanCast);
        }

        public SpellDataInst GetSpell()
        {
            if (igniteSpell != null)
            {
                return igniteSpell;
            }
            SpellDataInst[] spells = ObjectManager.Player.SummonerSpellbook.Spells;
            return spells.FirstOrDefault(spell => spell.Name == "SummonerDot");
        }

        public bool CanKill(Obj_AI_Hero enemy)
        {
            return enemy.Health <= IgniteDamage();
        }

        private double IgniteDamage()
        {
            return ObjectManager.Player.Level * 20 + 50;
        }
    }
}
