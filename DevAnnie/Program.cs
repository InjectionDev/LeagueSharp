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
 * ##### DevAnnie Mods #####
 * 
 * SBTW Assembly
 * SoloQ and Support Mode with separated logic
 * Flash + Combo Burst Key
 * Use E Shield against AA/spells and GapCloser
 * LastHit with Q in HarassMode to StackPassive (Save Q if enemy is near)
 * Use R + 4 Pyromania to Interrupt Dangerous Spells
 * Cast R if will Stun X Enemies
 * Cast R Burst Combo
 * Use Itens (DFG)
 * Skin Hack
 * Auto Spell Level UP
 * 
*/

namespace DevAnnie
{
    class Program
    {
        public const string ChampionName = "Annie";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Obj_AI_Hero Player;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SkinManager skinManager;
        public static SummonerSpellManager summonerSpellManager;
        public static ItemManager itemManager;
        public static AssemblyUtil assemblyUtil;
        public static LevelUpManager levelUpManager;
        public static MessageManager messageManager;

        private static int dtBurstComboStart;
        private static string msgFlashCombo = string.Empty;

        private static bool hasTibber = false;
        private static int tibberNetworkId;

        private static bool mustDebug = false;

        static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (!Player.ChampionName.Equals(ChampionName, StringComparison.CurrentCultureIgnoreCase))
                return;

            try
            {
                InitializeSpells();

                InitializeSkinManager();

                InitializeLevelUpManager();

                InitializeMainMenu();

                InitializeAttachEvents();

                Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

                messageManager = new MessageManager();

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
                Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie :: You have the latest version.</font>"));
            else
                Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie :: NEW VERSION available! Tap F8 for Update! {0}</font>", args.LastAssemblyVersion));
        } 

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Config.Item("ComboDamage").ValueChanged += (object sender, OnValueChangeEventArgs e) => { Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>(); };
            if (Config.Item("ComboDamage").GetValue<bool>())
            {
                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }
        }


        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var UseEAgainstAA = Config.Item("UseEAgainstAA").GetValue<bool>();

            if (UseEAgainstAA && E.IsReady() && sender is Obj_SpellMissile)
            {
                var missile = sender as Obj_SpellMissile;
                if (missile.SpellCaster is Obj_AI_Hero && missile.SpellCaster.IsEnemy && missile.Target.IsMe)
                    E.Cast(packetCast);
            }

            if (sender.Name == "Tibbers" && sender.IsAlly)
            {
                hasTibber = true;
                tibberNetworkId = sender.NetworkId;
                if (mustDebug)
                {
                    Game.PrintChat("hasTibber true");
                    Game.PrintChat("hasTibber " + sender.GetType().Name);
                }
                    
            }
        }


        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Tibbers" && sender.IsAlly)
            {
                hasTibber = false;
                tibberNetworkId = -1;
                if (mustDebug)
                    Game.PrintChat("hasTibber false");
            }
        }


        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var BarrierGapCloser = Config.Item("BarrierGapCloser").GetValue<bool>();
            var BarrierGapCloserMinHealth = Config.Item("BarrierGapCloserMinHealth").GetValue<Slider>().Value;
            var UseEGapCloser = Config.Item("UseEGapCloser").GetValue<bool>();
            
            if (BarrierGapCloser && summonerSpellManager.IsReadyBarrier() && gapcloser.Sender.IsValidTarget(Player.AttackRange) && Player.GetHealthPerc() < BarrierGapCloserMinHealth)
            {
                if (summonerSpellManager.CastBarrier())
                    Game.PrintChat(string.Format("OnEnemyGapcloser -> BarrierGapCloser on {0} !", gapcloser.Sender.SkinName));
            }

            if (UseEGapCloser && E.IsReady())
            {
                E.Cast(packetCast);
            }
        }


        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var UseRInterrupt = Config.Item("UseRInterrupt").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (UseRInterrupt && R.IsReady() && GetPassiveStacks() >= 4 &&
                spell.DangerLevel == InterruptableDangerLevel.High && unit.IsEnemy && unit.IsValidTarget(R.Range))
            {
                R.Cast(unit.ServerPosition, packetCast);
            }
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
                        JungleClear();
                        break;
                    case Orbwalking.OrbwalkingMode.LastHit:
                        break;
                    default:
                        break;
                }

                FlashCombo();

                TibbersControl();

                EPassiveStack();
                QPassiveStack();

                skinManager.Update();

                levelUpManager.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }



        static bool IsSoloQMode
        {
            get { return Config.Item("ModeType").GetValue<StringList>().SelectedIndex == 0; }
        }

        static bool IsSupportMode
        {
            get { return Config.Item("ModeType").GetValue<StringList>().SelectedIndex == 1; }
        }

        public static void FlashCombo()
        {
            var UseFlashCombo = Config.Item("FlashComboKey").GetValue<KeyBind>().Active;
            var FlashComboMinEnemies = Config.Item("FlashComboMinEnemies").GetValue<Slider>().Value;
            var FlashAntiSuicide = Config.Item("FlashAntiSuicide").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (!UseFlashCombo)
                return;

           // messageManager.AddMessage(0, "FlashComboKey ON", System.Drawing.Color.Yellow);

            int qtPassiveStacks = GetPassiveStacks();

            if (((qtPassiveStacks == 3 && E.IsReady()) || qtPassiveStacks == 4) && summonerSpellManager.IsReadyFlash() && R.IsReady())
            {
                var allEnemies = DevHelper.GetEnemyList()
                    .Where(x => Player.Distance(x) > R.Range && Player.Distance(x) < R.Range + 500);

                var enemies = DevHelper.GetEnemyList()
                    .Where(x => Player.Distance(x) > R.Range && Player.Distance(x) < R.Range + 400 && GetBurstComboDamage(x) * 0.9 > x.Health)
                    .OrderBy(x => x.Health);

                bool isSuicide = FlashAntiSuicide ? allEnemies.Count() - enemies.Count() > 2 : false;

                if (enemies.Any() && !isSuicide)
                { 
                    var enemy = enemies.First();
                    if (DevHelper.CountEnemyInPositionRange(enemy.ServerPosition, 250) >= FlashComboMinEnemies)
                    {
                        var predict = R.GetPrediction(enemy, true).CastPosition;

                        if (qtPassiveStacks == 3)
                        {
                            E.Cast(packetCast);
                        }

                        summonerSpellManager.CastFlash(predict);


                        if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                            itemManager.UseItem(ItemName.DeathfireGrasp, enemy);

                        if (R.IsReady())
                            R.Cast(predict, packetCast);

                        if (W.IsReady())
                            W.Cast(predict, packetCast);

                        if (E.IsReady())
                            E.Cast();

                    }
                }
            }
        }

        public static double GetBurstComboDamage(Obj_AI_Hero eTarget)
        {
            double totalComboDamage = 0;
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.W);

            if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                totalComboDamage = totalComboDamage * 1.2;

            if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                totalComboDamage += Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg);

            if (summonerSpellManager.IsReadyIgnite())
                totalComboDamage += Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite);

            return totalComboDamage;
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

            if (R.IsReady())
                totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);

            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.W);

            if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                totalComboDamage = totalComboDamage * 1.2;

            if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                totalComboDamage += Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg);

            totalComboDamage += summonerSpellManager.IsReadyIgnite() ? Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite) : 0;

            double totalManaCost = 0;

            if (R.IsReady())
                totalManaCost += Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
            totalManaCost += Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;

            if (mustDebug)
            {
                Game.PrintChat("BurstCombo Damage {0}/{1} {2}", Convert.ToInt32(totalComboDamage), Convert.ToInt32(eTarget.Health), eTarget.Health < totalComboDamage ? "BustKill" : "Harras");
                Game.PrintChat("BurstCombo Mana {0}/{1} {2}", Convert.ToInt32(totalManaCost), Convert.ToInt32(eTarget.Mana), Player.Mana >= totalManaCost ? "Mana OK" : "No Mana");
            }

            // R Combo
            if (eTarget.Health < totalComboDamage && Player.Mana >= totalManaCost)
            {
                if (totalComboDamage * 0.3 < eTarget.Health) // Anti OverKill
                {
                    if (mustDebug)
                        Game.PrintChat("BurstCombo R -> " + eTarget.BaseSkinName);

                    if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                        itemManager.UseItem(ItemName.DeathfireGrasp, eTarget);

                    if (R.IsReady() && useR)
                    {
                        var pred = R.GetPrediction(eTarget, true); 
                        R.Cast(pred.CastPosition, packetCast);
                    }

                    dtBurstComboStart = Environment.TickCount;
                }
                dtBurstComboStart = Environment.TickCount;
            }


            // R if Hit X Enemies
            if (R.IsReady() && useR)
            {
                if (DevHelper.CountEnemyInPositionRange(eTarget.ServerPosition, 250) >= UseRMinEnemies)
                {
                    if (itemManager.IsItemReady(ItemName.DeathfireGrasp))
                        itemManager.UseItem(ItemName.DeathfireGrasp, eTarget);

                    var pred = R.GetPrediction(eTarget, true);
                    R.Cast(pred.CastPosition, packetCast);

                    dtBurstComboStart = Environment.TickCount;
                }
            }

            // Ignite
            if (Environment.TickCount - dtBurstComboStart > 4000 && summonerSpellManager.IsReadyIgnite())
            {
                if (mustDebug)
                    Game.PrintChat("Ignite -> " + eTarget.BaseSkinName);
                summonerSpellManager.CastIgnite(eTarget); ;
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
            var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();


            if (useQ && eTarget.IsValidTarget(Q.Range) && Q.IsReady())
            {
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (useW && eTarget.IsValidTarget(W.Range) && W.IsReady())
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

            if (UseIgnite && summonerSpellManager.CanKillIgnite(eTarget))
            {
                if (summonerSpellManager.CastIgnite(eTarget))
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
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            
            if (eTarget.IsValidTarget(Q.Range) && Q.IsReady() && useQ)
            {
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

        }


        public static void QPassiveStack()
        {
            var UseQStackPassive = Config.Item("UseQStackPassive").GetValue<bool>();
            var UseQStackPassiveNoEnemy = Config.Item("UseQStackPassiveNoEnemy").GetValue<bool>();
            var UseQStackPassiveEverywhere = Config.Item("UseQStackPassiveEverywhere").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (GetPassiveStacks() >= 4)
                UseQStackPassive = false;

            if (UseQStackPassive && Q.IsReady())
            {
                if (UseQStackPassiveNoEnemy)
                {
                    float extraDist = 1.3f;
                    if (UseQStackPassiveEverywhere)
                        extraDist = 1;

                    var nearestEnemy = Player.GetNearestEnemy();
                    if (Player.Distance(nearestEnemy) > Q.Range * extraDist)
                    {
                        var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy).ToList();
                        var minionLastHit = allMinions.Where(x => Q.GetDamage(x) * 0.8f > x.Health).OrderBy(x => x.Health);

                        if (minionLastHit.Any())
                        {
                            var unit = minionLastHit.First();
                            Q.CastOnUnit(unit, packetCast);
                        }
                    }
                }
            }
        }

        public static void EPassiveStack()
        {
            var UseEStackPassiveNoEnemy = Config.Item("UseEStackPassiveNoEnemy").GetValue<bool>();
            var UseEStackPassiveFountain = Config.Item("UseEStackPassiveFountain").GetValue<bool>();
            var UseEStackPassiveEverywhere = Config.Item("UseEStackPassiveEverywhere").GetValue<bool>();

            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (GetPassiveStacks() < 4)
            {
                if (Player.IsRecalling())
                    return;

                if (UseEStackPassiveEverywhere && E.IsReady())
                {
                    E.Cast(packetCast);
                    return;
                }

                if (UseEStackPassiveNoEnemy && E.IsReady())
                {
                    var nearestEnemy = Player.GetNearestEnemy();
                    if (Player.Distance(nearestEnemy) > Q.Range * 1.2f) // save for when its needed
                        E.Cast(packetCast);
                }

                if (UseEStackPassiveFountain && E.IsReady())
                { 
                    if (Player.InFountain())
                        E.Cast(packetCast);
                }
            }
        }

        public static void TibbersControl()
        {
            if (hasTibber && R.IsReady())
            {
                var packetCast = Config.Item("PacketCast").GetValue<bool>();
                var tibber = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(tibberNetworkId);

                if (tibber != null)
                {
                    var nearEnemy = DevCommom.DevHelper.GetEnemyList().Where(x => tibber.ServerPosition.Distance(x.ServerPosition) <= 200).OrderBy(x => x.Health);
                    if (nearEnemy.Any())
                    {
                        R.Cast(nearEnemy.First(), packetCast);
                        messageManager.AddMessage(0, "Tibbers Target: " + nearEnemy.First().SkinName, System.Drawing.Color.Yellow);

                        if (mustDebug)
                            Game.PrintChat("Tibbers Target " + nearEnemy.First().SkinName);
                    }
                    else
                    {
                        nearEnemy = DevCommom.DevHelper.GetEnemyList().Where(x => tibber.ServerPosition.Distance(x.ServerPosition) <= 1000).OrderBy(x => x.Health);
                        if (nearEnemy.Any())
                        {
                            R.Cast(nearEnemy.First(), packetCast);
                            messageManager.AddMessage(0, "Tibbers Target: " + nearEnemy.First().SkinName, System.Drawing.Color.Yellow);

                            if (mustDebug)
                                Game.PrintChat("Tibbers Target " + nearEnemy.First().SkinName);
                        }
                        else
                        {
                            messageManager.RemoveMessage(0);
                        }
                    }
                }
                else
                {
                    messageManager.RemoveMessage(0);
                    if (mustDebug)
                        Game.PrintChat("Tibbers null");
                }
            }
        }

        public static void WaveClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaLaneClear").GetValue<Slider>().Value;

            if (Q.IsReady() && useQ)
            {
                var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy).ToList();
                var minionLastHit = allMinions.Where(x => Q.GetDamage(x) > x.Health * 0.75f).OrderBy(x => x.Health);

                if (minionLastHit.Any())
                {
                    var unit = minionLastHit.First();
                    Q.CastOnUnit(unit, packetCast);
                }
            }

            if (W.IsReady() && useW && Player.GetManaPerc() >= ManaLaneClear)
            {
                var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy).ToList();

                if (allMinionsW.Any())
                {
                    var farm = W.GetCircularFarmLocation(allMinionsW, W.Width * 0.75f);
                    if (farm.MinionsHit >= 3)
                    {
                        W.Cast(farm.Position, packetCast);
                    }
                }
            }

        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count == 0)
                return;

            var UseQJungleClear = Config.Item("UseQJungleClear").GetValue<bool>();
            var UseWJungleClear = Config.Item("UseWJungleClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            var mob = mobs.First();

            if (UseQJungleClear && Q.IsReady() && mob.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(mob, packetCast);
            }

            if (UseWJungleClear && W.IsReady() && mob.IsValidTarget(W.Range))
            {
                W.Cast(mob.Position, packetCast);
            }
        }


        public static int GetPassiveStacks()
        {
            var buffs = Player.Buffs.Where(buff => (buff.Name.ToLower() == "pyromania" || buff.Name.ToLower() == "pyromania_particle"));
            if (buffs.Any())
            {
                var buff = buffs.First();
                if (buff.Name.ToLower() == "pyromania_particle")
                    return 4;
                else
                    return buff.Count;
            }
            return 0;
        }

        private static void InitializeSkinManager()
        {
            skinManager = new SkinManager();
            skinManager.Add("Classic Annie");
            skinManager.Add("Goth Annie");
            skinManager.Add("Red Riding Annie");
            skinManager.Add("Annie in Wonderland");
            skinManager.Add("Prom Queen Annie");
            skinManager.Add("Frostfire Annie");
            skinManager.Add("Franken Tibbers Annie");
            skinManager.Add("Reverse Annie");
            skinManager.Add("Panda Annie");
        }

        private static void InitializeLevelUpManager()
        {
            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Start");

            var priority1 = new int[] { 1, 2, 1, 3, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3 };

            levelUpManager = new LevelUpManager();
            levelUpManager.Add("Q > W > Q > E ", priority1);

            if (mustDebug)
                Game.PrintChat("InitializeLevelUpManager Finish");
        }

        private static void InitializeSpells()
        {
            summonerSpellManager = new SummonerSpellManager();
            itemManager = new ItemManager();

            Q = new Spell(SpellSlot.Q, 650);
            Q.SetTargetted(0.25f, 1400);

            W = new Spell(SpellSlot.W, 625);
            W.SetSkillshot(0.6f, (float)(50 * Math.PI / 180), float.MaxValue, false, SkillshotType.SkillshotCone);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 600);
            R.SetSkillshot(0.25f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Base)
            {
                var target = args.Target as Obj_AI_Base;
                if (target.IsMinion && IsSupportMode)
                {
                    var allyADC = Player.GetNearestAlly();
                    if (allyADC.Distance(args.Target) < allyADC.AttackRange * 1.2)
                        args.Process = false;
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var useQ = Config.Item("UseQCombo").GetValue<bool>();
                var useW = Config.Item("UseWCombo").GetValue<bool>();

                if ((useQ && Q.IsReady()) || (useW && W.IsReady()))
                    args.Process = false;
            }
            else
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    var useQ = Config.Item("UseQHarass").GetValue<bool>();
                    var useW = Config.Item("UseWHarass").GetValue<bool>();

                    if ((useQ && Q.IsReady()) || (useW && W.IsReady()))
                        args.Process = false;
                }
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

            messageManager.Draw();
        }


        private static float GetComboDamage(Obj_AI_Hero enemy)
        {
            IEnumerable<SpellSlot> spellCombo = new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
            return (float)Player.GetComboDamage(enemy, spellCombo);
        }

        private static void InitializeMainMenu()
        {
            Config = new Menu("DevAnnie", "DevAnnie", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Mode", "Mode"));
            Config.SubMenu("Mode").AddItem(new MenuItem("ModeType", "Mode Type").SetValue(new StringList(new[] { "SoloQ", "Support" })));

            Config.AddSubMenu(new Menu("Passive Stack", "Passive"));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseEStackPassive", "==> Stack with E").SetValue(true));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseEStackPassiveNoEnemy", "When No Enemy Near").SetValue(true));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseEStackPassiveFountain", "At Fountain").SetValue(true));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseEStackPassiveEverywhere", "Everywhere").SetValue(false));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseQStackPassive", "==> Stack with Q").SetValue(true));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseQStackPassiveNoEnemy", "When No Enemy Near").SetValue(true));
            Config.SubMenu("Passive").AddItem(new MenuItem("UseQStackPassiveEverywhere", "Everywhere").SetValue(false));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRMinEnemies", "Use R Stun X Enemies").SetValue(new Slider(2, 1, 5)));

            Config.AddSubMenu(new Menu("Flash Combo", "FlashCombo"));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("FlashComboKey", "FlashCombo!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("FlashComboMinEnemies", "FlashCombo Min Enemies Hit").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("FlashAntiSuicide", "Use Flash Anti Suicide").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            //Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarassLastHit", "Use Q LastHit").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "W Min Mana LaneClear").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("UseQJungleClear", "Use Q").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("UseWJungleClear", "Use W").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEAgainstAA", "Use E against AA").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRInterrupt", "Use R + Pyromania to Interrupt").SetValue(true));

            Config.AddSubMenu(new Menu("GapCloser", "GapCloser"));
            Config.SubMenu("GapCloser").AddItem(new MenuItem("UseEGapCloser", "Use E onGapCloser").SetValue(true));
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
