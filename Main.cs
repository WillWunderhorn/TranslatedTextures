using MelonLoader;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

namespace TranslatedTextures
{
    public class Main : MelonMod
    {
        private class TextureRule
        {
            public string TextureFile { get; set; }
            public string TargetName { get; set; }
            public string ParentName { get; set; } = null;
            public string SourceTextureName { get; set; } = null;
            public bool ExactName { get; set; } = false;
        }

        private class CannedGearDef
        {
            public string TextureFile { get; set; }
            public string SourceTextureName { get; set; }
            public string ParentName { get; set; } = null;
            public string[] MeshNames { get; set; } = { "CannedFoodMesh", "CannedFoodMesh_Old" };
            public string LidSourceName { get; set; } = "GEAR_CanLid_Dif";
            public string LidTextureFile { get; set; } = "GEAR_CanLid_Dif.png";
        }
        private static readonly CannedGearDef[] CannedDefs = new[]
        {
            new CannedGearDef { TextureFile = "GEAR_TomatoSoupCan_Dif.png",      SourceTextureName = "GEAR_TomatoSoupCan_Dif" },
            new CannedGearDef { TextureFile = "GEAR_DogFood_Dif.png",            SourceTextureName = "GEAR_DogFood_Dif" },
            new CannedGearDef { TextureFile = "GEAR_FoodPorkAndBeans_Dif.png",   SourceTextureName = "GEAR_FoodPorkAndBeans_Dif",MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" } },
            new CannedGearDef { TextureFile = "GEAR_FoodCannedPeaches_Dif.png",  SourceTextureName = "GEAR_FoodCannedPeaches_Dif",MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" } },
            new CannedGearDef { TextureFile = "GEAR_CondensedMilk_Dif.png",      SourceTextureName = "GEAR_CondensedMilk_Mat",ParentName = "GEAR_CondensedMilk", LidSourceName = "GEAR_CanLid_Mat" },
            new CannedGearDef { TextureFile = "GEAR_FoodCannedPineapple_Dif.png",SourceTextureName = "GEAR_FoodCannedPineapple_Dif",MeshNames = new[] { "OBJ_CannedFood", "OBJ_CannedFood_Old" }},
            new CannedGearDef{TextureFile       = "GEAR_CannedSardines_Dif.png",SourceTextureName = "GEAR_CannedSardines_Dif",ParentName        = "GEAR_CannedSardines",},
        };

        private static readonly List<TextureRule> Rules = BuildRules();

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
                        ExactName = true,
                        ParentName = def.ParentName,
                        SourceTextureName = def.SourceTextureName,
                    });

                    if (def.LidTextureFile != null && def.LidSourceName != null)
                    {
                        rules.Add(new TextureRule
                        {
                            TextureFile = def.LidTextureFile,
                            TargetName = mesh,
                            ExactName = true,
                            ParentName = def.ParentName,
                            SourceTextureName = def.LidSourceName,
                        });
                    }
                }
            }

            rules.AddRange(new[]
            {
                new TextureRule { TextureFile = "OBJ_Sign_C.png",       TargetName = "OBJ_RoadSignI_LOD0",       ExactName = true },
                new TextureRule { TextureFile = "OBJ_Sign_C.png",       TargetName = "OBJ_RoadSignI_LOD1",       ExactName = true },

                new TextureRule { TextureFile = "GLB_MetalRusted_D.png", TargetName = "OBJ_SignDamEntrance_LOD0", ExactName = true },

                new TextureRule { TextureFile = "OBJ_SignDam_A.png",    TargetName = "OBJ_SignDamCarterB_LOD0",  ExactName = true },
                new TextureRule { TextureFile = "OBJ_SignDam_A.png",    TargetName = "OBJ_SignDamCarterB_LOD1",  ExactName = true },

                new TextureRule { TextureFile = "GEAR_CarBattery_Dif.png",  TargetName = "CarBattery_LOD0",          ExactName = true },
                new TextureRule { TextureFile = "OBJ_RailwayTruck.png",     TargetName = "OBJ_CarTruckDoorEXT_LOD0", ExactName = true },

                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png",    TargetName = "RifleAmmoBox_LOD0",    ExactName = true, SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png",    TargetName = "RifleAmmoBox_LOD1",    ExactName = true, SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png",    TargetName = "RifleAmmoBox_Old",     ExactName = true, SourceTextureName = "GEAR_RifleAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RifleAmmoBox_Dif.png",    TargetName = "GEAR_RifleAmmoBox",    ExactName = true, SourceTextureName = "GEAR_RifleAmmoBox_Dif" },

                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RevolverAmmoBox_LOD0", ExactName = true, SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RevolverAmmoBox_LOD1", ExactName = true, SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "RifleAmmoBox_Old",     ExactName = true, SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },
                new TextureRule { TextureFile = "GEAR_RevolverAmmoBox_Dif.png", TargetName = "GEAR_RevolverAmmoBox", ExactName = true, SourceTextureName = "GEAR_RevolverAmmoBox_Dif" },

                new TextureRule { TextureFile = "GEAR_Matches_Dif.png",              TargetName = "OBJ_WoodMatches_LOD0",        ExactName = true },
                new TextureRule { TextureFile = "GEAR_Matches_Dif.png",              TargetName = "OBJ_WoodMatches_LOD1",        ExactName = true },

                new TextureRule { TextureFile = "GEAR_AccelerantLighterFuel_Dif.png", TargetName = "Accelerant_LOD0",            ExactName = true },
                new TextureRule { TextureFile = "GEAR_AccelerantLighterFuel_Dif.png", TargetName = "Accelerant_LOD1",            ExactName = true },

                new TextureRule { TextureFile = "GEAR_AntibioticPillBottle_Dif.png", TargetName = "BottleAntibiotics_LOD0",     ExactName = true },
                new TextureRule { TextureFile = "GEAR_AntibioticPillBottle_Dif.png", TargetName = "BottleAntibiotics_LOD1",     ExactName = true },

                new TextureRule { TextureFile = "GEAR_HydrogenPeroxide_Dif.png",     TargetName = "BottleHydrogenPeroxide_LOD0", ExactName = true },
                new TextureRule { TextureFile = "GEAR_HydrogenPeroxide_Dif.png",     TargetName = "BottleHydrogenPeroxide_LOD1", ExactName = true },

                new TextureRule { TextureFile = "GEAR_Flare_Dif.png", TargetName = "UncappedFlareMesh", ExactName = true, SourceTextureName = "GEAR_Flare_Mat" },
                new TextureRule { TextureFile = "GEAR_Flare_Blue_Dif.png", TargetName = "UncappedFlareMesh", ExactName = true, SourceTextureName = "GEAR_Flare_Blue_Mat" },

                new TextureRule { TextureFile = "GEAR_SewingKit_Dif.png", TargetName = "SewingKitMesh",    ExactName = true },

                new TextureRule { TextureFile = "OBJ_SulfurBag_Dif.png", TargetName = "ScrapMetal_LOD0", ExactName = true, ParentName = "GEAR_DustingSulfur" },
                 
                new TextureRule { TextureFile = "GEAR_CoffeeTin_Dif.png",    TargetName = "OBJ_CoffeeTin_LOD0", ExactName = true, SourceTextureName = "GEAR_CoffeeTin_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTin_Dif.png",    TargetName = "OBJ_CoffeeTin_LOD1", ExactName = true, SourceTextureName = "GEAR_CoffeeTin_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTinLid_Dif.png", TargetName = "OBJ_CoffeeTin_LOD0", ExactName = true, SourceTextureName = "GEAR_CoffeeTinLid_Dif" },
                new TextureRule { TextureFile = "GEAR_CoffeeTinLid_Dif.png", TargetName = "OBJ_CoffeeTin_LOD1", ExactName = true, SourceTextureName = "GEAR_CoffeeTinLid_Dif" },

                new TextureRule { TextureFile = "GEAR_Film_Box_BW.png",     TargetName = "GEAR_FilmBoxBW",     ExactName = true },
                new TextureRule { TextureFile = "GEAR_Film_Box_Colour.png", TargetName = "GEAR_FilmBoxColour", ExactName = true },
                new TextureRule { TextureFile = "GEAR_Film_Box_Sepia.png",  TargetName = "GEAR_FilmBoxSepia",  ExactName = true },

                new TextureRule { TextureFile = "GEAR_GranolaBar_Dif.png",      TargetName = "GranolaBarMesh" },
                new TextureRule { TextureFile = "GEAR_FoodCannedCorn_Dif.png",  TargetName = "OBJ_CannedCornFood", ExactName = true, SourceTextureName = "GEAR_FoodCannedCorn_Dif" },

                new TextureRule { TextureFile = "GEAR_PeanutButter_Dif.png",  TargetName = "PeanutButter_LOD0", ExactName = true, SourceTextureName = "GEAR_PeanutButter_Dif" },

                new TextureRule { TextureFile = "GEAR_FoodSodaSummit_Dif.png",  TargetName = "FoodSodaCan_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodSodaSummit_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodSodaOrange_Dif.png",  TargetName = "FoodSodaCan_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodSodaOrange_Dif" },
                new TextureRule { TextureFile = "GEAR_FoodSodaGrape_Dif.png",  TargetName = "FoodSodaCan_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodSodaGrape_Dif" },

                new TextureRule { TextureFile = "GEAR_Oats.png",  TargetName = "OBJ_OatsTin_LOD0", ExactName = true, SourceTextureName = "GEAR_Oats" },

                new TextureRule { TextureFile = "GEAR_FoodCannedHam_Dif.png",  TargetName = "OBJ_CannedHamFood", ExactName = true},

                new TextureRule { TextureFile = "GEAR_DriedApples.png",  TargetName = "OBJ_DriedApples_LOD0", ExactName = true, SourceTextureName = "GEAR_DriedApples"},

                new TextureRule { TextureFile = "GEAR_KetchupChips_Dif.png",  TargetName = "BeefJerky_LOD0", ExactName = true, SourceTextureName = "GEAR_KetchupChips_Dif"},

                new TextureRule { TextureFile = "GEAR_SaltBag.png",  TargetName = "OBJ_SaltBag_LOD0", ExactName = true, SourceTextureName = "GEAR_SaltBag"},

                new TextureRule { TextureFile = "OBJ_HeatPad.png",  TargetName = "OBJ_HeatPad", ExactName = true, SourceTextureName = "OBJ_HeatPad"},

                new TextureRule { TextureFile = "GEAR_FoodMRE_Dif.png",  TargetName = "Obj_FoodMRE_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodMRE_Dif"},

                new TextureRule { TextureFile = "GEAR_FoodEnergyBar_Dif.png",  TargetName = "CandyBarMesh_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodEnergyBar_Dif"},

                new TextureRule { TextureFile = "GEAR_WaterPurificationTablets_Dif.png",  TargetName = "WaterPurificationTablets_LOD0", ExactName = true, SourceTextureName = "GEAR_WaterPurificationTablets_Dif"},

                new TextureRule { TextureFile = "CLTH_ACC_BallisticVest.png",  TargetName = "OBJ_BallisticVest_LOD0", ExactName = true, SourceTextureName = "CLTH_ACC_BallisticVest"},

                new TextureRule { TextureFile = "GEAR_FoodBeefJerky_Dif.png",  TargetName = "BeefJerky_LOD0", ExactName = true, SourceTextureName = "GEAR_FoodBeefJerky_Dif"},

                new TextureRule { TextureFile = "OBJ_Cash_Dif.png",  TargetName = "OBJ_CashBundle"},

                new TextureRule { TextureFile = "GEAR_LabelVitamins.png",  TargetName = "BottlePainKillers_LOD0"},

                new TextureRule { TextureFile = "GEAR_FoodCandyBars_Dif.png", TargetName = "CandyBarMesh", ExactName = true, SourceTextureName = "GEAR_FoodCandyBars_Mat" },

                new TextureRule { TextureFile = "OBJ_FoodEnergyDrink_A.png", TargetName = "OBJ_FoodEnergyDrink_A", ExactName = true, SourceTextureName = "OBJ_FoodEnergyDrink_A" },

                new TextureRule { TextureFile = "OBJ_CaffeineBox.png", TargetName = "OBJ_CaffeineBox", ExactName = true, SourceTextureName = "OBJ_CaffeineBox" },

                new TextureRule { TextureFile = "GEAR_GreenTeaPackage_Dif.png", TargetName = "OBJ_GreenTeaPackage_LOD0", ExactName = true, SourceTextureName = "GEAR_GreenTeaPackage_Dif" },

                new TextureRule { TextureFile = "GEAR_GunpowderCan_Dif.png", TargetName = "GunpowderCan_LOD0", ExactName = true, SourceTextureName = "GEAR_GunpowderCan_Dif" },

                new TextureRule { TextureFile = "GEAR_Cereal_A.png", TargetName = "OBJ_CerealBox_LOD0", ExactName = true, SourceTextureName = "GEAR_Cereal_A" },

                new TextureRule { TextureFile = "GEAR_BoxedCrackers_Dif.png", TargetName = "OBJ_BoxedCrackers_LOD0", ExactName = true, SourceTextureName = "GEAR_BoxedCrackers_Dif" },

                new TextureRule { TextureFile = "GEAR_FlourBag.png", TargetName = "OBJ_FlourBag_LOD0", ExactName = true, SourceTextureName = "GEAR_FlourBag" },

                new TextureRule { TextureFile = "GEAR_InsulatedFlask_G.png", TargetName = "OBJ_InsulatedFlask_LOD0", ExactName = true, SourceTextureName = "GEAR_InsulatedFlask_G" },

                new TextureRule { TextureFile = "GEAR_MapleSyrup_Dif.png", TargetName = "PeanutButter_LOD0", ExactName = true, SourceTextureName = "GEAR_MapleSyrup_Dif" },

                new TextureRule {TextureFile = "OBJ_PotassiumNitrate_Dif.png",TargetName = "ScrapMetal_LOD0",   ParentName = "GEAR_StumpRemover", ExactName = true},

                new TextureRule {TextureFile = "GEAR_LampFuel_Dif.png",TargetName = "LampFuel_LOD0" },

                new TextureRule {TextureFile = "GEAR_LampFuel_Dif.png",TargetName = "LampFuel" },

                new TextureRule { TextureFile = "OBJ_BookRevolver_A.png", TargetName = "Mesh", ExactName = true, SourceTextureName = "OBJ_BookRevolver_A" },

                new TextureRule { TextureFile = "OBJ_BookHardcover_Cooking.png", TargetName = "Mesh", ExactName = true, ParentName = "GEAR_BookCooking" },

                new TextureRule { TextureFile = "OBJ_BookSoftcover_Frontier.png", TargetName = "Mesh", ExactName = true, ParentName = "GEAR_BookRifleFirearm" },

                new TextureRule { TextureFile = "OBJ_BookSoftcover_IceFishing.png", TargetName = "Mesh", ExactName = true, ParentName = "GEAR_BookIceFishing" },

            });

            return rules;
        }

        internal static readonly Dictionary<string, string> UITextureMap = new Dictionary<string, string> 
        {
            { "ico_GearItem__PinnacleCanPeaches",     "ico_GearItem__PinnacleCanPeaches.png" }, 
            { "ico_GearItem__TomatoSoupCan",          "ico_GearItem__TomatoSoupCan.png" },
            { "ico_GearItem__WoodMatches",            "ico_GearItem__WoodMatches.png" },
            { "ico_GearItem__Accelerant",             "ico_GearItem__Accelerant.png" },
            { "ico_GearItem__BottleAntibiotics",      "ico_GearItem__BottleAntibiotics.png" },
            { "ico_GearItem__BottleHydrogenPeroxide", "ico_GearItem__BottleHydrogenPeroxide.png" },
            { "ico_GearItem__FlareA",                 "ico_GearItem__FlareA.png" },
            { "ico_GearItem__RifleAmmoBox",           "ico_GearItem__RifleAmmoBox.png" },
            { "ico_GearItem__SewingKit",              "ico_GearItem__SewingKit.png" },
            { "ico_GearItem__DustingSulfur",          "ico_GearItem__DustingSulfur.png" },
            { "ico_GearItem__CoffeeTin",              "ico_GearItem__CoffeeTin.png" },
            { "ico_GearItem__FilmBoxColour",          "ico_GearItem__FilmBoxColour.png" },
            { "ico_GearItem__FilmBoxBW",              "ico_GearItem__FilmBoxBW.png" },
            { "ico_GearItem__FilmBoxSepia",           "ico_GearItem__FilmBoxSepia.png" },
            { "ico_GearItem__GranolaBar",             "ico_GearItem__GranolaBar.png" },
            { "ico_GearItem__RevolverAmmoBox",        "ico_GearItem__RevolverAmmoBox.png" },
            { "ico_GearItem__DogFood",                "ico_GearItem__DogFood.png" },
            { "ico_GearItem__CannedCorn",             "ico_GearItem__CannedCorn.png" },
            { "ico_GearItem__CannedBeans",            "ico_GearItem__CannedBeans.png" },
            { "ico_GearItem__CondensedMilk",            "ico_GearItem__CondensedMilk.png" }, 
            { "ico_GearItem__CannedPineapple",            "ico_GearItem__CannedPineapple.png" }, 
            { "ico_GearItem__PeanutButter",            "ico_GearItem__PeanutButter.png" }, 
            { "ico_GearItem__Soda",            "ico_GearItem__Soda.png" }, 
            { "ico_GearItem__SodaOrange",            "ico_GearItem__SodaOrange.png" }, 
            { "ico_GearItem__SodaGrape",            "ico_GearItem__SodaGrape.png" }, 
            { "ico_GearItem__OatsTin",            "ico_GearItem__OatsTin.png" }, 
            { "ico_GearItem__CannedHam",            "ico_GearItem__CannedHam.png" }, 
            { "ico_GearItem__DriedApples",            "ico_GearItem__DriedApples.png" }, 
            { "ico_GearItem__KetchupChips",            "ico_GearItem__KetchupChips.png" }, 
            { "ico_GearItem__SaltBag",            "ico_GearItem__SaltBag.png" }, 
            { "ico_GearItem__BlueFlare",            "ico_GearItem__BlueFlare.png" }, 
            { "ico_GearItem__HeatPad",            "ico_GearItem__HeatPad.png" }, 
            { "ico_GearItem__MRE",            "ico_GearItem__MRE.png" }, 
            { "ico_GearItem__EnergyBar",            "ico_GearItem__EnergyBar.png" }, 
            { "ico_GearItem__CannedSardines",            "ico_GearItem__CannedSardines.png" }, 
            { "ico_GearItem__WaterPurificationTablets",            "ico_GearItem__WaterPurificationTablets.png" }, 
            { "ico_GearItem__BallisticVest",            "ico_GearItem__BallisticVest.png" }, 
            { "CLTH_ACC_CVR_BallisticVest",            "ico_GearItem__BallisticVest.png" }, 
            { "CLTH_ACC_BallisticVest",            "CLTH_ACC_BallisticVest1.png" }, 
            { "ico_GearItem__BeefJerky",            "ico_GearItem__BeefJerky.png" }, 
            { "ico_GearItem__CashBundle",            "ico_GearItem__CashBundle.png" }, 
            { "ico_GearItem__BottleVitaminC",            "ico_GearItem__BottleVitaminC.png" }, 
            { "ico_GearItem__CandyBar",            "ico_GearItem__CandyBar.png" }, 
            { "ico_GearItem__SodaEnergy",            "ico_GearItem__SodaEnergy.png" }, 
            { "ico_GearItem__BottleCaffeine",            "ico_GearItem__BottleCaffeine.png" }, 
            { "ico_GearItem__GreenTeaPackage",            "ico_GearItem__GreenTeaPackage.png" }, 
            { "ico_GearItem__GunpowderCan",            "ico_GearItem__GunpowderCan.png" }, 
            { "ico_GearItem__Cereal_A",            "ico_GearItem__Cereal_A.png" }, 
            { "ico_GearItem__Crackers",            "ico_GearItem__Crackers.png" }, 
            { "ico_GearItem__Flour",            "ico_GearItem__Flour.png" }, 
            { "ico_GearItem__InsulatedFlask_G",            "ico_GearItem__InsulatedFlask_G.png" },  
            { "ico_GearItem__MapleSyrup",            "ico_GearItem__MapleSyrup.png" },  
            { "ico_GearItem__StumpRemover",            "ico_GearItem__StumpRemover.png" },  
            { "ico_GearItem__LampFuel",            "ico_GearItem__LampFuel.png" },  
            { "ico_GearItem__LampFuelFull",            "ico_GearItem__LampFuel.png" },  
            { "ico_GearItem__BookRevolverFirearm",            "ico_GearItem__BookRevolverFirearm.png" },  
            { "ico_GearItem__BookCooking",            "ico_GearItem__BookCooking.png" },  
            { "ico_GearItem__BookRifleFirearm",            "ico_GearItem__BookRifleFirearm.png" },  
            { "ico_GearItem__BookIceFishing",            "ico_GearItem__BookIceFishing.png" },  
        };

        internal static Dictionary<string, Texture2D> LoadedTextures = new Dictionary<string, Texture2D>();

        private bool _f7Pressed = false;
        private static readonly List<float> _pendingTimers = new List<float>();
        private static string _pendingSceneName = null;

        public override void OnInitializeMelon()
        {
            LoadAllTextures();
        }

        private void LoadAllTextures()
        {
            LoggerInstance.Msg("[TranslatedTextures] === LOADING ===");

            foreach (var rule in Rules)
            {
                if (LoadedTextures.ContainsKey(rule.TextureFile)) continue;
                string resourcePath = $"TranslatedTextures.Resources.Textures.Russian.{rule.TextureFile}";
                Texture2D tex = LoadEmbeddedTexture(resourcePath, Path.GetFileNameWithoutExtension(rule.TextureFile));
                if (tex != null) LoadedTextures[rule.TextureFile] = tex;
            }

            foreach (var kv in UITextureMap)
            {
                if (LoadedTextures.ContainsKey(kv.Value)) continue;
                string resourcePath = $"TranslatedTextures.Resources.Textures.Russian.{kv.Value}";
                Texture2D tex = LoadEmbeddedTexture(resourcePath, Path.GetFileNameWithoutExtension(kv.Value));
                if (tex != null) LoadedTextures[kv.Value] = tex;
            }
        }

        private Texture2D LoadEmbeddedTexture(string resourceName, string textureName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    LoggerInstance.Error($"[TranslatedTextures] ❌ Not found: {resourceName}");
                    return null;
                }
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, bytes))
                {
                    tex.name = textureName;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    LoggerInstance.Msg($"[TranslatedTextures] ✓ Loaded: {textureName} ({tex.width}x{tex.height})");
                    return tex;
                }
                LoggerInstance.Error($"[TranslatedTextures] ❌ Unable to load: {textureName}");
                return null;
            }
        }

        internal static void ScheduleReplaceAfterDelay(string sceneName)
        {
            _pendingSceneName = sceneName;
            _pendingTimers.Clear();
            _pendingTimers.Add(0.3f);
            _pendingTimers.Add(0.8f);
            _pendingTimers.Add(2.0f);
            MelonLogger.Msg($"[TranslatedTextures] 3 attempts... {sceneName}");
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
                        MelonLogger.Msg($"[TranslatedTextures] Attempts ({_pendingTimers.Count} left)");
                        StaticReplaceTextures(_pendingSceneName);
                    }
                }
                if (_pendingTimers.Count == 0)
                    _pendingSceneName = null;
            }

            bool isDown = UnityEngine.Input.GetKey(UnityEngine.KeyCode.F7);
            if (isDown && !_f7Pressed)
            {
                _f7Pressed = true;
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                MelonLogger.Msg($"[TranslatedTextures] F7 — forced to reload textures {sceneName}");
                StaticReplaceTextures(sceneName);
            }
            else if (!isDown)
            {
                _f7Pressed = false;
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName.Contains("_") ||
                sceneName == "MainMenu" ||
                sceneName == "MainMenu_DLC01" ||
                sceneName == "Boot" ||
                sceneName == "Empty")
                return;

            StaticReplaceTextures(sceneName);
        }

        internal static void StaticReplaceTextures(string sceneName)
        {
            MelonLogger.Msg($"=== REPLACE TEXTURES: {sceneName} ===");
            int totalReplaced = 0;

            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (renderer == null) continue;
                string objName = renderer.gameObject.name;

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

                    if (!LoadedTextures.TryGetValue(rule.TextureFile, out Texture2D newTex)) continue;

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
                        MelonLogger.Msg($"REPLACED slot[{i}] on '{objName}'" +
                            $"{(rule.ParentName != null ? $" (parent: {rule.ParentName})" : "")}" +
                            $": mat='{mats[i].name}' -> '{rule.TextureFile}'");
                    }

                    if (changed) totalReplaced++;
                }
            }

            MelonLogger.Msg($"TOTAL REPLACED: {totalReplaced}");
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
            if (!Main.LoadedTextures.TryGetValue(fileKey, out Texture2D replacement)) return;
            MelonLogger.Msg($"[UI PATCH] replaced: '{texName}' -> '{fileKey}' on '{__instance.gameObject.name}'"); 
            value = replacement;
        }
    }

    [HarmonyPatch(typeof(Il2Cpp.Panel_Inventory), "OnDrop")]
    internal static class Panel_Inventory_OnDrop_Patch
    {
        static void Prefix()
        {
            MelonLogger.Msg("[TranslatedTextures] OnDrop detected!");
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Main.ScheduleReplaceAfterDelay(sceneName);
        }
    }
}