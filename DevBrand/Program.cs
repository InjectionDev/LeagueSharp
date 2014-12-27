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
 * ##### DevBrand Mods #####
 * 
 * + SBTW Assembly
 * + Ult Combo Logic
 * + Ult when X enemies in R Range
 * + Smart E usage on minions to harras (work in progress)
 * + Skin Hack
 * + Auto Spell Level UP
 * + Kill Steal Q/W/E/R
 * + Priorize Stun Harass/Combo 
*/

namespace DevBrand
{
    class Program
    {
        public const string ChampionName = "brand";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SkinManager skinManager;
        public static IgniteManager igniteManager;
        public static BarrierManager barrierManager;
        public static AssemblyUtil assemblyUtil;
        public static LevelUpManager levelUpManager;

        private static DateTime dtBurstComboStart = DateTime.MinValue;

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

                Game.PrintChat(string.Format("<font color='#fb762d'>DevBrand Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

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
                Game.PrintChat(string.Format("<font color='#fb762d'>DevBrand You have the latest version.</font>"));
            else
                Game.PrintChat(string.Format("<font color='#fb762d'>DevBrand NEW VERSION available! Tap F8 for Update! {0}</font>", args.LastAssemblyVersion));
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

            if (BarrierGapCloser && gapcloser.Sender.IsValidTarget(Player.AttackRange) && Player.GetHealthPerc() < BarrierGapCloserMinHealth)
            {
                if (barrierManager.Cast())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {

        }

        static bool HasPassiveBuff(Obj_AI_Base unit)
        {
            return unit.HasBuff("BrandAblaze");
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        BurstCombo();
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

                KillSteal();

                skinManager.Update();

                levelUpManager.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void BurstCombo()
        {
            var eTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var UseRMinEnemies = Config.Item("UseRMinEnemies").GetValue<Slider>().Value;
            

            double totalComboDamage = 0;
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.W);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);
            totalComboDamage += igniteManager.IsReady() ? Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite) : 0;

            double totalManaCost = 0;
            totalManaCost += Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
            totalManaCost += Player.Spellbook.GetSpell(SpellSlot.W).ManaCost;

            if (mustDebug)
            {
                Game.PrintChat("BurstCombo Damage {0}/{1} {2}", Convert.ToInt32(totalComboDamage), Convert.ToInt32(eTarget.Health), eTarget.Health < totalComboDamage ? "BustKill" : "Harras");
                Game.PrintChat("BurstCombo Mana {0}/{1} {2}", Convert.ToInt32(totalManaCost), Convert.ToInt32(eTarget.Mana), Player.Mana >= totalManaCost ? "Mana OK" : "No Mana");
            }

            // Passive UP +1 enemy Combo
            if (useR && R.IsReady())
            {
                var query = DevHelper.GetEnemyList()
                            .Where(x => x.IsValidTarget(R.Range) && HasPassiveBuff(x) && Player.GetSpellDamage(x, SpellSlot.R) > x.Health).OrderBy(x => x.Health);

                if (query.Any())
                    R.CastOnUnit(query.First(), packetCast);
            }


            // Combo Damage
            if (R.IsReady() && useR && eTarget.IsValidTarget(R.Range))
            {
                if (eTarget.Health < totalComboDamage * 0.9 && Player.Mana >= totalManaCost)
                {
                    if (totalComboDamage * 0.3 < eTarget.Health) // Anti OverKill
                    {
                        if (mustDebug)
                            Game.PrintChat("BurstCombo R");
                        R.CastOnUnit(eTarget, packetCast);
                    }
                    else
                    {
                        if (mustDebug)
                            Game.PrintChat("BurstCombo OverKill");
                    }
                    dtBurstComboStart = DateTime.Now;
                }
            }

            // R min enemies
            if (R.IsReady() && useR && eTarget.IsValidTarget(R.Range))
            {
                var enemiesInRange = DevHelper.GetEnemyList().Where(x => x.Distance(eTarget) < 400);
                if (enemiesInRange.Count() >= UseRMinEnemies)
                    R.CastOnUnit(eTarget, packetCast);
            }

            if (dtBurstComboStart.AddSeconds(5) > DateTime.Now && igniteManager.IsReady())
            {
                if (mustDebug)
                    Game.PrintChat("Ignite");
                igniteManager.Cast(eTarget);
            }
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
            var PriorizeStun = Config.Item("PriorizeStunCombo").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                if (PriorizeStun)
                {
                    if (HasPassiveBuff(eTarget))
                        Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                }
                else
                {
                    Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                }
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
            }

            if (igniteManager.CanKill(eTarget))
            {
                if (igniteManager.Cast(eTarget))
                    Game.PrintChat(string.Format("Ignite Combo KS -> {0} ", eTarget.SkinName));
            }
        }

        public static void Harass()
        {
            var eTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (eTarget == null)
                return;

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var PriorizeStun = Config.Item("PriorizeStunHarass").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                if (PriorizeStun)
                { 
                    if (HasPassiveBuff(eTarget))
                        Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                }
                else
                {
                    Q.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                }
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (eTarget.IsValidTarget(E.Range) && E.IsReady() && useE)
            {
                E.CastOnUnit(eTarget, packetCast);
            }
        }

        public static void WaveClear()
        {
            //var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaLaneClear").GetValue<Slider>().Value;


            if (W.IsReady() && useW && Player.GetManaPerc() >= ManaLaneClear)
            {
                var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, W.Range + W.Width, MinionTypes.All, MinionTeam.Enemy).ToList();

                if (allMinionsW.Any())
                {
                    var farm = W.GetCircularFarmLocation(allMinionsW, W.Width * 0.8f);
                    if (farm.MinionsHit >= 2)
                    {
                        W.Cast(farm.Position, packetCast);
                        return;
                    }
                }
            }

            if (E.IsReady() && useE && Player.GetManaPerc() >= ManaLaneClear)
            {
                var minionList = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health)
                    .Where(x => HasPassiveBuff(x));

                var jungleList = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
                    .Where(x => HasPassiveBuff(x));

                if (jungleList.Any())
                    E.CastOnUnit(jungleList.First(), packetCast);
                else if (minionList.Any())
                    E.CastOnUnit(jungleList.First(), packetCast);
            }

        }

        static void KillSteal()
        {
            var UseQKillSteal = Config.Item("UseQKillSteal").GetValue<bool>();
            var UseWKillSteal = Config.Item("UseWKillSteal").GetValue<bool>();
            var UseEKillSteal = Config.Item("UseEKillSteal").GetValue<bool>();
            var UseRKillSteal = Config.Item("UseRKillSteal").GetValue<bool>();
            var KillSteal = Config.Item("KillSteal").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (KillSteal)
            {

                if (UseQKillSteal && Q.IsReady())
                {
                    var ksQ = DevHelper.GetEnemyList().Where(x => x.IsValidTarget(Q.Range) && Q.GetDamage(x) > x.Health * 1.1).OrderBy(x => x.Health).ToList();
                    if (ksQ.Any())
                    {
                        var target = ksQ.First();
                        Q.CastIfHitchanceEquals(target, target.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                    }
                }

                if (UseWKillSteal && W.IsReady())
                {
                    var ksW = DevHelper.GetEnemyList().Where(x => x.IsValidTarget(W.Range) && W.GetDamage(x) > x.Health * 1.1).OrderBy(x => x.Health).ToList();
                    if (ksW.Any())
                    {
                        var target = ksW.First();
                        W.CastIfHitchanceEquals(target, target.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
                    }
                }

                if (UseEKillSteal && E.IsReady())
                {
                    var ksE = DevHelper.GetEnemyList().Where(x => x.IsValidTarget(E.Range) && E.GetDamage(x) > x.Health * 1.1).OrderBy(x => x.Health).ToList();
                    if (ksE.Any())
                    {
                        var target = ksE.First();
                        E.CastOnUnit(target, packetCast);
                    }
                }

                if (UseRKillSteal && R.IsReady())
                {
                    var ksR = DevHelper.GetEnemyList().Where(x => x.IsValidTarget(R.Range) && R.GetDamage(x) > x.Health * 1.1).OrderBy(x => x.Health).ToList();
                    if (ksR.Any())
                    {
                        var target = ksR.First();
                        R.CastOnUnit(target, packetCast);
                    }
                }

            }
        }


        private static void InitializeLevelUpManager()
        {
            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Start");

            var priority1 = new int[] { 2, 1, 3, 2, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3 };

            levelUpManager = new LevelUpManager();
            levelUpManager.Add("W > Q > E > W ", priority1);

            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Finish");
        }

        private static void InitializeSkinManager()
        {
            skinManager = new SkinManager();
            skinManager.Add("Classic Brand");
            skinManager.Add("Apocalyptic Brand");
            skinManager.Add("Vandal Brand");
            skinManager.Add("Cryocore Brand");
            skinManager.Add("Zombie Brand");
        }

        private static void InitializeSpells()
        {
            igniteManager = new IgniteManager();
            barrierManager = new BarrierManager();

            Q = new Spell(SpellSlot.Q, 1100);
            Q.SetSkillshot(0.25f, 60, 1600, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900);
            W.SetSkillshot(0.85f, 240, float.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 650);
            E.SetTargetted(0.2f, float.MaxValue);

            R = new Spell(SpellSlot.R, 750);
            R.SetTargetted(0.2f, float.MaxValue);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            //{
            //    var useQ = Config.Item("UseQCombo").GetValue<bool>();
            //    var useW = Config.Item("UseWCombo").GetValue<bool>();
            //    var useE = Config.Item("UseQCombo").GetValue<bool>();

            //    if (Player.GetNearestEnemy().IsValidTarget(W.Range) && ((useQ && Q.IsReady()) || (useW && W.IsReady() || useE && E.IsReady())))
            //        args.Process = false;
            //}
            //else
            //    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            //    {
            //        var useQ = Config.Item("UseQHarass").GetValue<bool>();
            //        var useW = Config.Item("UseWHarass").GetValue<bool>();
            //        var useE = Config.Item("UseEHarass").GetValue<bool>();

            //        if (Player.GetNearestEnemy().IsValidTarget(W.Range) && ((useQ && Q.IsReady()) || (useW && W.IsReady() || useE && E.IsReady())))
            //            args.Process = false;
            //    }
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
            Config = new Menu("DevBrand", "DevBrand", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRMinEnemies", "Use R if Hit X").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("PriorizeStunCombo", "Priorize Q Stun").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("PriorizeStunHarass", "Priorize Q Stun").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "Min Mana LaneClear").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));

            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal", "Kill Steal").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("UseQKillSteal", "Use Q").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("UseWKillSteal", "Use W").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("UseEKillSteal", "Use E").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("UseRKillSteal", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloser", "Barrier onGapCloser").SetValue(true));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("BarrierGapCloserMinHealth", "Barrier MinHealth").SetValue(new Slider(40, 0, 100)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ComboDamage", "Drawings on HPBar").SetValue(true));

            skinManager.AddToMenu(ref Config);

            levelUpManager.AddToMenu(ref Config);

            Config.AddToMainMenu();
        }
    }
}
