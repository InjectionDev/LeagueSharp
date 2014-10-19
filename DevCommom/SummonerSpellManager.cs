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
        public static SpellSlot IgniteSlot;
        public static SpellSlot FlashSlot;
        public static SpellSlot BarrierSlot;
        public static SpellSlot HealSlot;
        public static SpellSlot ExhaustSlot;

        public SummonerSpellManager()
        {
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
            BarrierSlot = ObjectManager.Player.GetSpellSlot("SummonerBarrier");
            HealSlot = ObjectManager.Player.GetSpellSlot("SummonerHeal");
            ExhaustSlot = ObjectManager.Player.GetSpellSlot("SummonerExhaust");
        }

        // Cast

        public bool CastIgnite(Obj_AI_Hero target)
        {
            return ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
        }

        public bool CastFlash(Vector3 position)
        {
            return ObjectManager.Player.SummonerSpellbook.CastSpell(FlashSlot, position);
        }

        public bool CastBarrier()
        {
            return ObjectManager.Player.SummonerSpellbook.CastSpell(BarrierSlot);
        }

        public bool CastHeal()
        {
            return ObjectManager.Player.SummonerSpellbook.CastSpell(HealSlot);
        }

        public bool CastExhaust(Obj_AI_Hero target)
        {
            return ObjectManager.Player.SummonerSpellbook.CastSpell(ExhaustSlot, target);
        }

        // IsReady

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

        // 

        public bool CanKillIgnite(Obj_AI_Hero target)
        {
            return IsReadyIgnite() && target.Health < ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }
    }
}
