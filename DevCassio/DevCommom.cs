using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace LeagueSharp
{
    public class DevCommom
    {
        public static Obj_AI_Hero GetNearestEnemy()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValidTarget())
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
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
