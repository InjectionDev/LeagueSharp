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
 * ##### DevKarma Mods #####
 * 
 * + Logic with R+Q/R+W/R+E
 * + Priorize Heal with R+W when LowHealth (with Slider)
 * + Skin Hack
 * + Shield/Heal Allies
*/

namespace DevLulu
{
    public class Program
    {
        public const string ChampionName = "karma";

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

            try
            {
                InitializeSpells();

                InitializeSkinManager();

                InitializeMainMenu();

                InitializeAttachEvents();

                Game.PrintChat(string.Format("<font color='#F7A100'>DevKarma Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("ComboDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }
        }


        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var BarrierGapCloser = Config.Item("BarrierGapCloser").GetValue<bool>();
            var BarrierGapCloserMinHealth = Config.Item("BarrierGapCloserMinHealth").GetValue<Slider>().Value;
            var EGapCloser = Config.Item("EGapCloser").GetValue<bool>();

            if (BarrierGapCloser && gapcloser.Sender.IsValidTarget(Player.AttackRange) && Player.GetHealthPerc() < BarrierGapCloserMinHealth)
            {
                if (BarrierManager.Cast())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

            if (EGapCloser && E.IsReady())
            {
                if (R.IsReady())
                    R.Cast();

                E.Cast();
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {

        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            try
            { 
                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        Combo();
                        HelpAlly();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        HelpAlly();
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        public static void HelpAlly()
        {
            var HelpAlly = Config.Item("HelpAlly").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var UseEHelpAlly = Config.Item("UseEHelpAlly").GetValue<bool>();
            var AllyMinHealth = Config.Item("AllyMinHealth").GetValue<Slider>().Value;

            if (HelpAlly)
            {
                var AllyList = DevHelper.GetAllyList().Where(x => Player.Distance(x.ServerPosition) < E.Range && x.GetHealthPerc() < AllyMinHealth && DevHelper.CountEnemyInTargetRange(x, x.AttackRange) > 0).OrderBy(x => x.Health);
                if (AllyList.Count() > 0)
                {
                    var ally = AllyList.First();

                    if (R.IsReady())
                        R.Cast();

                    if (UseEHelpAlly && E.IsReady())
                        E.CastOnUnit(ally, packetCast);
                }
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

            var UseWComboHeal = Config.Item("UseWComboHeal").GetValue<bool>();
            var UseWHealMinHealth = Config.Item("UseWHealMinHealth").GetValue<Slider>().Value;

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                var pred = Q.GetPrediction(eTarget);
                if (pred.Hitchance >= (eTarget.IsMoving ? HitChance.High : HitChance.Medium))
                {
                    if (useR && R.IsReady())
                    {
                        if (!(UseWComboHeal && Player.GetHealthPerc() < UseWHealMinHealth))
                            R.Cast();
                    }
                    Q.Cast(pred.CastPosition, packetCast);
                }
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                if (useR && R.IsReady() && UseWComboHeal && Player.GetHealthPerc() < UseWHealMinHealth)
                    R.Cast();

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
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var UseWHarassHeal = Config.Item("UseWHarassHeal").GetValue<bool>();
            var UseWHealMinHealthHarass = Config.Item("UseWHealMinHealthHarass").GetValue<Slider>().Value;

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                var pred = Q.GetPrediction(eTarget);
                if (pred.Hitchance >= (eTarget.IsMoving ? HitChance.High : HitChance.Medium))
                {
                    if (useR && R.IsReady())
                    {
                        if (!(UseWHarassHeal && Player.GetHealthPerc() < UseWHealMinHealthHarass))
                            R.Cast();
                    }
                    Q.Cast(pred.CastPosition, packetCast);
                }
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                if (useR && R.IsReady() && UseWHarassHeal && Player.GetHealthPerc() < UseWHealMinHealthHarass)
                    R.Cast();

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
            SkinManager.Add("Classic Karma");
            SkinManager.Add("Sun Goddess Karma");
            SkinManager.Add("Sakura Karma");
            SkinManager.Add("Traditional Karma");
            SkinManager.Add("Order of the Lotus Karma");
        }

        private static void InitializeSpells()
        {
            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 1000);
            Q.SetSkillshot(0.25f, 60, 1700, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 650);
            W.SetTargetted(0.1f, float.MaxValue);

            E = new Spell(SpellSlot.E, 800);
            E.SetTargetted(0.1f, float.MaxValue);

            R = new Spell(SpellSlot.R);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Player.GetNearestEnemy().Distance(Player) < 1000)
                args.Process = false;
        }

        static void Drawing_OnDraw(EventArgs args)
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

        private static float GetComboDamage(Obj_AI_Hero enemy)
        {
            IEnumerable<SpellSlot> spellCombo = new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
            return (float)Damage.GetComboDamage(Player, enemy, spellCombo);
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevKarma", "DevKarma", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWComboHeal", "Priorize W to Heal").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWHealMinHealth", "W Heal Min Health").SetValue(new Slider(40, 1, 100)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseRHarass", "Use R").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarassHeal", "Priorize W to Heal").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHealMinHealthHarass", "W Heal Min Health").SetValue(new Slider(50, 1, 100)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(25, 1, 100)));

            Config.AddSubMenu(new Menu("Freeze", "Freeze"));
            Config.SubMenu("Freeze").AddItem(new MenuItem("UseQFreeze", "Use Q LastHit").SetValue(true));
            Config.SubMenu("Freeze").AddItem(new MenuItem("ManaFreeze", "Min Mana Q").SetValue(new Slider(25, 1, 100)));

            Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseRAlly", "Use R Ally").SetValue(true));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("UseRAllyMinHealth", "Ally MinHealth").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Help Ally", "HelpAlly"));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("HelpAlly", "Help Ally").SetValue(true));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("UseEHelpAlly", "Use E Ally").SetValue(true));
            Config.SubMenu("HelpAlly").AddItem(new MenuItem("AllyMinHealth", "Help Ally MinHealth").SetValue(new Slider(50, 1, 100)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloserMinHealth", "Barrier MinHealth").SetValue(new Slider(40, 0, 100)));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("EGapCloser", "E onGapCloser").SetValue(true));

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
