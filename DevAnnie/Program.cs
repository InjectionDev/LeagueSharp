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
 * Flash + Combo Burst
 * Use E against AA and GapCloser
 * LastHit with Q in HarassMode to StackPassive (Save Q if enemy is near)
 * Use R + 4 Pyromania to Interrupt Dangerous Spells
 * Cast R if will Stun X Enemies
 * Cast R Burst Combo
 * Use Itens (DFG)
 * Skin Hack
 * 
*/

namespace DevAnnie
{
    class Program
    {
        public const string ChampionName = "annie";

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

        private static DateTime dtBurstComboStart = DateTime.MinValue;
        private static string msgFlashCombo = string.Empty;

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

                //Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie Loaded v{0}</font>", Assembly.GetExecutingAssembly().GetName().Version));

                assemblyUtil = new AssemblyUtil(Assembly.GetExecutingAssembly().GetName().Name);
                assemblyUtil.onGetVersionCompleted += AssemblyUtil_onGetVersionCompleted;
                assemblyUtil.GetLastVersionAsync();

                Game.PrintChat(string.Format("<font color='#FF0000'>DevAnnie: THIS ASSEMBLY IS NOT FINISHED YET!!!</font>"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void AssemblyUtil_onGetVersionCompleted(OnGetVersionCompletedArgs args)
        {
            if (args.LastAssemblyVersion == Assembly.GetExecutingAssembly().GetName().Version.ToString())
                Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie You have the lastest version.</font>"));
            else
                Game.PrintChat(string.Format("<font color='#fb762d'>DevAnnie NEW VERSION available! Tap F8 for Update! {0}</font>", args.LastAssemblyVersion));

            if (args.CurrentCommomVersion != args.LastCommomVersion)
                Game.PrintChat(string.Format("<font color='#fb762d'>DevCommom Library NEW VERSION available! Please Update while NOT INGAME! {0}</font>", args.LastCommomVersion));
        }

        private static void InitializeAttachEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
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
            var UseEAgainstAA = Config.Item("UseEAgainstAA").GetValue<bool>();

            if (UseEAgainstAA && E.IsReady() && sender.IsEnemy && sender is Obj_SpellMissile)
            {
                var missile = sender as Obj_SpellMissile;
                if (missile.SpellCaster is Obj_AI_Hero && missile.Target.IsMe)
                    CastE();
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
                CastE();
            }
        }

        private static void CastE()
        {
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            if (packetCast)
                Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(Player.NetworkId, SpellSlot.E)).Send();
            else
                E.Cast();
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
                        FlashCombo();
                        BurstCombo();
                        Combo();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        QHarassLastHit();
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

                skinManager.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void FlashCombo()
        {
            var UseFlashCombo = Config.Item("UseFlashCombo").GetValue<bool>();
            var FlashComboMinEnemies = Config.Item("FlashComboMinEnemies").GetValue<Slider>().Value;
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var FlashAntiSuicide = Config.Item("FlashAntiSuicide").GetValue<bool>();
            
            int qtPassiveStacks = GetPassiveStacks();
            if (UseFlashCombo && ((qtPassiveStacks == 3 && E.IsReady()) || qtPassiveStacks == 4) && summonerSpellManager.IsReadyFlash() && R.IsReady())
            {
                var allEnemies = DevHelper.GetEnemyList()
                    .Where(x => Player.Distance(x) > R.Range && Player.Distance(x) < R.Range + 500);

                var enemies = DevHelper.GetEnemyList()
                    .Where(x => Player.Distance(x) > R.Range && Player.Distance(x) < R.Range + 400 && GetBurstComboDamage(x) * 0.9 > x.Health)
                    .OrderBy(x => x.Health);

                bool isSuicide = FlashAntiSuicide ? allEnemies.Count() - enemies.Count() > 2 : false;

                if (enemies.Count() > 0 && !isSuicide)
                { 
                    var enemy = enemies.First();
                    if (DevHelper.CountEnemyInPositionRange(enemy.ServerPosition, 250) >= FlashComboMinEnemies)
                    {
                        var predict = R.GetPrediction(enemy, true).CastPosition;

                        if (qtPassiveStacks == 3)
                        {
                            if (packetCast)
                                Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(Player.NetworkId, SpellSlot.E)).Send();
                            else
                                E.Cast();
                        }

                        summonerSpellManager.CastFlash(predict);

                        if (itemManager.IsReadyDFG())
                            itemManager.CastDFG(enemy);

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

            if (itemManager.IsReadyDFG())
                totalComboDamage = totalComboDamage * 1.2;

            if (itemManager.IsReadyDFG())
                totalComboDamage += Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg);

            if (summonerSpellManager.IsReadyIgnite())
                totalComboDamage += Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite);

            return totalComboDamage;
        }

        public static void BurstCombo()
        {
            var eTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

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
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
            totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.W);

            ////if (itemManager.IsReadyDFG())
            ////    totalComboDamage = totalComboDamage * 1.2;

            ////if (itemManager.IsReadyDFG())
            ////    totalComboDamage += Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg);

            totalComboDamage += summonerSpellManager.IsReadyIgnite() ? Player.GetSummonerSpellDamage(eTarget, Damage.SummonerSpell.Ignite) : 0;

            double totalManaCost = 0;
            totalManaCost += Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
            totalManaCost += Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;

            if (mustDebug)
            {
                Game.PrintChat("BurstCombo Damage {0}/{1} {2}", Convert.ToInt32(totalComboDamage), Convert.ToInt32(eTarget.Health), eTarget.Health < totalComboDamage ? "BustKill" : "Harras");
                Game.PrintChat("BurstCombo Mana {0}/{1} {2}", Convert.ToInt32(totalManaCost), Convert.ToInt32(eTarget.Mana), Player.Mana >= totalManaCost ? "Mana OK" : "No Mana");
            }

            // R KS
            if (R.IsReady() && useR)
            {
                if (eTarget.Health < totalComboDamage * 0.9 && Player.Mana >= totalManaCost)
                {
                    if (totalComboDamage * 0.3 < eTarget.Health) // Anti OverKill
                    {
                        if (mustDebug)
                            Game.PrintChat("BurstCombo R -> " + eTarget.BaseSkinName);

                        ////if (itemManager.IsReadyDFG())
                        ////    itemManager.CastDFG(eTarget);

                        R.CastOnUnit(eTarget, packetCast);
                        dtBurstComboStart = DateTime.Now;
                    }
                    else
                    {
                        if (mustDebug)
                            Game.PrintChat("BurstCombo OverKill -> Save R");
                    }
                    dtBurstComboStart = DateTime.Now;
                }
            }

            // R if Hit X Enemies
            int qtPassiveStacks = GetPassiveStacks();
            if (R.IsReady() && useR && ((qtPassiveStacks == 3 && E.IsReady()) || qtPassiveStacks == 4))
            {
                if (DevHelper.CountEnemyInPositionRange(eTarget.ServerPosition, 250) >= UseRMinEnemies)
                {
                    if (qtPassiveStacks == 3)
                    {
                        if (packetCast)
                            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(Player.NetworkId, SpellSlot.E)).Send();
                        else
                            E.Cast();
                    }

                    ////if (itemManager.IsReadyDFG())
                    ////    itemManager.CastDFG(eTarget);

                    R.CastOnUnit(eTarget, packetCast);
                    dtBurstComboStart = DateTime.Now;
                }
            }

            // Ignite
            if (dtBurstComboStart.AddSeconds(4) > DateTime.Now && summonerSpellManager.IsReadyIgnite())
            {
                if (mustDebug)
                    Game.PrintChat("Ignite -> " + eTarget.BaseSkinName);
                summonerSpellManager.CastIgnite(eTarget); ;
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
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }


            if (summonerSpellManager.CanKillIgnite(eTarget))
            {
                if (summonerSpellManager.CastIgnite(eTarget))
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
                Q.CastOnUnit(eTarget, packetCast);
            }

            if (eTarget.IsValidTarget(W.Range) && W.IsReady() && useW)
            {
                W.CastIfHitchanceEquals(eTarget, eTarget.IsMoving ? HitChance.High : HitChance.Medium, packetCast);
            }

        }

        public static void QHarassLastHit()
        {
            var UseQHarassLastHit = Config.Item("UseQHarassLastHit").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();

            if (UseQHarassLastHit && Q.IsReady() && GetPassiveStacks() < 4)
            {
                var nearestEnemy = Player.GetNearestEnemy();
                if (Player.Distance(nearestEnemy) > Q.Range + 100)
                {
                    var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy).ToList();
                    var minionLastHit = allMinions.Where(x => HealthPrediction.LaneClearHealthPrediction(x, (int)Q.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.Q) * 0.9f).OrderBy(x => x.Health);

                    if (minionLastHit.Count() > 0)
                    {
                        var unit = minionLastHit.First();
                        Q.CastOnUnit(unit, packetCast);
                    }
                }
            }
        }

        public static void WaveClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var ManaLaneClear = Config.Item("ManaLaneClear").GetValue<Slider>().Value;
            var UseQHarassLastHit = Config.Item("UseQHarassLastHit").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy).ToList();
                var minionLastHit = allMinions.Where(x => HealthPrediction.LaneClearHealthPrediction(x, (int)Q.Delay * 1000) < Player.GetSpellDamage(x, SpellSlot.Q) * 0.9f).OrderBy(x => x.Health);

                if (minionLastHit.Count() > 0)
                {
                    var unit = minionLastHit.First();
                    Q.CastOnUnit(unit, packetCast);
                }
            }

            if (W.IsReady() && useW && Player.GetManaPerc() >= ManaLaneClear)
            {
                var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy).ToList();

                if (allMinionsW.Count > 0)
                {
                    var farm = W.GetCircularFarmLocation(allMinionsW, W.Width * 0.8f);
                    if (farm.MinionsHit >= 3)
                    {
                        W.Cast(farm.Position, packetCast);
                    }
                }
            }


        }

        public static int GetPassiveStacks()
        {
            var buffs = Player.Buffs.Where(buff => (buff.Name == "pyromania" || buff.Name == "pyromania_particle"));
            if (buffs.Count() > 0)
            {
                var buff = buffs.First();
                if (buff.Name == "pyromania_particle")
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

        private static void InitializeSpells()
        {
            summonerSpellManager = new SummonerSpellManager();
            itemManager = new ItemManager();

            Q = new Spell(SpellSlot.Q, 650);
            Q.SetTargetted(0.25f, 1400);

            W = new Spell(SpellSlot.W, 650);
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
            Config = new Menu("DevAnnie", "DevAnnie", true);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRMinEnemies", "Use R Stun X Enemies").SetValue(new Slider(2, 1, 5)));

            Config.AddSubMenu(new Menu("FlashCombo", "FlashCombo"));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("UseFlashCombo", "Use Flash Combo").SetValue(true));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("FlashComboMinEnemies", "FlashCombo Min Enemies Hit").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("FlashCombo").AddItem(new MenuItem("FlashAntiSuicide", "Use Flash Anti Suicide").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarassLastHit", "Use Q LastHit").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("ManaLaneClear", "W Min Mana LaneClear").SetValue(new Slider(30, 1, 100)));

            Config.AddSubMenu(new Menu("Extra", "Extra"));
            Config.SubMenu("Extra").AddItem(new MenuItem("PacketCast", "Use PacketCast").SetValue(true));
            Config.SubMenu("Extra").AddItem(new MenuItem("UseEAgainstAA", "Use E against AA").SetValue(true));
            Config.SubMenu("Extra").AddItem(new MenuItem("UseRInterrupt", "Use R + Pyromania to Interrupt").SetValue(true));

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

            Config.AddToMainMenu();
        }
    }
}
