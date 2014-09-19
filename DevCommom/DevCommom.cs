using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public static class DevCommom
    {
        public static List<Obj_AI_Hero> GetEnemyList()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValidTarget())
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
                .ToList();
        }

        public static List<Obj_AI_Hero> GetAllyList()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsAlly && x.IsValidTarget())
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
                .ToList();
        }

        public static Obj_AI_Hero GetNearestEnemy()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValidTarget())
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        public static Obj_AI_Hero GetNearestEnemyFromUnit(Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValidTarget())
                .OrderBy(x => unit.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        public static float GetHealthPerc()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }

        public static float GetManaPerc()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }



    }
}
