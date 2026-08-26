using MelonLoader;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

namespace TranslatedTextures
{
    public class Main : MelonMod
    {
        internal class TextureRule
        {
            public string? TextureFile { get; set; }
            public string? TargetName { get; set; }
            public string? ParentName { get; set; } = null;
            public string? SourceTextureName { get; set; } = null;
            public bool ExactName { get; set; } = false;
            public string? DamageTextureFile { get; set; } = null;
        }

        private class CannedGearDef
        {
            public string? TextureFile { get; set; }
            public string? SourceTextureName { get; set; }
            public string? ParentName { get; set; } = null;
            public string[] MeshNames { get; set; } = { "CannedFoodMesh", "CannedFoodMesh_Old" };
            public string LidSourceName { get; set; } = "GEAR_CanLid_Dif";
            public string LidTextureFile { get; set; } = "GEAR_CanLid_Dif.png";
        }

        private static readonly CannedGearDef[] CannedDefs = new[]
        {
            new CannedGearDef { TextureFile = "GEAR_TomatoSoupCan_Dif.png", SourceTextureName = "GEAR_TomatoSoupCan_Dif" },
            new CannedGearDef { TextureFile = "GEAR_DogFood_Dif.png", SourceTextureName = "GEAR_DogFood_Dif" },
            new CannedGearDef { TextureFile = "GEAR_FoodPorkAndBeans_Dif.png", SourceTextureName = "GEAR_FoodPorkAndBeans_Dif", MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" } },
            new CannedGearDef { TextureFile = "GEAR_FoodCannedPeaches_Dif.png", SourceTextureName = "GEAR_FoodCannedPeaches_Dif", MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" } },
            new CannedGearDef { TextureFile = "GEAR_CondensedMilk_Dif.png", SourceTextureName = "GEAR_CondensedMilk_Mat", ParentName = "GEAR_CondensedMilk", LidSourceName = "GEAR_CanLid_Mat" },
            new CannedGearDef { TextureFile = "GEAR_FoodCannedPineapple_Dif.png", SourceTextureName = "GEAR_FoodCannedPineapple_Dif", MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" } },
            new CannedGearDef { TextureFile = "GEAR_CannedSardines_Dif.png", SourceTextureName = "GEAR_CannedSardines_Dif", ParentName = "GEAR_CannedSardines" },
        };

        private static string NormalizeName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"\s\(\d+\)$", "");
        }

        private static readonly Dictionary<string, List<Material>> _decalMaterialCache = new Dictionary<string, List<Material>>();

        private static readonly Dictionary<string, string> _decalTextureMap = new Dictionary<string, string>
        {
            { "FX_DecalWolfScaredFlares_A01", "FX_DecalWolfScaredFlares_A01.png" },
            { "FX_DecalGraffity_A09",         "FX_DecalGraffity_A09.png"         },
            { "FX_MaintenanceShedStoryDecal", "FX_MaintenanceShedStoryDecal.png" },
            { "FX_BusDecalBlackRock", "FX_BusDecalBlackRock.png" },
            { "FX_LangstonTrainLetters_A01", "FX_LangstonTrainLetters_A.png" }, 
        };
        //maybe fix this in future versions?
        internal static void ForceReplaceDecals(bool forceRescan = false)
        {
            if (forceRescan)
                _decalMaterialCache.Clear();

            bool needScan = false;
            foreach (var key in _decalTextureMap.Keys)
                if (!_decalMaterialCache.ContainsKey(key)) { needScan = true; break; }

            if (needScan)
            {
                foreach (var key in _decalTextureMap.Keys)
                    if (!_decalMaterialCache.ContainsKey(key))
                        _decalMaterialCache[key] = new List<Material>();

                foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (mat == null) continue;

                    foreach (var kv in _decalTextureMap)
                    {
                        string texKey = kv.Key;
                        if (mat.name != null && mat.name.Contains(texKey))
                        {
                            _decalMaterialCache[texKey].Add(mat);
                            continue;
                        }

                        Texture mainTex = mat.mainTexture;
                        if (mainTex != null && mainTex.name.Contains(texKey))
                        { _decalMaterialCache[texKey].Add(mat); continue; }
                        if (mat.HasProperty("_MainTex"))
                        {
                            var t = mat.GetTexture("_MainTex");
                            if (t != null && t.name.Contains(texKey))
                                _decalMaterialCache[texKey].Add(mat);
                        }
                    }
                }

                //MelonLogger.Msg($"[Decals] Scanned: " +
                //    string.Join(", ", System.Linq.Enumerable.Select(
                //        _decalMaterialCache, kv => $"{kv.Key}={kv.Value.Count}")));
            }

            foreach (var kv in _decalTextureMap)
            {
                string texKey = kv.Key;
                string fileName = kv.Value;

                if (!_decalMaterialCache.TryGetValue(texKey, out var mats) || mats.Count == 0) continue;

                Texture2D newTex = GetOrLoadTexture(fileName);
                if (newTex == null) continue;

                foreach (var mat in mats)
                {
                    if (mat == null) continue;
                    mat.mainTexture = newTex;
                    if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", newTex);
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", newTex);
                }
            }
        }

        internal static readonly List<TextureRule> Rules = BuildRules();

        internal static void ApplyTexturesToObject(GameObject root)
        {
            if (root == null) return;

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;

                string objName = NormalizeName(renderer.gameObject.name);

                foreach (var rule in Rules)
                {
                    bool nameMatch = rule.ExactName
                        ? objName == rule.TargetName
                        : objName.Contains(rule.TargetName);

                    if (!nameMatch) continue;

                    if (rule.ParentName != null)
                    {
                        Transform parent = renderer.transform.parent;
                        bool parentFound = false;
                        while (parent != null)
                        {
                            if (parent.name == rule.ParentName) { parentFound = true; break; }
                            parent = parent.parent;
                        }
                        if (!parentFound) continue;
                    }

                    Texture2D newTex = GetOrLoadTexture(rule.TextureFile);
                    if (newTex == null) continue;

                    Material[] mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;

                        if (rule.SourceTextureName != null)
                        {
                            string matName = mats[i].name;
                            Texture currentTex = mats[i].mainTexture;
                            string texName = currentTex != null ? currentTex.name : "";

                            if (!matName.Contains(rule.SourceTextureName) &&
                                !texName.Contains(rule.SourceTextureName)) continue;
                        }

                        mats[i].mainTexture = newTex;


                        if (rule.DamageTextureFile != null)
                        {
                            Texture2D dmgTex = GetOrLoadTexture(rule.DamageTextureFile);
                            if (dmgTex != null)
                                mats[i].SetTexture(711, dmgTex);
                        }
                    }
                }
            }
        }

        private static List<TextureRule> BuildRules()
        {
            var rules = new List<TextureRule>();

            foreach (var def in CannedDefs)
            {
                foreach (var mesh in def.MeshNames)
                {
                    rules.Add(new TextureRule
                    {
                        TextureFile = def.TextureFile,
                        TargetName = mesh,
                        ParentName = def.ParentName,
                        SourceTextureName = def.SourceTextureName
                    });

                    if (def.LidTextureFile != null && def.LidSourceName != null)
                    {
                        rules.Add(new TextureRule
                        {
                            TextureFile = def.LidTextureFile,
                            TargetName = mesh,
                            ParentName = def.ParentName,
                            SourceTextureName = def.LidSourceName
                        });
                    }
                }
            }

            rules.AddRange(new[]
            {
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png", TargetName = "RifleAmmoBox_LOD0", SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png", TargetName = "RifleAmmoBox_LOD1", SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png", TargetName = "RifleAmmoBox_Old", SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png", TargetName = "GEAR_RifleAmmoBox", SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RevolverAmmoBox_LOD0", SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RevolverAmmoBox_LOD1", SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RifleAmmoBox_Old", SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "GEAR_RevolverAmmoBox", SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_Matches_Dif.png", TargetName = "Matches", ExactName = true, SourceTextureName = "GEAR_Matches_Mat" },
                new TextureRule { TextureFile = "GEAR_Matches_Dif.png", TargetName = "OBJ_WoodMatches_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_Matches_Dif.png", TargetName = "OBJ_WoodMatches_LOD1", ExactName = true },
                new TextureRule { TextureFile = "GEAR_AccelerantLighterFuel_Dif.png", TargetName = "Accelerant_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_AccelerantLighterFuel_Dif.png", TargetName = "Accelerant_LOD1", ExactName = true },
                new TextureRule { TextureFile = "GEAR_AntibioticPillBottle_Dif.png", TargetName = "BottleAntibiotics_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_AntibioticPillBottle_Dif.png", TargetName = "BottleAntibiotics_LOD1", ExactName = true },
                new TextureRule { TextureFile = "GEAR_HydrogenPeroxide_Dif.png", TargetName = "BottleHydrogenPeroxide_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_HydrogenPeroxide_Dif.png", TargetName = "BottleHydrogenPeroxide_LOD1", ExactName = true },
                new TextureRule { TextureFile = "GEAR_Flare_Dif.png", TargetName = "UncappedFlareMesh", SourceTextureName = "GEAR_Flare_Mat" },
                new TextureRule { TextureFile = "GEAR_Flare_Blue_Dif.png", TargetName = "UncappedFlareMesh", SourceTextureName = "GEAR_Flare_Blue_Mat" },
                new TextureRule { TextureFile = "GEAR_SewingKit_Dif.png", TargetName = "SewingKitMesh", ExactName = true },
                new TextureRule { TextureFile = "GEAR_CoffeeTin_Dif.png", TargetName = "OBJ_CoffeeTin_LOD0", SourceTextureName = "GEAR_CoffeeTin_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTin_Dif.png", TargetName = "OBJ_CoffeeTin_LOD1", SourceTextureName = "GEAR_CoffeeTin_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTinLid_Dif.png", TargetName = "OBJ_CoffeeTin_LOD0", SourceTextureName = "GEAR_CoffeeTinLid_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTinLid_Dif.png", TargetName = "OBJ_CoffeeTin_LOD1", SourceTextureName = "GEAR_CoffeeTinLid_Dif" },
                new TextureRule { TextureFile = "GEAR_Film_Box_BW.png", TargetName = "GEAR_FilmBoxBW" },
                new TextureRule { TextureFile = "GEAR_Film_Box_Colour.png", TargetName = "GEAR_FilmBoxColour" },
                new TextureRule { TextureFile = "GEAR_Film_Box_Sepia.png", TargetName = "GEAR_FilmBoxSepia" },
                new TextureRule { TextureFile = "GEAR_GranolaBar_Dif.png", TargetName = "GranolaBarMesh" },
                new TextureRule { TextureFile = "GEAR_FoodCannedCorn_Dif.png", TargetName = "OBJ_CannedCornFood", SourceTextureName = "GEAR_FoodCannedCorn_Dif" },
                new TextureRule { TextureFile = "GEAR_PeanutButter_Dif.png", TargetName = "PeanutButter_LOD0", SourceTextureName = "GEAR_PeanutButter_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodSodaSummit_Dif.png", TargetName = "FoodSodaCan_LOD0", SourceTextureName = "GEAR_FoodSodaSummit_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodSodaOrange_Dif.png", TargetName = "FoodSodaCan_LOD0", SourceTextureName = "GEAR_FoodSodaOrange_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodSodaGrape_Dif.png", TargetName = "FoodSodaCan_LOD0", SourceTextureName = "GEAR_FoodSodaGrape_Dif" },
                new TextureRule { TextureFile = "GEAR_Oats.png", TargetName = "OBJ_OatsTin_LOD0", SourceTextureName = "GEAR_Oats" },
                new TextureRule { TextureFile = "GEAR_FoodCannedHam_Dif.png", TargetName = "OBJ_CannedHamFood", ExactName = true },
                new TextureRule { TextureFile = "GEAR_DriedApples.png", TargetName = "OBJ_DriedApples_LOD0", SourceTextureName = "GEAR_DriedApples" },
                new TextureRule { TextureFile = "GEAR_KetchupChips_Dif.png", TargetName = "BeefJerky_LOD0", SourceTextureName = "GEAR_KetchupChips_Dif" },
                new TextureRule { TextureFile = "GEAR_SaltBag.png", TargetName = "OBJ_SaltBag_LOD0", SourceTextureName = "GEAR_SaltBag" },
                new TextureRule { TextureFile = "GEAR_FoodMRE_Dif.png", TargetName = "Obj_FoodMRE_LOD0", SourceTextureName = "GEAR_FoodMRE_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodEnergyBar_Dif.png", TargetName = "CandyBarMesh_LOD0", SourceTextureName = "GEAR_FoodEnergyBar_Dif" },
                new TextureRule { TextureFile = "GEAR_WaterPurificationTablets_Dif.png", TargetName = "WaterPurificationTablets_LOD0", SourceTextureName = "GEAR_WaterPurificationTablets_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodBeefJerky_Dif.png", TargetName = "BeefJerky_LOD0", SourceTextureName = "GEAR_FoodBeefJerky_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodCandyBars_Dif.png", TargetName = "CandyBarMesh", SourceTextureName = "GEAR_FoodCandyBars_Mat" },
                new TextureRule { TextureFile = "GEAR_GreenTeaPackage_Dif.png", TargetName = "OBJ_GreenTeaPackage_LOD0", SourceTextureName = "GEAR_GreenTeaPackage_Dif" },
                new TextureRule { TextureFile = "GEAR_GunpowderCan_Dif.png", TargetName = "GunpowderCan_LOD0", SourceTextureName = "GEAR_GunpowderCan_Dif" },
                new TextureRule { TextureFile = "GEAR_Cereal_A.png", TargetName = "OBJ_CerealBox_LOD0", SourceTextureName = "GEAR_Cereal_A" },
                new TextureRule { TextureFile = "GEAR_BoxedCrackers_Dif.png", TargetName = "OBJ_BoxedCrackers_LOD0", SourceTextureName = "GEAR_BoxedCrackers_Dif" },
                new TextureRule { TextureFile = "GEAR_FlourBag.png", TargetName = "OBJ_FlourBag_LOD0", SourceTextureName = "GEAR_FlourBag" },
                new TextureRule { TextureFile = "GEAR_InsulatedFlask_G.png", TargetName = "OBJ_InsulatedFlask_LOD0", SourceTextureName = "GEAR_InsulatedFlask_G" },
                new TextureRule { TextureFile = "GEAR_MapleSyrup_Dif.png", TargetName = "PeanutButter_LOD0", SourceTextureName = "GEAR_MapleSyrup_Dif" },
                new TextureRule { TextureFile = "GEAR_LampFuel_Dif.png", TargetName = "LampFuel" },
                new TextureRule { TextureFile = "GEAR_LampFuel_Dif.png", TargetName = "LampFuel_LOD0" },
                new TextureRule { TextureFile = "GEAR_CarBattery_Dif.png", TargetName = "CarBattery_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_WaterBottleLabel.png", TargetName = "Water500ml_LOD0", ExactName = true, SourceTextureName = "GEAR_WaterBottleLabel_Mat" },
                new TextureRule { TextureFile = "GEAR_WaterBottleLabel.png", TargetName = "Water1000ml_LOD0", ExactName = true, SourceTextureName = "GEAR_WaterBottleLabel_Mat" },
                new TextureRule { TextureFile = "GLB_MetalRusted_D.png", TargetName = "OBJ_SignDamEntrance_LOD0", ExactName = true },
                new TextureRule { TextureFile = "CLTH_ACC_BallisticVest.png", TargetName = "OBJ_BallisticVest_LOD0", SourceTextureName = "CLTH_ACC_BallisticVest" },
                new TextureRule { TextureFile = "STR_CrashedAirliner.png", TargetName = "OBJ_PlaneMainBody_LOD0", ExactName = true },
                new TextureRule { TextureFile = "TEX_PrisonBus_Complete_BRtext.png", TargetName = "OBJ_PrisonBus_Complete_text", ExactName = true },
                new TextureRule { TextureFile = "GEAR_GranolaBar_Dif.png", TargetName = "OBJ_CandyBoxD_LOD0", ExactName = true, SourceTextureName = "GEAR_GranolaBar_Mat" },
                new TextureRule { TextureFile = "FX_DecalWolfScaredFlares_A01.png", TargetName = "Decal", SourceTextureName = "FX_DecalWolfScaredFlares_A01" },
                new TextureRule { TextureFile = "FX_BusDecalBlackRock.png", TargetName = "Decal", SourceTextureName = "FX_BusDecalBlackRock" },
                new TextureRule { TextureFile = "FX_DecalGraffity_A09.png", TargetName = "Decal", SourceTextureName = "FX_DecalGraffity_A09" },
                new TextureRule { TextureFile = "FX_MaintenanceShedStoryDecal.png", TargetName = "Decal", SourceTextureName = "Decal-320440" }, 
                new TextureRule { TextureFile = "STR_PostOfficeASign_A.png", TargetName = "STR_PostOfficeA_LOD0", ExactName = true, SourceTextureName = "STR_PostOfficeASign_A01" },
                new TextureRule { TextureFile = "STR_WaterTower_Dif.png", TargetName = "STR_WaterTower_LOD0", ExactName = true, SourceTextureName = "STR_WaterTower_Mat" },
                new TextureRule { TextureFile = "GEAR_FoodCandyBars_Dif.png", TargetName = "OBJ_CandyBoxA_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodCandyBars_Mat" },
                new TextureRule { TextureFile = "OBJ_SulfurBag_Dif.png", TargetName = "ScrapMetal_LOD0", ParentName = "GEAR_DustingSulfur", SourceTextureName = "OBJ_SulfurBag_Mat" },
                new TextureRule { TextureFile = "OBJ_FoodEnergyDrink_A.png", TargetName = "OBJ_FoodEnergyDrink_A", SourceTextureName = "OBJ_FoodEnergyDrink_A" },
                new TextureRule { TextureFile = "OBJ_CaffeineBox.png", TargetName = "OBJ_CaffeineBox", SourceTextureName = "OBJ_CaffeineBox" },
                new TextureRule { TextureFile = "OBJ_PotassiumNitrate_Dif.png", TargetName = "ScrapMetal_LOD0", ParentName = "GEAR_StumpRemover", SourceTextureName = "OBJ_PotassiumNitrateMesh_Mat" },
                new TextureRule { TextureFile = "OBJ_Cash_Dif.png", TargetName = "OBJ_CashBundle" },
                new TextureRule { TextureFile = "OBJ_HeatPad.png", TargetName = "OBJ_HeatPad", SourceTextureName = "OBJ_HeatPad" },
                new TextureRule { TextureFile = "OBJ_BookRevolver_A.png", TargetName = "Mesh", SourceTextureName = "OBJ_BookRevolver_A" },
                new TextureRule { TextureFile = "OBJ_BookHardcover_Cooking.png", TargetName = "Mesh", ParentName = "GEAR_BookCooking", SourceTextureName = "OBJ_Book_Cooking_Mat" },
                new TextureRule { TextureFile = "OBJ_BookSoftcover_Frontier.png", TargetName = "Mesh", ParentName = "GEAR_BookRifleFirearm", SourceTextureName = "OBJ_Book_Frontier_Mat" },
                new TextureRule { TextureFile = "OBJ_BookSoftcover_IceFishing.png", TargetName = "Mesh", ParentName = "GEAR_BookIceFishing", SourceTextureName = "OBJ_Book_Fishing_Mat" },
                new TextureRule { TextureFile = "OBJ_BookMagazine_Guns.png", TargetName = "Mesh", ParentName = "GEAR_BookRifleFirearmAdvanced", SourceTextureName = "OBJ_Book_Guns_Mat" },
                new TextureRule { TextureFile = "OBJ_BookGunsmithing_A.png", TargetName = "Mesh", ParentName = "GEAR_BookGunsmithing", SourceTextureName = "OBJ_BookGunsmithing_A" },
                new TextureRule { TextureFile = "OBJ_BookHardcover_Mending.png", TargetName = "Mesh", ParentName = "GEAR_BookMending", SourceTextureName = "OBJ_Book_Mending_Mat" },
                new TextureRule { TextureFile = "OBJ_BookMagazine_Archery.png", TargetName = "Mesh", ParentName = "GEAR_BookArchery", SourceTextureName = "OBJ_Book_Archery_Mat" },
                new TextureRule { TextureFile = "OBJ_BookHardcover_FieldDressingVol1.png", TargetName = "Mesh", ParentName = "GEAR_BookCarcassHarvesting", SourceTextureName = "OBJ_Book_Harvesting_Mat" },
                new TextureRule { TextureFile = "OBJ_BookHardcover_Survive.png", TargetName = "Mesh", ParentName = "GEAR_BookFireStarting", SourceTextureName = "OBJ_Book_FireStarting_Mat" },
                new TextureRule { TextureFile = "OBJ_Sign_C.png", TargetName = "OBJ_RoadSignI_LOD0", SourceTextureName = "OBJ_Sign_C01" },
                new TextureRule { TextureFile = "OBJ_Sign_C.png", TargetName = "OBJ_RoadSignI_LOD1", SourceTextureName = "OBJ_Sign_C01" },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "OBJ_SignDamCarterB_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "OBJ_SignDamCarterB_LOD1", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RailwayTruck.png", TargetName = "OBJ_CarTruckDoorEXT_LOD0", ExactName = true, SourceTextureName = "OBJ_RailwayTruck" },
                new TextureRule { TextureFile = "OBJ_IndustrialDebrisA.png", TargetName = "OBJ_IndustrailDebrisC_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignDam_B_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignDam_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignRestroom_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignDam_C_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignAidStation_B_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignAidStation_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignElevator_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "OBJ_SignDamTresspassing_LOD0", ExactName = true, SourceTextureName = "OBJ_SignDam_A" },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "OBJ_SignDamCarterInterior", SourceTextureName = "OBJ_SignDam_A01" },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "OBJ_SignDam_A01", ExactName = true, SourceTextureName = "OBJ_SignDam_A01" },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_B.png", TargetName = "OBJ_SignDeco7_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_B.png", TargetName = "OBJ_SignDeco1_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_B.png", TargetName = "OBJ_SignDeco2_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png", TargetName = "STR_DaTuSign_Prefab", ExactName = true, SourceTextureName = "OBJ_SignDam_A01" },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_B.png", TargetName = "OBJ_SignDeco6_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_B.png", TargetName = "OBJ_SignDeco5_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Foreclosed_A.png", TargetName = "OBJ_Thomson_Foreclosure_B_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Thompson_Signs_A.png", TargetName = "STR_Thompson_Store_LOD0", ExactName = true, SourceTextureName = "STR_Thomson_Signs_A" },
                new TextureRule { TextureFile = "OBJ_PosterGarden.png", TargetName = "OBJ_Poster_VictoryGarden", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignStop_A.png", TargetName = "OBJ_SignStopB_LOD0", ExactName = true, SourceTextureName = "OBJ_SignStop_A02" },
                new TextureRule { TextureFile = "OBJ_RoadsideStand_A.png", TargetName = "STR_RoadsideStand_A_LOD0", ExactName = true, SourceTextureName = "OBJ_RoadsideStand_A" },
                new TextureRule { TextureFile = "OBJ_PlanePartsA_Dif.png", TargetName = "OBJ_PlaneTail_Prefab", SourceTextureName = "OBJ_PlanePartsA_Mat" },
                new TextureRule { TextureFile = "OBJ_BlackRockMineA_Signs_Base_B.png", TargetName = "OBJ_Blackrock_Mine_sign_E_Prefab", ExactName = true, SourceTextureName = "OBJ_Blackrockmine_signs_B" },
                new TextureRule { TextureFile = "OBJ_BlackRockMineA_Signs_Base_A.png", TargetName = "OBJ_Blackrock_Mine_sign_B_Prefab", ExactName = true, SourceTextureName = "OBJ_Blackrockmine_signs_A" },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_MineSignC_LOD0", ExactName = true, SourceTextureName = "OBJ_MineSigns_A" },
                new TextureRule { TextureFile = "OBJ_Mine_Sign_ToxicGas.png", TargetName = "OBJ_Blackrock_Mine_ToxicGas_Sign_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_B.png", TargetName = "OBJ_RoadSignG_LOD0", ExactName = true, SourceTextureName = "OBJ_Sign_B01" },
                new TextureRule { TextureFile = "OBJ_CarTruck_Blackrock.png", TargetName = "OBJ_Blackrock_TruckDoorRightEXT_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_SignPrisonB_LOD0", ExactName = true, SourceTextureName = "OBJ_MineandPrisonSignA" },
                new TextureRule { TextureFile = "OBJ_SignMilton_B.png", TargetName = "OBJ_MiltonRoadSignA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Thompson_Signs_A.png", TargetName = "OBJ_Thomson_MaxWeight_Sign_A_LOD0", ExactName = true, SourceTextureName = "STR_Thomson_Signs_A" },
                new TextureRule { TextureFile = "OBJ_CargoContainer_A.png", TargetName = "OBJ_CargoContainer_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_SignMineG_LOD0", ExactName = true, SourceTextureName = "OBJ_MineSignA" },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_D.png", TargetName = "OBJ_PrisonStopCheckSignA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_SignPrisonD_LOD0", ExactName = true, SourceTextureName = "OBJ_MineandPrisonSignA" },
                new TextureRule { TextureFile = "OBJ_BlackrockPower_Signs.png", TargetName = "OBJ_PrisonDirectorySignA_LOD0", ExactName = true, SourceTextureName = "OBJ_Sign_G01" },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_C.png", TargetName = "OBJ_PrisonInfirmarySignA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_E.png", TargetName = "OBJ_ClipBoardPapers_A", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_E.png", TargetName = "OBJ_BuisnessCard_A_Prefab", ExactName = true },
                new TextureRule { TextureFile = "OBJ_signs_tunnel_base_A.png", TargetName = "OBJ_Sign_Steamtunnel_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_E.png", TargetName = "OBJ_Sign_Hotsteam_A", ExactName = true },
                new TextureRule { TextureFile = "OBJ_signs_tunnel_base_A.png", TargetName = "OBJ_Sign_Steamtunnel_C_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_signs_tunnel_base_A.png", TargetName = "OBJ_Sign_Steamtunnel_B_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_signs_tunnel_base_A.png", TargetName = "OBJ_Sign_Steamtunnel_E_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_D.png", TargetName = "OBJ_SignControlRoom_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_signs_tunnel_base_A.png", TargetName = "OBJ_Sign_Steamtunnel_D_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignQuincysQuonset_A.png", TargetName = "OBJ_SignQuincysQuonset_A_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_GasPump_B.png", TargetName = "OBJ_GasPump_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RailwayTruck.png", TargetName = "OBJ_CarTruckDoorLeftEXT_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RailwayTruckCannery_A.png", TargetName = "", ExactName = true },
                new TextureRule { TextureFile = "OBJ_FishingBoat_lower_Dif.png", TargetName = "OBJ_FishingBoatA_Prefab", ExactName = true, SourceTextureName = "OBJ_FishingBoat_lower_Mat" },
                new TextureRule { TextureFile = "OBJ_SignCannery_A.png", TargetName = "OBJ_SignCannarySmall_A", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignCannery_A.png", TargetName = "OBJ_SignCanneryBig_A", ExactName = true },
                new TextureRule { TextureFile = "OBJ_LocomotiveA_Dif.png", TargetName = "OBJ_LocomotiveA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignMilton_C.png", TargetName = "OBJ_HuntingLodgeASign_LOD0", ExactName = true, SourceTextureName = "OBJ_SignMilton_C01" },
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "STR_ConvenienceStoreA_LOD0", ExactName = true, SourceTextureName = "OBJ_InteriorWallDecoStore_A01" },
                new TextureRule { TextureFile = "OBJ_SignGasStation_A.png", TargetName = "OBJ_GasStationExteriorSignA_LOD0", ExactName = true, SourceTextureName = "OBJ_SignGasStation_A01"},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "STR_GasStationCanopy_LOD0", ExactName = true, SourceTextureName = "OBJ_InteriorWallDecoStore_A01" },
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign10_LOD0", ExactName = true, SourceTextureName = "OBJ_InteriorWallDecoStore_A01"},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign2_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign11_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_ConvenienceStoreNotes_Dif.png", TargetName = "OBJ_StoreSignNote5_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSignNote4_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign3_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_ConvenienceStoreNotes_Dif.png", TargetName = "OBJ_StoreSignNote2_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign9_LOD0", ExactName = true, SourceTextureName = "OBJ_InteriorWallDecoStore_A01"},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign1_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign12_LOD0", ExactName = true, SourceTextureName = "OBJ_InteriorWallDecoStore_A01"},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign4_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_StoreSign5_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_ConvenienceStoreNotes_Dif.png", TargetName = "OBJ_StoreSignNote3_Prefab", ExactName = true},
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_WetFloorSign_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_ConvenienceStoreNotes_Dif.png", TargetName = "OBJ_StoreSignNote1_LOD0", ExactName = true},
                new TextureRule { TextureFile = "OBJ_ConvenienceStoreNotes_Dif.png", TargetName = "OBJ_StoreSignNote6_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_InteriorCampOfficeDeco_A.png", TargetName = "OBJ_PictureFrameWelcomeSign_Prefab (PLACED)", ExactName = true},
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A0_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A1_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A2_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A3_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A4_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_PosterCougar_A.png", TargetName = "OBJ_PosterCougar_A5_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignMilton_C.png", TargetName = "OBJ_PostOfficeSignB_LOD0", ExactName = true, SourceTextureName = "OBJ_SignMilton_C01" },
                new TextureRule { TextureFile = "OBJ_SignCreditUnion_A.png", TargetName = "STR_BankA_LOD0", ExactName = true, SourceTextureName = "OBJ_CreditUnionSign_Mat" },
                new TextureRule { TextureFile = "OBJ_InteriorWallDecoStore.png", TargetName = "OBJ_NoCashNote_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_CandyStand.png", TargetName = "OBJ_CandyStand_LOD0", ExactName = true, SourceTextureName = "OBJ_CandyStand_Mat" },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PancakePeach", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PieMeat", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PorridgeFruit", ExactName = true }, 
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_Fishcakes", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PieFishermans", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_B.png", TargetName = "OBJ_RecipeCard_SoupPotato", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_B.png", TargetName = "OBJ_RecipeCard_BarPemmican", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_B.png", TargetName = "OBJ_RecipeCard_SoupRabbit", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PieForagers", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_StewMeat", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_PiePredator", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RecipeCards_A.png", TargetName = "OBJ_RecipeCard_StewVegetables", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignMilton_A.png", TargetName = "OBJ_MiltonSign_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_RailStation_Signs.png", TargetName = "OBJ_Railstation_Sign_B", ExactName = true, SourceTextureName = "OBJ_RailStation_Signs" },
                new TextureRule { TextureFile = "OBJ_RailStation_Signs.png", TargetName = "OBJ_RailStation_Sign_A", ExactName = true, SourceTextureName = "OBJ_RailStation_Signs" }, 
                
                //Graves
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneA_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneB_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneC_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneD_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneE_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },
                new TextureRule { TextureFile = "OBJ_GraveStonesA.png", TargetName = "OBJ_GraveStoneF_LOD0", ExactName = true, SourceTextureName = "OBJ_GraveStonesA_Mat" },

                new TextureRule { TextureFile = "GEAR_PotatoSackContainer_A.png", TargetName = "OBJ_PotatoSackContainer_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_Camera.png", TargetName = "GEAR_Camera", ExactName = true },
                new TextureRule { TextureFile = "GEAR_Camera.png", TargetName = "OBJ_Camera_LOD0"},//CAMERA IN HANDS FIXED!
                new TextureRule { TextureFile = "FX_DecalHangarSign.png", TargetName = "STR_AF_Hangar_Sign_Prefab", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSignAtlasA.png", TargetName = "OBJ_MineArea_SignJ_LOD0", ExactName = true, SourceTextureName = "OBJ_MineSignAtlasA_Mat" },
                new TextureRule { TextureFile = "OBJ_Airfield_Signs.png", TargetName = "OBJ_PrisonDirectorySignA_LOD0", ExactName = true, SourceTextureName = "OBJ_Airfield_Signs" },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_D.png", TargetName = "OBJ_PrisonLoadingAreaSignA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_MineSignA_LOD0", ExactName = true, SourceTextureName = "OBJ_MineSignA" },
                new TextureRule { TextureFile = "OBJ_BlackRockMineA_Signs_Base_B.png", TargetName = "OBJ_MineArea_SignD_Prefab (PLACED)", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSigns_A.png", TargetName = "OBJ_MineSignB_LOD0", ExactName = true, SourceTextureName = "OBJ_MineSignA" },
                new TextureRule { TextureFile = "OBJ_BlackRockMineA_Signs_Base_A.png", TargetName = "OBJ_Blackrock_Mine_sign_D_Prefab", ExactName = true },
                new TextureRule { TextureFile = "OBJ_MineSignAtlasA.png", TargetName = "OBJ_SignThink_A_Prefab (PLACED)", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Mountain_Pass_Signs_A.png", TargetName = "OBJ_Sign_Refuge_Cabin_A_Prefab", ExactName = true },
                new TextureRule { TextureFile = "OBJ_IndustrialDeco_D.png", TargetName = "OBJ_PrisonExitSignA_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignPosters.png", TargetName = "OBJ_SignPostalLake_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignPosters.png", TargetName = "OBJ_SignPostalCoastal_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignPosters.png", TargetName = "OBJ_SignPostalWhaling_LOD0", ExactName = true },
                new TextureRule { TextureFile = "OBJ_Airfield_Signs.png", TargetName = "OBJ_AirfieldTerminalSignB_Prefab (PLACED)", ExactName = true },
                new TextureRule { TextureFile = "OBJ_TransformerInterior_B_Dif.png", TargetName = "OBJ_TransformerInterior_B", ExactName = true },
                new TextureRule { TextureFile = "GEAR_CarBattery_Dif.png", TargetName = "OBJ_TransformerBatteryFixed_B", ExactName = true },
                new TextureRule { TextureFile = "GEAR_FoodMRE_Dif.png", TargetName = "OBJ_MREWrapper_Old", ExactName = true },
                new TextureRule{TextureFile = "OBJ_FoodAirlineChicken_A.png", TargetName = "Obj_FoodAirline_A_LOD0", ExactName = true, ParentName = "GEAR_AirlineFoodChick", SourceTextureName = "OBJ_FoodAirlineChicken"},
                new TextureRule { TextureFile = "OBJ_FoodAirlineVeg_A.png",   TargetName = "Obj_FoodAirline_A_LOD0", ExactName = true, ParentName = "GEAR_AirlineFoodVeg", SourceTextureName = "OBJ_FoodAirlineVeg" },
            });

            return rules;
        }

        internal static readonly Dictionary<string, string> UITextureMap = new Dictionary<string, string> 
        {
            // icons
            { "ico_GearItem__PinnacleCanPeaches", "ico_GearItem__PinnacleCanPeaches.png" },
            { "ico_GearItem__TomatoSoupCan", "ico_GearItem__TomatoSoupCan.png" },
            { "ico_GearItem__WoodMatches", "ico_GearItem__WoodMatches.png" },
            { "ico_GearItem__Accelerant", "ico_GearItem__Accelerant.png" },
            { "ico_GearItem__BottleAntibiotics", "ico_GearItem__BottleAntibiotics.png" },
            { "ico_GearItem__BottleHydrogenPeroxide", "ico_GearItem__BottleHydrogenPeroxide.png" },
            { "ico_GearItem__FlareA", "ico_GearItem__FlareA.png" },
            { "ico_GearItem__RifleAmmoBox", "ico_GearItem__RifleAmmoBox.png" },
            { "ico_GearItem__SewingKit", "ico_GearItem__SewingKit.png" },
            { "ico_GearItem__DustingSulfur", "ico_GearItem__DustingSulfur.png" },
            { "ico_GearItem__CoffeeTin", "ico_GearItem__CoffeeTin.png" },
            { "ico_GearItem__FilmBoxColour", "ico_GearItem__FilmBoxColour.png" },
            { "ico_GearItem__FilmBoxBW", "ico_GearItem__FilmBoxBW.png" },
            { "ico_GearItem__FilmBoxSepia", "ico_GearItem__FilmBoxSepia.png" },
            { "ico_GearItem__GranolaBar", "ico_GearItem__GranolaBar.png" },
            { "ico_GearItem__RevolverAmmoBox", "ico_GearItem__RevolverAmmoBox.png" },
            { "ico_GearItem__DogFood", "ico_GearItem__DogFood.png" },
            { "ico_GearItem__CannedCorn", "ico_GearItem__CannedCorn.png" },
            { "ico_GearItem__CannedBeans", "ico_GearItem__CannedBeans.png" },
            { "ico_GearItem__CondensedMilk", "ico_GearItem__CondensedMilk.png" },
            { "ico_GearItem__CannedPineapple", "ico_GearItem__CannedPineapple.png" },
            { "ico_GearItem__PeanutButter", "ico_GearItem__PeanutButter.png" },
            { "ico_GearItem__Soda", "ico_GearItem__Soda.png" },
            { "ico_GearItem__SodaOrange", "ico_GearItem__SodaOrange.png" },
            { "ico_GearItem__SodaGrape", "ico_GearItem__SodaGrape.png" },
            { "ico_GearItem__OatsTin", "ico_GearItem__OatsTin.png" },
            { "ico_GearItem__CannedHam", "ico_GearItem__CannedHam.png" },
            { "ico_GearItem__DriedApples", "ico_GearItem__DriedApples.png" },
            { "ico_GearItem__KetchupChips", "ico_GearItem__KetchupChips.png" },
            { "ico_GearItem__SaltBag", "ico_GearItem__SaltBag.png" },
            { "ico_GearItem__BlueFlare", "ico_GearItem__BlueFlare.png" },
            { "ico_GearItem__HeatPad", "ico_GearItem__HeatPad.png" },
            { "ico_GearItem__MRE", "ico_GearItem__MRE.png" },
            { "ico_GearItem__EnergyBar", "ico_GearItem__EnergyBar.png" },
            { "ico_GearItem__CannedSardines", "ico_GearItem__CannedSardines.png" },
            { "ico_GearItem__WaterPurificationTablets", "ico_GearItem__WaterPurificationTablets.png" },
            { "ico_GearItem__BallisticVest", "ico_GearItem__BallisticVest.png" },
            { "CLTH_ACC_CVR_BallisticVest", "ico_GearItem__BallisticVest.png" },
            { "CLTH_ACC_BallisticVest", "CLTH_ACC_BallisticVest1.png" },
            { "ico_GearItem__BeefJerky", "ico_GearItem__BeefJerky.png" },
            { "ico_GearItem__CashBundle", "ico_GearItem__CashBundle.png" },
            { "ico_GearItem__BottleVitaminC", "ico_GearItem__BottleVitaminC.png" },
            { "ico_GearItem__CandyBar", "ico_GearItem__CandyBar.png" },
            { "ico_GearItem__SodaEnergy", "ico_GearItem__SodaEnergy.png" },
            { "ico_GearItem__BottleCaffeine", "ico_GearItem__BottleCaffeine.png" },
            { "ico_GearItem__GreenTeaPackage", "ico_GearItem__GreenTeaPackage.png" },
            { "ico_GearItem__GunpowderCan", "ico_GearItem__GunpowderCan.png" },
            { "ico_GearItem__Cereal_A", "ico_GearItem__Cereal_A.png" },
            { "ico_GearItem__Crackers", "ico_GearItem__Crackers.png" },
            { "ico_GearItem__Flour", "ico_GearItem__Flour.png" },
            { "ico_GearItem__InsulatedFlask_G", "ico_GearItem__InsulatedFlask_G.png" },
            { "ico_GearItem__MapleSyrup", "ico_GearItem__MapleSyrup.png" },
            { "ico_GearItem__StumpRemover", "ico_GearItem__StumpRemover.png" },
            { "ico_GearItem__LampFuel", "ico_GearItem__LampFuel.png" },
            { "ico_GearItem__LampFuelFull", "ico_GearItem__LampFuel.png" },
            { "ico_GearItem__BookRevolverFirearm", "ico_GearItem__BookRevolverFirearm.png" },
            { "ico_GearItem__BookCooking", "ico_GearItem__BookCooking.png" },
            { "ico_GearItem__BookRifleFirearm", "ico_GearItem__BookRifleFirearm.png" },
            { "ico_GearItem__BookIceFishing", "ico_GearItem__BookIceFishing.png" },
            { "ico_GearItem__BookRifleFirearmAdvanced", "ico_GearItem__BookRifleFirearmAdvanced.png" },
            { "ico_GearItem__BookGunsmithing", "ico_GearItem__BookGunsmithing.png" },
            { "ico_GearItem__BookMending", "ico_GearItem__BookMending.png" },
            { "ico_GearItem__BookArchery", "ico_GearItem__BookArchery.png" },
            { "ico_GearItem__BookCarcassHarvesting", "ico_GearItem__BookCarcassHarvesting.png" },
            { "ico_GearItem__BookFireStarting", "ico_GearItem__BookFireStarting.png" },
            { "ico_GearItem__CarBattery", "ico_GearItem__CarBattery.png" },
            { "ico_GearItem__Camera", "ico_GearItem__Camera.png" },
            { "ico_GearItem__RecipeCardPancakePeach", "ico_GearItem__RecipeCardPancakePeach.png" },
            { "ico_GearItem__RecipeCardPieMeat", "ico_GearItem__RecipeCardPieMeat.png" },
            { "ico_GearItem__RecipeCardPorridgeFruit", "ico_GearItem__RecipeCardPorridgeFruit.png" },
            { "ico_GearItem__RecipeCardFishcakes", "ico_GearItem__RecipeCardFishcakes.png" },
            { "ico_GearItem__RecipeCardPieFishermans", "ico_GearItem__RecipeCardPieFishermans.png" },
            { "ico_GearItem__RecipeCardSoupPotato", "ico_GearItem__RecipeCardSoupPotato.png" },
            { "ico_GearItem__RecipeCardBarPemmican", "ico_GearItem__RecipeCardBarPemmican.png" },
            { "ico_GearItem__RecipeCardPieForagers", "ico_GearItem__RecipeCardPieForagers.png" },
            { "ico_GearItem__RecipeCardSoupRabbit", "ico_GearItem__RecipeCardSoupRabbit.png" },
            { "ico_GearItem__RecipeCardPiePredator", "ico_GearItem__RecipeCardPiePredator.png" },
            { "ico_GearItem__RecipeCardStewMeat", "ico_GearItem__RecipeCardStewMeat.png" },
            { "ico_GearItem__RecipeCardStewVegetables", "ico_GearItem__RecipeCardStewVegetables.png" },
            { "ico_GearItem__AirlineFoodChick", "ico_GearItem__AirlineFoodChick.png" },
            { "ico_GearItem__AirlineFoodVeg", "ico_GearItem__AirlineFoodVeg.png" },
        };

        internal static Dictionary<string, Texture2D> LoadedTextures = new Dictionary<string, Texture2D>();

        //private bool _f7Pressed = false;
        private static readonly List<float> _pendingTimers = new List<float>();
        private static string _pendingSceneName = null;

        //public override void OnInitializeMelon()
        //{
        //    LoggerInstance.Msg("[TranslatedTextures] Lazy loading enabled");
        //}

        private static Assembly _assembly = null;

        private static Assembly GetAssembly()
        {
            if (_assembly == null)
                _assembly = Assembly.GetExecutingAssembly();
            return _assembly;
        }

        private static Texture2D LoadEmbeddedTexture(string resourceName, string textureName)
        {
            try
            {
                Assembly asm = GetAssembly();
                if (asm == null)
                {
                    MelonLogger.Error($"[TranslatedTextures] Assembly is null for: {resourceName}");
                    return null;
                }

                using Stream stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MelonLogger.Error($"[TranslatedTextures] Not found: {resourceName}");
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] bytes = ms.ToArray();

                if (bytes.Length == 0)
                {
                    MelonLogger.Error($"[TranslatedTextures] Empty stream for: {resourceName}");
                    return null;
                }

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (ImageConversion.LoadImage(tex, bytes))
                {
                    tex.name = textureName;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    //MelonLogger.Msg($"[TranslatedTextures] Loaded: {textureName} ({tex.width}x{tex.height})");
                    return tex;
                }

                byte[] pngSig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                int idx = -1;
                for (int i = 0; i <= bytes.Length - pngSig.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < pngSig.Length; j++)
                    {
                        if (bytes[i + j] != pngSig[j]) { match = false; break; }
                    }
                    if (match) { idx = i; break; }
                }

                if (idx >= 0)
                {
                    int pngLen = bytes.Length - idx;
                    byte[] pngBytes = new byte[pngLen];
                    System.Array.Copy(bytes, idx, pngBytes, 0, pngLen);

                    Texture2D tex2 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex2, pngBytes))
                    {
                        tex2.name = textureName;
                        tex2.filterMode = FilterMode.Bilinear;
                        tex2.wrapMode = TextureWrapMode.Repeat;
                        tex2.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                        UnityEngine.Object.DontDestroyOnLoad(tex2);
                        //MelonLogger.Msg($"[TranslatedTextures] Loaded (embedded PNG): {textureName} ({tex2.width}x{tex2.height})");
                        return tex2;
                    }
                }

                MelonLogger.Error($"[TranslatedTextures] ImageConversion failed for: {textureName}");
                return null;
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"[TranslatedTextures] Exception loading '{resourceName}': {e.Message}");
                return null;
            }
        }

        internal static Texture2D GetOrLoadTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            if (fileName == ".png") return null;

            if (LoadedTextures.TryGetValue(fileName, out Texture2D existing))
                return existing;

            string resourceName = $"TranslatedTextures.Resources.Textures.Russian.{fileName}";
            string textureName = Path.GetFileNameWithoutExtension(fileName);
            Texture2D tex = LoadEmbeddedTexture(resourceName, textureName);

            if (tex == null && Path.GetExtension(fileName).Equals(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                string altFile = Path.ChangeExtension(fileName, ".psd");
                string altResource = $"TranslatedTextures.Resources.Textures.Russian.{altFile}";
                tex = LoadEmbeddedTexture(altResource, textureName);
            }

            if (tex != null)
                LoadedTextures[fileName] = tex;

            return tex;
        }

        internal static void ScheduleReplaceAfterDelay(string sceneName)
        {
            _pendingSceneName = sceneName;
            _pendingTimers.Clear();

            _pendingTimers.Add(0.15f);
            _pendingTimers.Add(0.4f);
        }

        public override void OnUpdate()
        {
            if (_pendingTimers.Count > 0 && _pendingSceneName != null)
            {
                for (int i = _pendingTimers.Count - 1; i >= 0; i--)
                {
                    _pendingTimers[i] -= Time.deltaTime;
                    if (_pendingTimers[i] <= 0f)
                    {
                        _pendingTimers.RemoveAt(i);
                        StaticReplaceTextures(_pendingSceneName);
                    }
                }
                if (_pendingTimers.Count == 0) _pendingSceneName = null;
            }

            //bool isDown = UnityEngine.Input.GetKey(UnityEngine.KeyCode.F7);
            //if (isDown && !_f7Pressed)
            //{
            //    _f7Pressed = true;
            //    string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            //    //MelonLogger.Msg($"[TranslatedTextures] F7 forced reload: {sceneName}");
            //    StaticReplaceTextures(sceneName);
            //    ForceReplaceDecals(true);
            //}
            //else if (!isDown) _f7Pressed = false;
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu" || sceneName == "MainMenu_DLC01" ||
                sceneName == "Boot" || sceneName == "Empty") return;

            _decalMaterialCache.Clear();

            MelonCoroutines.Start(DelayedSceneInit(sceneName));
        }

        private static System.Collections.IEnumerator DelayedSceneInit(string sceneName)
        {

            yield return null;

            StaticReplaceTextures(sceneName);
            ScheduleReplaceAfterDelay(sceneName);
        }

        internal static void StaticReplaceTextures(string sceneName)
        {
            int totalReplaced = 0;

            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                string objName = NormalizeName(renderer.gameObject.name);

                foreach (var rule in Rules)
                {
                    bool nameMatch = rule.ExactName
                        ? objName == rule.TargetName
                        : objName.Contains(rule.TargetName);

                    if (!nameMatch) continue;

                    if (rule.ParentName != null)
                    {
                        Transform parent = renderer.transform.parent;
                        bool parentFound = false;
                        while (parent != null)
                        {
                            if (parent.name == rule.ParentName) { parentFound = true; break; }
                            parent = parent.parent;
                        }
                        if (!parentFound) continue;
                    }

                    Texture2D newTex = GetOrLoadTexture(rule.TextureFile);
                    if (newTex == null) continue;

                    Material[] mats = renderer.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;

                        if (rule.SourceTextureName != null)
                        {
                            string matName = mats[i].name;
                            Texture currentTex = mats[i].mainTexture;
                            string texName = currentTex != null ? currentTex.name : "";

                            if (!matName.Contains(rule.SourceTextureName) &&
                                !texName.Contains(rule.SourceTextureName)) continue;
                        }

                        mats[i].mainTexture = newTex;
                        changed = true;

                        if (rule.DamageTextureFile != null)
                        {
                            Texture2D dmgTex = GetOrLoadTexture(rule.DamageTextureFile);    
                            if (dmgTex != null)
                                mats[i].SetTexture(711, dmgTex);
                        }
                    }

                    if (changed) totalReplaced++;
                }
            }

            foreach (Renderer renderer in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                if (renderer is MeshRenderer) continue;
                if (renderer == null) continue;

                string objName = renderer.gameObject.name;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                foreach (var rule in Rules)
                {
                    bool nameMatch = rule.ExactName
                        ? objName == rule.TargetName
                        : objName.Contains(rule.TargetName);

                    if (!nameMatch) continue;

                    bool changed = false;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;

                        string matName = mats[i].name ?? "";
                        Texture currentTex = mats[i].mainTexture;
                        string texName = currentTex != null ? currentTex.name : "";

                        if (rule.SourceTextureName != null &&
                            !matName.Contains(rule.SourceTextureName) &&
                            !texName.Contains(rule.SourceTextureName)) continue;

                        Texture2D newTex = GetOrLoadTexture(rule.TextureFile);
                        if (newTex == null) continue;

                        mats[i].mainTexture = newTex;



                        changed = true;
                    }

                    if (changed)
                        renderer.sharedMaterials = mats;
                }
            }

            ForceReplaceDecals(true);

            //MelonLogger.Msg($"[TranslatedTextures] StaticReplace done: {totalReplaced} replaced");
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.GearItem), "Awake")]
    internal static class GearItem_Awake_Patch
    {
        static void Postfix(Il2Cpp.GearItem __instance)
        {
            if (__instance == null) return;

            Main.ApplyTexturesToObject(__instance.gameObject);

            MelonCoroutines.Start(DelayedApply(__instance.gameObject));
        }

        private static System.Collections.IEnumerator DelayedApply(GameObject go)
        {
            yield return null;
            if (go != null)
                Main.ApplyTexturesToObject(go);

            yield return new WaitForSeconds(0.2f);
            if (go != null)
                Main.ApplyTexturesToObject(go);
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.UITexture), nameof(Il2Cpp.UITexture.mainTexture), MethodType.Setter)]
    internal static class UITexture_SetMainTexture_Patch
    {
        static void Prefix(Il2Cpp.UITexture __instance, ref Texture value)
        {
            if (value == null) return;

            string texName = value.name;
            if (!Main.UITextureMap.TryGetValue(texName, out string fileKey)) return;

            Texture2D replacement = Main.GetOrLoadTexture(fileKey);
            if (replacement == null) return;

            value = replacement;
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Panel_Inventory), "OnDrop")]
    internal static class Panel_Inventory_OnDrop_Patch
    {
        static void Prefix()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Main.ScheduleReplaceAfterDelay(sceneName);
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Panel_Inventory), "Enable")]
    internal static class Panel_Inventory_Enable_Patch
    {
        static void Postfix()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Main.ScheduleReplaceAfterDelay(sceneName);
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Panel_Container), "Enable")]
    [HarmonyPriority(Priority.Low)]
    internal static class Panel_Container_Enable_Patch
    {
        static void Postfix()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Main.ScheduleReplaceAfterDelay(sceneName);
        }
    }
}