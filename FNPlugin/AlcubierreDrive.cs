﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin
{
    class AlcubierreDrive : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        private Vector3d heading_act;
        private Vector3d old_orbit;
        //private float warpspeed = 30000000.0f;
        public const float warpspeed = 29979245.8f;
        const float initial_megajoules_required = 1000;
        private float Megajoules_required = 1000;
        private bool dowarpup = false;
        private bool dowarpdown = false;
        private int wcount = 0;
        private float[] warp_factors = {0.1f,0.25f,0.5f,0.75f,1.0f,2.0f,3.0f,4.0f,5.0f,7.5f,10.0f,15f,20.0f};
		[KSPField(isPersistant = true)]
        public int selected_factor = 0;
        protected float myScience;
        protected float mass_divisor = 10f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string warpdriveType;
        
        [KSPField(isPersistant = false, guiActive = true, guiName = "Light Speed Factor")]
        public string LightSpeedFactor;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string DriveStatus;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;

        [KSPField(isPersistant = true)]
        public bool isupgraded = false;

        [KSPField(isPersistant = true)]
        public string serialisedwarpvector;

        protected GameObject warp_effect;
        protected GameObject warp_effect2;
        protected Texture[] warp_textures;
        protected Texture[] warp_textures2;
        protected AudioSource warp_sound;
        protected float tex_count;
        const float warp_size = 50000;

        [KSPEvent(guiActive = true, guiName = "Activate Warp Drive", active = true)]
        public void ActivateWarpDrive() {
            if (IsEnabled) {
                return;
            }
            
            Vessel vess = this.part.vessel;
            float atmosphere_height = vess.mainBody.maxAtmosphereAltitude;
            if (vess.altitude <= atmosphere_height && vess.mainBody.flightGlobalsIndex != 0) {
                ScreenMessages.PostScreenMessage("Cannot activate warp drive within the atmosphere!");
                return;
            }
            
            var resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ExoticMatter").id, resources);
            float electrical_current_available = 0;
            for (int i = 0; i < resources.Count; ++i) {
                electrical_current_available += (float)resources.ElementAt(i).amount;
            }
            if (electrical_current_available < Megajoules_required * warp_factors[selected_factor]) {
                ScreenMessages.PostScreenMessage("Warp drive charging!");
                return;
            }
            part.RequestResource("ExoticMatter", Megajoules_required * warp_factors[selected_factor]);
            warp_sound.Play();
            warp_sound.loop = true;
            
            
            //Orbit planetOrbit = vessel.orbit.referenceBody.orbit;
            Vector3d heading = part.transform.up;
            double temp1 = heading.y;
            heading.y = heading.z;
            heading.z = temp1;
            
            Vector3d position = vessel.orbit.pos;
            heading = heading * warpspeed * warp_factors[selected_factor];
            heading_act = heading;
            serialisedwarpvector = ConfigNode.WriteVector(heading);
            
            vessel.GoOnRails();
            
            vessel.orbit.UpdateFromStateVectors(position, vessel.orbit.vel + heading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
            vessel.GoOffRails();
            IsEnabled = true;
            
            
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Warp Drive", active = false)]
        public void DeactivateWarpDrive() {
            if (dowarpup) {
                return;
            }


            float atmosphere_height = this.vessel.mainBody.maxAtmosphereAltitude;
            if (this.vessel.altitude <= atmosphere_height && vessel.mainBody.flightGlobalsIndex != 0) {
                ScreenMessages.PostScreenMessage("Cannot deactivate warp drive within the atmosphere!");
                return;
            }
            IsEnabled = false;
            warp_sound.Stop();
            
            Vector3d heading = heading_act;
            heading.x = -heading.x;
            heading.y = -heading.y;
            heading.z = -heading.z;
            vessel.GoOnRails();
            //PatchedConicSolver.
            //this.vessel.ChangeWorldVelocity(heading);
            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
            vessel.GoOffRails();

            
            //CheatOptions.UnbreakableJoints = false;
            //CheatOptions.NoCrashDamage = false;           
        }

        [KSPEvent(guiActive = true, guiName = "Warp Speed (+)", active = true)]
        public void ToggleWarpSpeed() {
            if (IsEnabled) { return; }

            selected_factor++;
            if (selected_factor >= warp_factors.Length) {
                selected_factor = 0;
            }
        }

		[KSPEvent(guiActive = true, guiName = "Warp Speed (-)", active = true)]
		public void ToggleWarpSpeedDown() {
			if (IsEnabled) { return; }

			selected_factor-=1;
			if (selected_factor < 0) {
				selected_factor = warp_factors.Length-1;
			}
		}

        [KSPAction("Activate Warp Drive")]
        public void ActivateWarpDriveAction(KSPActionParam param) {
            ActivateWarpDrive();
        }

        [KSPAction("Deactivate Warp Drive")]
        public void DeactivateWarpDriveAction(KSPActionParam param) {
            DeactivateWarpDrive();
        }

        [KSPAction("Warp Speed (+)")]
        public void ToggleWarpSpeedAction(KSPActionParam param) {
            ToggleWarpSpeed();
        }

		[KSPAction("Warp Speed (-)")]
		public void ToggleWarpSpeedDownAction(KSPActionParam param) {
			ToggleWarpSpeedDown();
		}

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitDrive() {
            if (isupgraded || myScience < upgradeCost) { return; }
            isupgraded = true;
            
            warpdriveType = upgradedName;
            mass_divisor = 40f;
            //recalculatePower();
            part.RequestResource("Science", upgradeCost);
            //IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) {
            Actions["ActivateWarpDriveAction"].guiName = Events["ActivateWarpDrive"].guiName = String.Format("Activate Warp Drive");
            Actions["DeactivateWarpDriveAction"].guiName = Events["DeactivateWarpDrive"].guiName = String.Format("Deactivate Warp Drive");
			Actions["ToggleWarpSpeedAction"].guiName = Events["ToggleWarpSpeed"].guiName = String.Format("Warp Speed (+)");
			Actions["ToggleWarpSpeedDownAction"].guiName = Events["ToggleWarpSpeedDown"].guiName = String.Format("Warp Speed (-)");
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
            if (serialisedwarpvector != null) {
                heading_act = ConfigNode.ParseVector3D(serialisedwarpvector);
            }

            
            warp_effect2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warp_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warp_effect.collider.enabled = false;
            warp_effect2.collider.enabled = false;
            Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
            Vector3 end_beam_pos = ship_pos + transform.up * warp_size;
            Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;
            warp_effect.transform.localScale = new Vector3(6.3f, mid_pos.magnitude, 6.3f);
            warp_effect.transform.position = new Vector3(mid_pos.x, ship_pos.y+mid_pos.y, mid_pos.z);
            warp_effect.transform.rotation = part.transform.rotation;
            warp_effect2.transform.localScale = new Vector3(2.45f, mid_pos.magnitude, 2.45f);
            warp_effect2.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
            warp_effect2.transform.rotation = part.transform.rotation;
            //warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
            //warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));
            //KSP.IO.File.
            warp_effect.renderer.material.shader = Shader.Find("Unlit/Transparent");
            warp_effect2.renderer.material.shader = Shader.Find("Unlit/Transparent");

            warp_textures = new Texture[33];
            warp_textures[0] = GameDatabase.Instance.GetTexture("WarpPlugin/warp", false);
            warp_textures[1] = GameDatabase.Instance.GetTexture("WarpPlugin/warp2", false);
            warp_textures[2] = GameDatabase.Instance.GetTexture("WarpPlugin/warp3", false);
            warp_textures[3] = GameDatabase.Instance.GetTexture("WarpPlugin/warp4", false);
            warp_textures[4] = GameDatabase.Instance.GetTexture("WarpPlugin/warp5", false);
            warp_textures[5] = GameDatabase.Instance.GetTexture("WarpPlugin/warp6", false);
            warp_textures[6] = GameDatabase.Instance.GetTexture("WarpPlugin/warp7", false);
            warp_textures[7] = GameDatabase.Instance.GetTexture("WarpPlugin/warp8", false);
            warp_textures[8] = GameDatabase.Instance.GetTexture("WarpPlugin/warp9", false);
            warp_textures[9] = GameDatabase.Instance.GetTexture("WarpPlugin/warp10", false);
            warp_textures[10] = GameDatabase.Instance.GetTexture("WarpPlugin/warp11", false);
            warp_textures[11] = GameDatabase.Instance.GetTexture("WarpPlugin/warp10", false);
            warp_textures[12] = GameDatabase.Instance.GetTexture("WarpPlugin/warp11", false);
            warp_textures[13] = GameDatabase.Instance.GetTexture("WarpPlugin/warp12", false);
            warp_textures[14] = GameDatabase.Instance.GetTexture("WarpPlugin/warp13", false);
            warp_textures[15] = GameDatabase.Instance.GetTexture("WarpPlugin/warp14", false);
            warp_textures[16] = GameDatabase.Instance.GetTexture("WarpPlugin/warp15", false);
            warp_textures[17] = GameDatabase.Instance.GetTexture("WarpPlugin/warp16", false);
            warp_textures[18] = GameDatabase.Instance.GetTexture("WarpPlugin/warp15", false);
            warp_textures[19] = GameDatabase.Instance.GetTexture("WarpPlugin/warp14", false);
            warp_textures[20] = GameDatabase.Instance.GetTexture("WarpPlugin/warp13", false);
            warp_textures[21] = GameDatabase.Instance.GetTexture("WarpPlugin/warp12", false);
            warp_textures[22] = GameDatabase.Instance.GetTexture("WarpPlugin/warp11", false);
            warp_textures[23] = GameDatabase.Instance.GetTexture("WarpPlugin/warp10", false);
            warp_textures[24] = GameDatabase.Instance.GetTexture("WarpPlugin/warp9", false);
            warp_textures[25] = GameDatabase.Instance.GetTexture("WarpPlugin/warp8", false);
            warp_textures[26] = GameDatabase.Instance.GetTexture("WarpPlugin/warp7", false);
            warp_textures[27] = GameDatabase.Instance.GetTexture("WarpPlugin/warp6", false);
            warp_textures[28] = GameDatabase.Instance.GetTexture("WarpPlugin/warp5", false);
            warp_textures[29] = GameDatabase.Instance.GetTexture("WarpPlugin/warp4", false);
            warp_textures[30] = GameDatabase.Instance.GetTexture("WarpPlugin/warp3", false);
            warp_textures[31] = GameDatabase.Instance.GetTexture("WarpPlugin/warp2", false);
            warp_textures[32] = GameDatabase.Instance.GetTexture("WarpPlugin/warp", false);

            warp_textures2 = new Texture[33];
            warp_textures2[0] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr", false);
            warp_textures2[1] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr2", false);
            warp_textures2[2] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr3", false);
            warp_textures2[3] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr4", false);
            warp_textures2[4] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr5", false);
            warp_textures2[5] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr6", false);
            warp_textures2[6] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr7", false);
            warp_textures2[7] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr8", false);
            warp_textures2[8] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr9", false);
            warp_textures2[9] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr10", false);
            warp_textures2[10] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr11", false);
            warp_textures2[11] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr10", false);
            warp_textures2[12] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr11", false);
            warp_textures2[13] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr12", false);
            warp_textures2[14] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr13", false);
            warp_textures2[15] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr14", false);
            warp_textures2[16] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr15", false);
            warp_textures2[17] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr16", false);
            warp_textures2[18] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr15", false);
            warp_textures2[19] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr14", false);
            warp_textures2[20] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr13", false);
            warp_textures2[21] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr12", false);
            warp_textures2[22] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr11", false);
            warp_textures2[23] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr10", false);
            warp_textures2[24] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr9", false);
            warp_textures2[25] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr8", false);
            warp_textures2[26] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr7", false);
            warp_textures2[27] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr6", false);
            warp_textures2[28] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr5", false);
            warp_textures2[29] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr4", false);
            warp_textures2[30] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr3", false);
            warp_textures2[31] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr2", false);
            warp_textures2[32] = GameDatabase.Instance.GetTexture("WarpPlugin/warpr", false);
            
                        

            warp_effect.renderer.material.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
            warp_effect2.renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 0.1f);
            warp_effect.renderer.material.mainTexture = warp_textures[0];
            warp_effect.renderer.receiveShadows = false;
			//warp_effect.layer = LayerMask.NameToLayer ("Ignore Raycast");
			//warp_effect.collider.isTrigger = true;
			warp_effect2.renderer.material.mainTexture = warp_textures2[0];
            warp_effect2.renderer.receiveShadows = false;
            warp_effect2.renderer.material.mainTextureOffset = new Vector2(-0.2f, -0.2f);
			//warp_effect2.layer = LayerMask.NameToLayer ("Ignore Raycast");
			//warp_effect2.collider.isTrigger = true;
            warp_effect2.renderer.material.renderQueue = 1000;
            warp_effect.renderer.material.renderQueue = 1001;
            /*gameObject.AddComponent<Light>();
            gameObject.light.color = Color.cyan;
            gameObject.light.intensity = 1f;
            gameObject.light.range = 4000f;
            gameObject.light.type = LightType.Spot;
            gameObject.light.transform.position = end_beam_pos;
            gameObject.light.cullingMask = ~0;*/
            
            //light.

            warp_sound = gameObject.AddComponent<AudioSource>();
            warp_sound.clip = GameDatabase.Instance.GetAudioClip("WarpPlugin/Sounds/warp_sound");
            warp_sound.volume = 1;
            warp_sound.panLevel = 0;
            warp_sound.rolloffMode = AudioRolloffMode.Linear;
            warp_sound.Stop();

            if (IsEnabled) {
                warp_sound.Play();
                warp_sound.loop = true;
            }

            if (isupgraded) {
                warpdriveType = upgradedName;
                mass_divisor = 40f;
            }else {
                warpdriveType = originalName;
                mass_divisor = 10f;
            }
            
            //warp_effect.transform.localScale.y = 2.5f;
            //warp_effect.transform.localScale.z = 200f;

        }

        public override void OnUpdate() {
            Events["ActivateWarpDrive"].active = !IsEnabled;
            Events["DeactivateWarpDrive"].active = IsEnabled;
            Events["ToggleWarpSpeed"].active = !IsEnabled;
            Fields["upgradeCostStr"].guiActive = !isupgraded;
            Events["RetrofitDrive"].active = !isupgraded && myScience >= upgradeCost;

            LightSpeedFactor = warp_factors[selected_factor].ToString("0.00") + "c";

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            float currentscience = 0;
            foreach (PartResource partresource in partresources) {
                currentscience += (float)partresource.amount;
            }
            myScience = currentscience;

            upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";

            
        }

        public override void OnFixedUpdate() {
            //if (!IsEnabled) { return; }
            Megajoules_required = initial_megajoules_required * vessel.GetTotalMass() / mass_divisor;
            Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
            Vector3 end_beam_pos = ship_pos + part.transform.up * warp_size;
            Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f ;
            warp_effect.transform.rotation = part.transform.rotation;
            warp_effect.transform.localScale = new Vector3(6.2f, mid_pos.magnitude, 6.2f);
            warp_effect.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect.transform.rotation = part.transform.rotation;
            warp_effect2.transform.rotation = part.transform.rotation;
            warp_effect2.transform.localScale = new Vector3(2.4f, mid_pos.magnitude, 2.4f);
            warp_effect2.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect2.transform.rotation = part.transform.rotation;
            
            //if (tex_count < warp_textures.Length) {
            warp_effect.renderer.material.mainTexture = warp_textures[((int)tex_count)%warp_textures.Length];
            warp_effect2.renderer.material.mainTexture = warp_textures2[((int)tex_count+8) % warp_textures.Length];
            tex_count+=1f*warp_factors[selected_factor];
            //}else {
            //    tex_count = 0;
            //}

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel)) {
                FNResourceManager megamanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).getManagerForVessel(vessel);
                float available_power = megamanager.getStableResourceSupply();
                float power_returned = consumePower(available_power*TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                part.RequestResource("ExoticMatter", -power_returned / 1000.0f);
            }


            if (!IsEnabled) {
                //ChargeStatus = "";
                var resources = new List<PartResource>();
                part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ExoticMatter").id, resources);
                float electrical_current_available = 0;
                for (int i = 0; i < resources.Count; ++i) {
                    electrical_current_available += (float)resources.ElementAt(i).amount;
                }
                if (electrical_current_available < Megajoules_required * warp_factors[selected_factor]) {
                    float electrical_current_pct = 100.0f * electrical_current_available / (Megajoules_required * warp_factors[selected_factor]);
                    DriveStatus = String.Format("Charging: ") + electrical_current_pct.ToString("0.00") + String.Format("%");

                }
                else {
                    DriveStatus = "Ready.";
                }
                //light.intensity = 0;
                warp_effect2.renderer.enabled = false;
                warp_effect.renderer.enabled = false;
            }else {
                DriveStatus = "Active.";
                warp_effect2.renderer.enabled = true;
                warp_effect.renderer.enabled = true;
                
            }
  
            
            
        }

    }

    
}