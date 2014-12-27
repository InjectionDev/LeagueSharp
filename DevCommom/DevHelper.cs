using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace DevCommom
{
    public static class DevHelper
    {

        public static List<Obj_AI_Hero> GetEnemyList()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValid)
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
                .ToList();
        }

        public static List<Obj_AI_Hero> GetAllyList()
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsAlly && x.IsValid)
                .OrderBy(x => ObjectManager.Player.ServerPosition.Distance(x.ServerPosition))
                .ToList();
        }

        public static Obj_AI_Hero GetNearestEnemy(this Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValid && x.NetworkId != unit.NetworkId)
                .OrderBy(x => unit.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        public static Obj_AI_Hero GetNearestAlly(this Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsAlly && x.IsValid && x.NetworkId != unit.NetworkId)
                .OrderBy(x => unit.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        public static Obj_AI_Hero GetNearestEnemyFromUnit(this Obj_AI_Base unit)
        {
            return ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsEnemy && x.IsValid)
                .OrderBy(x => unit.ServerPosition.Distance(x.ServerPosition))
                .FirstOrDefault();
        }

        public static float GetHealthPerc(this Obj_AI_Base unit)
        {
            return (unit.Health / unit.MaxHealth) * 100;
        }

        public static float GetManaPerc(this Obj_AI_Base unit)
        {
            return (unit.Mana / unit.MaxMana) * 100;
        }

        public static void SendMovePacket(this Obj_AI_Base v, Vector2 point)
        {
            Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(point.X, point.Y)).Send();
        }

        public static bool IsUnderEnemyTurret(this Obj_AI_Base unit)
        {
            IEnumerable<Obj_AI_Turret> query;

            if (unit.IsEnemy)
            {
                query = ObjectManager.Get<Obj_AI_Turret>()
                    .Where(x => x.IsAlly && x.IsValid && !x.IsDead && unit.ServerPosition.Distance(x.ServerPosition) < 950);
            }
            else
            {
                query = ObjectManager.Get<Obj_AI_Turret>()
                    .Where(x => x.IsEnemy && x.IsValid && !x.IsDead && unit.ServerPosition.Distance(x.ServerPosition) < 950);
            }

            return query.Any();
        }

        public static void Ping(Vector3 pos)
        {
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(pos.X, pos.Y, 0, 0, Packet.PingType.Normal)).Process();
        }

        public static float GetDistanceSqr(Obj_AI_Base source, Obj_AI_Base target)
        {
            return Vector2.DistanceSquared(source.ServerPosition.To2D(), target.ServerPosition.To2D());
        }

        public static bool IsFacing(this Obj_AI_Base source, Obj_AI_Base target)
        {
            if (!source.IsValid || !target.IsValid)
                return false;

            if (source.Path.Count() > 0 && source.Path[0].Distance(target.ServerPosition) < target.Distance(source))
                return true;
            else
                return false;
        }

        public static bool IsKillable(this Obj_AI_Hero source, Obj_AI_Base target, IEnumerable<SpellSlot> spellCombo)
        {
            return Damage.GetComboDamage(source, target, spellCombo) * 0.9 > target.Health;
        }

        public static int CountEnemyInPositionRange(Vector3 position, float range)
        {
            return GetEnemyList().Where(x => x.ServerPosition.Distance(position) <= range).Count();
        }

        private static readonly string[] AttackResets = { "dariusnoxiantacticsonh", "fioraflurry", "garenq", "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq", "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade", "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack", "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq", "xenzhaocombotarget", "yorickspectral" };
        private static readonly string[] NoAttacks = { "jarvanivcataclysmattack", "monkeykingdoubleattack", "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2", "zyragraspingplantattackfire", "zyragraspingplantattack2fire" };
        private static readonly string[] Attacks = { "caitlynheadshotmissile", "frostarrow", "garenslash2", "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute", "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "viktorqbuff", "xenzhaothrust2", "xenzhaothrust3" };

        public static bool IsAutoAttack(string spellName)
        {
            return (spellName.ToLower().Contains("attack") && !NoAttacks.Contains(spellName.ToLower())) || Attacks.Contains(spellName.ToLower());
        }

        public static bool IsMinion(AttackableUnit unit, bool includeWards = false)
        {
            if (unit is Obj_AI_Minion)
            {
                var minion = unit as Obj_AI_Minion;
                var name = minion.BaseSkinName.ToLower();
                return name.Contains("minion") || (includeWards && (name.Contains("ward") || name.Contains("trinket")));
            }
            else
                return false;
        }

        public static float GetRealDistance(GameObject unit, GameObject target)
        {
            return unit.Position.Distance(target.Position) + unit.BoundingRadius + target.BoundingRadius;
        }
    }
}
