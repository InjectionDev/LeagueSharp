using DevCommom;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

/*
 * ##### DevFiora Mods #####
 * 
 * Full SBTW script
 * Smart Track of Second Q Cast
 * Use W to counter AA
 * Use W/E after attack
 * Cast R if will Hit/Kill # (slider menu)
 * Skin Hack
 * Auto Spell Level UP
 *  
*/

namespace DevFiora
{
    public class Program
    {
        public const string ChampionName = "Fiora";

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
        public static AssemblyUtil assemblyUtil;
        public static LevelUpManager levelUpManager;

        private static int lastQCastTime;

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

                InitializeLevelUpManager();

                InitializeMainMenu();

                InitializeAttachEvents();

                Game.PrintChat(string.Format("<font color='#fb762d'>DevFiora Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

                assemblyUtil = new AssemblyUtil(Assembly.GetExecutingAssembly().GetName().Name);
                assemblyUtil.onGetVersionCompleted += AssemblyUtil_onGetVersionCompleted;
                assemblyUtil.GetLastVersionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void AssemblyUtil_onGetVersionCompleted(OnGetVersionCompletedArgs args)
        {
            if (args.LastAssemblyVersion == Assembly.GetExecutingAssembly().GetName().Version.ToString())
                Game.PrintChat(string.Format("<font color='#fb762d'>DevFiora You have the latest version.</font>"));
            else
                Game.PrintChat(string.Format("<font color='#fb762d'>DevFiora NEW VERSION available! Tap F8 for Update! {0}</font>", args.LastAssemblyVersion));
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;

            Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("ComboDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var UseWAfterAttack = Config.Item("UseWAfterAttack").GetValue<bool>();
            var UseEAfterAttack = Config.Item("UseEAfterAttack").GetValue<bool>();

            var missile = (Obj_SpellMissile)sender;
            if (missile.SpellCaster is Obj_AI_Hero && missile.SpellCaster.IsValid && DevHelper.IsAutoAttack(missile.SData.Name))
            {
                if (UseWAfterAttack)
                    W.Cast(UsePackets()); 
   
                if (UseEAfterAttack)
                    E.Cast(UsePackets()); 
            }
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //if (sender.IsMe && Player.GetSpellSlot(args.SData.Name, false) == SpellSlot.Q)
            //{
            //    lastQCastTime = Environment.TickCount;
            //}

            var UseWAgainstAA = Config.Item("UseWAgainstAA").GetValue<bool>();

            if (UseWAgainstAA && sender.IsEnemy && !sender.IsMinion && args.Target.IsMe && W.IsReady() && DevHelper.IsAutoAttack(args.SData.Name))
                W.Cast(UsePackets());
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
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        WaveClear();
                        break;
                    case Orbwalking.OrbwalkingMode.LastHit:
                        //Freeze();
                        break;
                    default:
                        break;
                }

                SkinManager.Update();

                levelUpManager.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static bool HasSecondQBuff()
        {
            return Player.HasBuff("FioraQCD");
        }

        private static bool UsePackets()
        {
            return Config.Item("PacketCast").GetValue<bool>();
        }

        private static void CastFirstQ(Obj_AI_Hero eTarget)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (HasSecondQBuff() || Environment.TickCount - lastQCastTime < 3800)
                return;

            if (Orbwalking.InAutoAttackRange(eTarget))
                return;

            lastQCastTime = Environment.TickCount;
            Q.CastOnUnit(eTarget, UsePackets());
        }

        private static void CastSecondQ(Obj_AI_Hero eTarget)
        {
            if (!HasSecondQBuff())
                return;

            if (Environment.TickCount - lastQCastTime > 3600)
            {
                Q.CastOnUnit(eTarget, UsePackets());
            }

            if (Player.GetSpellDamage(eTarget, SpellSlot.Q) * 0.95 > eTarget.Health)
            {
                Q.CastOnUnit(eTarget, UsePackets());
            }

            if (Player.Distance(eTarget) > Orbwalking.GetRealAutoAttackRange(eTarget) * 1.2 || Player.Distance(eTarget) > 550)
            {
                Q.CastOnUnit(eTarget, UsePackets());
            }
        }

        private static void CastRMinHit()
        {
            var minEnemyHitCount = Config.Item("UseRMinHit").GetValue<Slider>().Value;
            var minEnemyKillCount = Config.Item("UseRMinKill").GetValue<Slider>().Value;

            if (minEnemyHitCount == 0)
                return;

            var enemies = DevHelper.GetEnemyList().Where(hero => hero.IsValidTarget(R.Range) && CountEnemyInPositionRange(hero.ServerPosition, 300) >= minEnemyHitCount).OrderBy(hero => hero.Health);
            if (enemies.Any())
            {
                var enemy = enemies.First();
                var enemyKillable = DevHelper.GetEnemyList().Where(x => x.ServerPosition.Distance(enemy.ServerPosition) <= 300 && R.GetDamage(x) * 0.9 > x.Health);
                if (enemyKillable.Count() >= minEnemyKillCount)
                    R.CastOnUnit(enemy, UsePackets());
            }
        }

        public static int CountEnemyInPositionRange(Vector3 position, float range)
        {
            return DevHelper.GetEnemyList().Count(x => x.ServerPosition.Distance(position) <= range);
        }

        public static void Combo()
        {
            var eTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

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
                CastFirstQ(eTarget);
                CastSecondQ(eTarget);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                if (Orbwalking.InAutoAttackRange(eTarget))
                    W.Cast(UsePackets());
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (Orbwalking.InAutoAttackRange(eTarget))
                    E.Cast(UsePackets());
            }

            if (useR && R.IsReady())
                CastRMinHit();

            if (IgniteManager.CanKill(eTarget))
            {
                if (IgniteManager.Cast(eTarget))
                    Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }
        }

        public static void Harass()
        {
            var eTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (eTarget == null)
                return;

            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var ManaHarass = Config.Item("ManaHarass").GetValue<Slider>().Value;

            if (Player.GetManaPerc() < ManaHarass)
                return;

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                if (Orbwalking.InAutoAttackRange(eTarget))
                    W.Cast(UsePackets());
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                if (Orbwalking.InAutoAttackRange(eTarget))
                    E.Cast(UsePackets());
            }
        }

        private static void WaveClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaLaneClear").GetValue<Slider>().Value;

            if (Player.GetManaPerc() < ManaLaneClear)
                return;

            var minionList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            var jungleList = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (jungleList.Any())
            {
                var jungle = jungleList.First();
                if (useQ && Q.IsReady())
                    Q.CastOnUnit(jungle, UsePackets());
                if (useE && E.IsReady())
                    E.CastOnUnit(jungle, UsePackets());
            }

            if (minionList.Any())
            {
                var minion = minionList.First();
                if (useQ && Q.IsReady())
                    Q.CastOnUnit(minion, UsePackets());
                if (useE && E.IsReady())
                    E.CastOnUnit(minion, UsePackets());
            }
        }

        private static void InitializeSkinManager()
        {
            SkinManager = new SkinManager();
            SkinManager.Add("Classic Fiora");
            SkinManager.Add("Royal Guard Fiora");
            SkinManager.Add("Nightraven Fiora");
            SkinManager.Add("Headmistress Fiora");
        }

        private static void InitializeLevelUpManager()
        {
            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Start");

            var priority1 = new int[] { 2, 1, 3, 1, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3 };

            levelUpManager = new LevelUpManager();
            levelUpManager.Add("W > Q > E > Q ", priority1);

            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Finish");
        }

        private static void InitializeSpells()
        {
            IgniteManager = new IgniteManager();
            BarrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 600);
            Q.SetTargetted(0.25f, float.MaxValue);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 400);
            R.SetTargetted(0.25f, float.MaxValue);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {

        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("QRange").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (Config.Item("RRange").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }

        private static float GetComboDamage(Obj_AI_Hero enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (Q.IsReady() || W.IsReady() || E.IsReady())
                damage += Player.GetAutoAttackDamage(enemy) * 2;
            else
                damage += Player.GetAutoAttackDamage(enemy);

            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevFiora", "DevFiora", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRMinHit", "R if Hit #").SetValue(new Slider(2, 0, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRMinKill", "R if Kill #").SetValue(new Slider(1, 0, 5)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("ManaHarass", "Min Mana Harass").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWAgainstAA", "Use W Against AA").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseWAfterAttack", "Use W After Attack").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEAfterAttack", "Use E After Attack").SetValue(true));

            //Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            //Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            //Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloserMinHealth", "Barrier MinHealth").SetValue(new Slider(40, 0, 100)));
            //Config.SubMenu("GapCloser").AddItem(new MenuItem("EGapCloser", "E onGapCloser").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ComboDamage", "Drawings on HPBar").SetValue(true));

            SkinManager.AddToMenu(ref Config);

            levelUpManager.AddToMenu(ref Config);

            Config.AddToMainMenu();
        }
    }
}
