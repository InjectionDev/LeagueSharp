using DevCommom;
using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/*
 * ##### DevLulu Mods #####
 * + Support Mode and AP Carry Mode with separated logic
 * + E + Q Combo ! (Use enemy/ally/minion to hit target)
 * + E/R Interrupt Spells
 * + W Interrupt/Gapcloser
 * + Skin Hack

*/

namespace DevLulu
{
    public class Program
    {
        public const string ChampionName = "lulu";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SkinManager SkinManager;
        public static IgniteManager IgniteManager;
        public static BarrierManager BarrierManager;

        private static bool mustDebug = false;

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (!Player.ChampionName.ToLower().Contains(ChampionName))
                return;

            InitializeSpells();

            InitializeSkinManager();

            InitializeMainMenu();

            InitializeAttachEvents();

            Game.PrintChat(string.Format("<font color='#F7A100'>DevLulu Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

            Game.PrintChat(string.Format("<font color='#F7A100'>THIS ASSEMBLY IS NOT FINISHED YET!!!</font>"));
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            //Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            //Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            //Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            //if (Config.Item("ComboDamage").GetValue<bool>())
            //{
            //    Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            //    Utility.HpBarDamageIndicator.Enabled = true;
            //}
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var BarrierGapCloser = Config.Item("BarrierGapCloser").GetValue<bool>();
            var WGapCloser = Config.Item("WGapCloser").GetValue<bool>();

            if (BarrierGapCloser && gapcloser.Sender.IsValidTarget(Player.AttackRange))
            {
                if (BarrierManager.Cast())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

            if (WGapCloser && W.IsReady() && gapcloser.Sender.IsValidTarget(W.Range))
            {
                W.CastOnUnit(gapcloser.Sender, packetCast);
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var WInterruptSpell = Config.Item("WInterruptSpell").GetValue<bool>();

            if (WInterruptSpell && W.IsReady() && unit.IsValidTarget(W.Range))
            {
                W.CastOnUnit(unit, packetCast);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    //BurstCombo();
                    Combo();
                    ComboEQ();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //WaveClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    //Freeze();
                    break;
                default:
                    break;
            }

            SkinManager.Update();
        }

        public static void ComboEQ()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range * 2, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            if (!Q.IsReady() || !E.IsReady())
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var EnemyList = DevHelper.GetEnemyList().Where(x => Player.Distance(x) < E.Range);
            var queryEnemyList = EnemyList.Where(x => x.Distance(eTarget) < Q.Range);
            if (queryEnemyList.Count() > 0)
            {
                var unit = queryEnemyList.First();
                E.CastOnUnit(unit, packetCast);
                Q.Cast(eTarget.ServerPosition, packetCast);
                return;
            }

            var AllyList = DevHelper.GetAllyList().Where(x => Player.Distance(x) < E.Range);
            var queryAllyList = AllyList.Where(x => x.Distance(eTarget) < Q.Range);
            if (queryAllyList.Count() > 0)
            {
                var unit = queryEnemyList.First();
                E.CastOnUnit(unit, packetCast);
                Q.Cast(eTarget.ServerPosition, packetCast);
                return;
            }

            var MinionList = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.All, MinionOrderTypes.Health);
            var queryMinionList = MinionList.Where(x => x.Distance(eTarget) < Q.Range);
            if (queryMinionList.Count() > 0)
            {
                var unit = queryEnemyList.First();
                E.CastOnUnit(unit, packetCast);
                Q.Cast(eTarget.ServerPosition, packetCast);
                return;
            }
        }


        public static void Combo()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
            }

            if (IgniteManager.CanKill(eTarget))
            {
                if (IgniteManager.Cast(eTarget))
                    Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }
        }

        public static void Harass()
        {
            var eTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
            }
        }

        private static void InitializeSkinManager()
        {
            SkinManager = new SkinManager();
            SkinManager.Add("Classic Lulu");
            SkinManager.Add("Bittersweet Lulu");
            SkinManager.Add("Wicked Lulu");
            SkinManager.Add("Dragon Trainer Lulu");
            SkinManager.Add("Winter Wonder Lulu");
        }

        private static void InitializeSpells()
        {
            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 950);
            Q.SetSkillshot(0.2f, 60, 1450, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 650);
            W.SetTargetted(0, float.MaxValue);

            E = new Spell(SpellSlot.E, 650);
            E.SetTargetted(0, float.MaxValue);

            R = new Spell(SpellSlot.R, 900);
            R.SetTargetted(0, float.MaxValue);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevLulu", "DevLulu", true);

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

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(40, 1, 100)));

            Config.AddSubMenu(new Menu("Freeze", "Freeze"));
            Config.SubMenu("Freeze").AddItem(new MenuItem("UseQFreeze", "Use Q LastHit").SetValue(true));
            Config.SubMenu("Freeze").AddItem(new MenuItem("ManaFreeze", "Min Mana Q").SetValue(new Slider(40, 1, 100)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("WGapCloser", "W onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("WInterruptSpell", "W Interrupt Spell").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ComboDamage", "Drawings on HPBar").SetValue(true));

            SkinManager.AddToMenu(ref Config);

            Config.AddToMainMenu();
        }
    }
}
