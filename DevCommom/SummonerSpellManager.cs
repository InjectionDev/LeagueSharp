using LeagueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using SharpDX;


namespace DevCommom
{
    public class SummonerSpellManager
    {
        public SpellSlot IgniteSlot = SpellSlot.Unknown;
        public SpellSlot FlashSlot = SpellSlot.Unknown;
        public SpellSlot BarrierSlot = SpellSlot.Unknown;
        public SpellSlot HealSlot = SpellSlot.Unknown;
        public SpellSlot ExhaustSlot = SpellSlot.Unknown;


        public SummonerSpellManager()
        {
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
            BarrierSlot = ObjectManager.Player.GetSpellSlot("SummonerBarrier");
            HealSlot = ObjectManager.Player.GetSpellSlot("SummonerHeal");
            ExhaustSlot = ObjectManager.Player.GetSpellSlot("SummonerExhaust");
        }

        public bool CastIgnite(Obj_AI_Hero target)
        {
            if (IsReadyIgnite())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
            else
                return false;
        }

        public bool CastFlash(Vector3 position)
        {
            if (IsReadyFlash())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(FlashSlot, position);
            else
                return false;
        }

        public bool CastBarrier()
        {
            if (IsReadyBarrier())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(BarrierSlot);
            else
                return false;
        }

        public bool CastHeal()
        {
            if (IsReadyHeal())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(HealSlot);
            else
                return false;
        }

        public bool CastExhaust(Obj_AI_Hero target)
        {
            if (IsReadyExhaust())
                return ObjectManager.Player.SummonerSpellbook.CastSpell(ExhaustSlot, target);
            else
                return false;
        }

        public bool IsReadyIgnite()
        {
            return (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready);
        }

        public bool IsReadyFlash()
        {
            return (FlashSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(FlashSlot) == SpellState.Ready);
        }

        public bool IsReadyBarrier()
        {
            return (BarrierSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(BarrierSlot) == SpellState.Ready);
        }

        public bool IsReadyHeal()
        {
            return (HealSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(HealSlot) == SpellState.Ready);
        }

        public bool IsReadyExhaust()
        {
            return (ExhaustSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(ExhaustSlot) == SpellState.Ready);
        }

        public bool CanKillIgnite(Obj_AI_Hero target)
        {
            return IsReadyIgnite() && target.Health < ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        public double GetIgniteDamage(Obj_AI_Hero target)
        {
            if (IsReadyIgnite())
                return ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            else
                return 0;
        }
    }
}
