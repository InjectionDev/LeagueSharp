using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

/*
 * Script Base: https://github.com/fueledbyflux/LeagueSharp-Public/tree/master/SigmaCass
*/

namespace TopCassio
{
    class Program
    {
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static List<Obj_AI_Base> minions;
        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
            Game.OnGameUpdate += OnTick;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.IsReady())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static void OnTick(EventArgs args)
        {
            minions = MinionManager.GetMinions(ObjectManager.Player.Position, E.Range, MinionTypes.All);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                harass();
            }
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                waveClear();
            }
            if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
            {
                freeze();
            }
        }

        public static void combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (eTarget.IsValidTarget(R.Range) && R.IsReady() && useR)
            {
                R.CastIfWillHit(eTarget, Config.Item("rCount").GetValue<Slider>().Value, true);
                return;
            }
            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (eTarget.HasBuffOfType(BuffType.Poison) || DamageLib.getDmg(eTarget, DamageLib.SpellType.E) > eTarget.Health)
                {
                    E.CastOnUnit(eTarget, true);
                    return;
                }
            }
            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, HitChance.High, true);
                return;
            }
            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, HitChance.High, true);
                return;
            }

        }
        public static void harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (eTarget.HasBuffOfType(BuffType.Poison) || DamageLib.getDmg(eTarget, DamageLib.SpellType.E) > eTarget.Health)
                {
                    E.CastOnUnit(eTarget, true);
                    return;
                }
            }
            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, HitChance.High, true);
                return;
            }
            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, HitChance.High, true);
                return;
            }
        }
        public static void waveClear()
        {
            if (minions.Count > 1)
            {
                foreach (var minion in minions)
                {
                    var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                    if (minion.HasBuffOfType(BuffType.Poison) && E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(minion, true);
                    }

                    if (minion.IsValidTarget(Q.Range))
                    {
                        Q.Cast(Q.GetCircularFarmLocation(minions).Position, true);
                    }

                    if (minion.IsValidTarget(Q.Range))
                    {
                        W.Cast(W.GetCircularFarmLocation(minions).Position, true);
                    }
                }
            }
        }
        public static void freeze()
        {
            if (minions.Count > 1)
            {
                foreach (var minion in minions)
                {
                    var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                    if (E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range) && minion.HasBuffOfType(BuffType.Poison))
                    {
                        E.CastOnUnit(minion, true);
                    }
                }
            }
        }

        private static void onGameLoad(EventArgs args)
        {
            Q = new Spell(SpellSlot.Q, 925);
            W = new Spell(SpellSlot.W, 925);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 875);

            Q.SetSkillshot(0.25f, 130, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.5f, 212, 2500, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 210, float.MaxValue, false, SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu("TopCassio", "TopCassio", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("rCount", "Min R Count").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            Config.AddToMainMenu();
        }
    }
}