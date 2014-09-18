using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using LeagueSharp;
using LeagueSharp.Common;
using Igniter;


/*
 * #### Script Base: https://github.com/fueledbyflux/LeagueSharp-Public/tree/master/SigmaCass ####
 * 
 * #### InjectionDev Mods ####
 * + AntiGapCloser with R when LowHealth
 * + LastHit E On Posioned Minions
 * + LastHit E On Non-Posioned Minions, if no enemy near
 * + Ignite KS
*/

namespace DevCassio
{
    class Program
    {
        public const string ChampionName = "Cassiopeia";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static List<Obj_AI_Base> minions;

        public static Ignite ignite = new Ignite();

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += onGameLoad;
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
                Combo();
            }
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                WaveClear();
            }
            if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
            {
                Freeze();
            }
        }

        public static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

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
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, true);
                return;
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, true);
                return;
            }

            if (ignite.CanKill(eTarget))
            {
                ignite.Cast(eTarget);
                Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }

        }

        public static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

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
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, true);
                return;
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, true);
                return;
            }
        }

        public static void WaveClear()
        {
            if (minions.Count > 1)
            {
                foreach (var minion in minions)
                {
                    var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                    if (E.IsReady() && minion.HasBuffOfType(BuffType.Poison) && E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(minion, true);
                    }

                    if (Q.IsReady() && minion.IsValidTarget(Q.Range))
                    {
                        Q.Cast(Q.GetCircularFarmLocation(minions).Position, true);
                    }

                    if (W.IsReady() && minion.IsValidTarget(Q.Range))
                    {
                        W.Cast(W.GetCircularFarmLocation(minions).Position, true);
                    }
                }
            }
        }

        public static void Freeze()
        {
            var nearTarget = DevCommom.GetNearestEnemy();

            if (minions.Count > 1)
            {
                foreach (var minion in minions)
                {
                    var predHP = HealthPrediction.GetHealthPrediction(minion, (int)E.Delay);

                    if (E.IsReady() && E.GetDamage(minion) > minion.Health && predHP > 0 && minion.IsValidTarget(E.Range))
                    {
                        if (minion.HasBuffOfType(BuffType.Poison))
                        {
                            E.CastOnUnit(minion, true);
                        }
                        else if (Player.ServerPosition.Distance(nearTarget.ServerPosition) > Q.Range + 50)
                        {
                            E.CastOnUnit(minion, true);
                        }
                    }
                }
            }
        }

        private static void onGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != ChampionName)
                return;

            float defaultHitBox = 75;


            Q = new Spell(SpellSlot.Q, 850 + defaultHitBox);
            W = new Spell(SpellSlot.W, 850 + defaultHitBox);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 800 + defaultHitBox);

            Q.SetSkillshot(0.6f, 140, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.5f, 210, 2500, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 210, float.MaxValue, false, SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            SetMainMenu();

            // Attach Events
            Game.OnGameUpdate += OnTick;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            ignite.CanKillstealEnemies += ignite_CanKillstealEnemies;

            Game.PrintChat(string.Format("<font color='#F7A100'>DevCassio Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
        }

        static void ignite_CanKillstealEnemies(object sender, IgniteEventArgs e)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                ignite.Cast(e.Enemies.FirstOrDefault());
                Game.PrintChat(string.Format("Ignite KS -> {0} ", e.Enemies.FirstOrDefault().SkinName));
            }
        }

        static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == Packet.C2S.Move.Header)
            {
                // TODO: Block R
            }
        }

        static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            Game.PrintChat(string.Format("OnPosibleToInterrupt -> {0} cast {1}", unit.SkinName, spell.SpellName));
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            Game.PrintChat(string.Format("OnEnemyGapcloser -> {0}", gapcloser.Sender.SkinName));

            var RAntiGapcloser = Config.Item("RAntiGapcloser").GetValue<bool>();
            var RAntiGapcloserMinHealth = Config.Item("RAntiGapcloserMinHealth").GetValue<Slider>().Value;

            if (RAntiGapcloser && DevCommom.GetHealthPerc() < RAntiGapcloserMinHealth && gapcloser.Sender.IsValidTarget(R.Range))
            {
                R.Cast(gapcloser.Sender.ServerPosition, true);
                Game.PrintChat(string.Format("OnEnemyGapcloser -> RAntiGapcloser on {0} !", gapcloser.Sender.SkinName));
            }
        }
        

        private static void SetMainMenu()
        {
            Config = new Menu("DevCassio", "DevCassio", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
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

            Config.AddSubMenu(new Menu("AntiGapcloser", "Anti Gapcloser"));
            Config.SubMenu("AntiGapcloser").AddItem(new MenuItem("RAntiGapcloser", "R AntiGapcloser").SetValue(true));
            Config.SubMenu("AntiGapcloser").AddItem(new MenuItem("RAntiGapcloserMinHealth", "R AntiGapcloser Min Health %").SetValue(new Slider(50, 0, 100)));

            Config.AddToMainMenu();
        }
    }
}