﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin
{
    class FNNozzleController : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool isHybrid = false;
        [KSPField(isPersistant = false)]
        public bool isJet;
        
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
        public string fuelmode;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;

        private float maxISP;
        private float minISP;
        private float assThermalPower;
        private float powerRatio = 0.358f;
        private float engineMaxThrust;
        private bool isLFO = false;
        private float ispMultiplier = 1;
        private ConfigNode[] propellants;
        private VInfoBox fuel_gauge;
        protected float myScience = 0;

        [KSPField(isPersistant = true)]
        private int fuel_mode = 1;

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
        public void TogglePropellant() {
            
            fuel_mode++;
            if (fuel_mode >= propellants.Length) {
                fuel_mode = 0;
            }

            
            setupPropellants();

        }

        [KSPAction("Toggle Propellant")]
        public void TogglePropellantAction(KSPActionParam param) {
            TogglePropellant();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
            if (isupgraded || myScience < upgradeCost) { return; } // || !hasScience || myScience < upgradeCost) { return; }
            isupgraded = true;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                engineType = upgradedName;
                propellants = FNNozzleController.getPropellantsHybrid();
                isHybrid = true;
            }

        }

        public void setupPropellants() {
            ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
            ConfigNode chosenpropellant = propellants[fuel_mode];
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();
            
            //VStackIcon stackicon = new VStackIcon(part);
            bool currentpropellant_is_jet = false;
            bool currentpropellant_is_electric = false;
            //part.stackIcon.RemoveInfo(fuel_gauge);
            //part.stackIcon.ClearInfoBoxes();
            //part.stackIcon.DisplayInfo().
                        
            for (int i = 0; i < assprops.Length; ++i) {
                fuelmode = chosenpropellant.GetValue("guiName");
                ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
                isLFO = bool.Parse(chosenpropellant.GetValue("isLFO"));
                if(chosenpropellant.HasValue("isJet")) {
                    currentpropellant_is_jet = bool.Parse(chosenpropellant.GetValue("isJet"));
                }
                
                ModuleEngines.Propellant curprop = new ModuleEngines.Propellant();
                curprop.Load(assprops[i]);
                if (curprop.drawStackGauge) {
                    curprop.drawStackGauge = false;
                    if (currentpropellant_is_jet) {
                        print("Atmosphere");
                        fuel_gauge.SetMessage("Atmosphere");
                    }else {
                        fuel_gauge.SetMessage(curprop.name);
                    }
                    fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                    fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                    fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetValue(0f);
                }
                list_of_propellants.Add(curprop);
            }
            
            
            Part[] childParts = this.part.FindChildParts<Part>(true);
            PartModuleList childModules;
            for (int i = 0; i < childParts.Length; ++i) {
                childModules = childParts.ElementAt(i).Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
                    if (thisModule2 != null) {
                        FNReactor fnr = (FNReactor)thisModule;
                        FloatCurve newISP = new FloatCurve();
                        if (!currentpropellant_is_jet) {
                            maxISP = (float)Math.Sqrt((double)fnr.getReactorTemp()) * 17*ispMultiplier;
                            minISP = maxISP * 0.4f;
                            newISP.Add(0, maxISP, 0, 0);
                            newISP.Add(1, minISP, 0, 0);
                            curEngine.useVelocityCurve = false;
                            curEngine.useEngineResponseTime = false;
                        }else {
                            maxISP = 2500;
                            newISP.Add(0, 1200);
                            newISP.Add(0.3f, 2500);
                            newISP.Add(1, 800);
                            curEngine.useVelocityCurve = true;
                            curEngine.useEngineResponseTime = true;
                        }
                        //ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
                        curEngine.atmosphereCurve = newISP;
                        assThermalPower = fnr.getReactorThermalPower();
                        engineMaxThrust = 2000 * assThermalPower / maxISP / 9.81f;
                        if (isLFO) {
                            engineMaxThrust = engineMaxThrust*1.5f;
                        }
                        curEngine.maxThrust = engineMaxThrust;
                    }

                }

            }

            Part parent = this.part.parent;
            if (parent != null) {
                childModules = parent.Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
                    if (thisModule2 != null) {
                        FNReactor fnr = (FNReactor)thisModule;
                        FloatCurve newISP = new FloatCurve();
                        if (!currentpropellant_is_jet) {
                            maxISP = (float)Math.Sqrt((double)fnr.getReactorTemp()) * 17 * ispMultiplier;
                            minISP = maxISP * 0.4f;
                            newISP.Add(0, maxISP, 0, 0);
                            newISP.Add(1, minISP, 0, 0);
                            curEngine.useVelocityCurve = false;
                            curEngine.useEngineResponseTime = false;
                        }
                        else {
                            maxISP = 2500;
                            newISP.Add(0, 1200);
                            newISP.Add(0.3f, 2500);
                            newISP.Add(1, 800);
                            curEngine.useVelocityCurve = true;
                            curEngine.useEngineResponseTime = true;
                        }
                        //ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
                        curEngine.atmosphereCurve = newISP;
                        assThermalPower = fnr.getReactorThermalPower();
                        engineMaxThrust = 2000 * assThermalPower / maxISP / 9.81f;
                        if (isLFO) {
                            engineMaxThrust = engineMaxThrust * 1.5f;
                        }
                        curEngine.maxThrust = engineMaxThrust;

                    }
                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {
                curEngine.propellants.Clear();
                curEngine.propellants = list_of_propellants;
                curEngine.SetupPropellant();
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(curEngine.propellants[0].id, partresources);

            if (partresources.Count == 0 && fuel_mode != 0) {
                TogglePropellant();
            }
            /*else {
                if ((!isJet && currentpropellant_is_jet) || (isJet && !currentpropellant_is_jet)) {
                    TogglePropellant();
                }
            }*/
        }
        
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            Actions["TogglePropellantAction"].guiName = Events["TogglePropellant"].guiName = String.Format("Toggle Propellant");
            fuel_gauge = part.stackIcon.DisplayInfo();
            if (isHybrid) {
                propellants = getPropellantsHybrid();
            }else {
                propellants = getPropellants(isJet);
            }
            engineType = originalName;
            if (isupgraded) {
                engineType = upgradedName;
            }

            setupPropellants();
            
            
        }

        public override void OnUpdate() {
            Events["RetrofitEngine"].active = !isupgraded && isJet && myScience >= upgradeCost;
            Fields["upgradeCostStr"].guiActive = !isupgraded && isJet;
            Fields["engineType"].guiActive = isJet;
            
            ModuleEngines curEngineT = (ModuleEngines)this.part.Modules["ModuleEngines"];
            if (curEngineT.isOperational && !IsEnabled) {
                IsEnabled = true;
                part.force_activate();
                
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            float currentscience = 0;
            foreach (PartResource partresource in partresources) {
                currentscience += (float)partresource.amount;
            }
            myScience = currentscience;

            upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";

            float currentpropellant = 0;
            float maxpropellant = 0;

            partresources = new List<PartResource>();
            part.GetConnectedResources(curEngineT.propellants[0].id, partresources);

            foreach (PartResource partresource in partresources) {
                currentpropellant += (float) partresource.amount;
                maxpropellant += (float)partresource.maxAmount;
            }
            
            if (curEngineT.isOperational) {
                if (!fuel_gauge.infoBoxRef.expanded) {
                    fuel_gauge.infoBoxRef.Expand();
                }
                fuel_gauge.length = 2;
                if (maxpropellant > 0) {
                    fuel_gauge.SetValue(currentpropellant / maxpropellant);
                }else {
                    fuel_gauge.SetValue(0);
                }
            }else {
                if (!fuel_gauge.infoBoxRef.collapsed) {
                    fuel_gauge.infoBoxRef.Collapse();
                }
            }
        }

        public override void OnFixedUpdate() {
            ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
            //print(curEngine.currentThrottle.ToString() + "\n");

            if (curEngine.maxThrust <= 0 && curEngine.isEnabled && curEngine.currentThrottle > 0) {
                setupPropellants();
                if (curEngine.maxThrust <= 0) {
                    curEngine.maxThrust = 0;
                }
            }


            if (curEngine.currentThrottle > 0 && curEngine.isEnabled && assThermalPower > 0) {
                //float thermalReceived = part.RequestResource("ThermalPower", assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle);
                float thermalReceived = consumePower(assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle, FNResourceManager.FNRESOURCE_THERMALPOWER);
                if (thermalReceived >= assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle) {
                    float thermalThrustPerSecond = thermalReceived / TimeWarp.fixedDeltaTime / curEngine.currentThrottle * engineMaxThrust / assThermalPower;
                    curEngine.maxThrust = thermalThrustPerSecond;
                }
                else {
                    if (thermalReceived > 0) {
                        float thermalThrustPerSecond = thermalReceived / TimeWarp.fixedDeltaTime / curEngine.currentThrottle * engineMaxThrust / assThermalPower;
                        curEngine.maxThrust = thermalThrustPerSecond;
                    }
                    else {
                        curEngine.maxThrust = 0f;
                        curEngine.BurstFlameoutGroups();
                    }

                }
            }else {
                if (assThermalPower <= 0) {
                    curEngine.maxThrust = 0f;
                }
            }
            //curEngine.currentThrottle
            
        }

        public override string GetInfo() {
            return String.Format("Engine parameters determined by attached reactor.");
        }

        public static string getPropellantFilePath(bool isJet) {
            if (isJet) {
                return KSPUtil.ApplicationRootPath + "gamedata/warpplugin/IntakeEnginePropellants.cfg";
            }else {
                return KSPUtil.ApplicationRootPath + "gamedata/warpplugin/EnginePropellants.cfg";
            }
        }

        public static ConfigNode[] getPropellants(bool isJet) {
            ConfigNode config = ConfigNode.Load(getPropellantFilePath(isJet));
            ConfigNode[] propellantlist = config.GetNodes("PROPELLANTS");
            return propellantlist;
        }

        public static ConfigNode[] getPropellantsHybrid() {
            ConfigNode config = ConfigNode.Load(getPropellantFilePath(true));
            ConfigNode config2 = ConfigNode.Load(getPropellantFilePath(false));
            ConfigNode[] propellantlist = config.GetNodes("PROPELLANTS");
            ConfigNode[] propellantlist2 = config2.GetNodes("PROPELLANTS");
            propellantlist = propellantlist.Concat(propellantlist2).ToArray();
            return propellantlist;
        }
        
        
    }

    

    
}