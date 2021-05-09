﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Visual.Sound;
using System.Collections.Generic;

namespace VisualAdjustments
{
    class DollResourcesManager
    {
        static private SortedList<string, EquipmentEntityLink> head = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> hair = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> beard = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> eyebrows = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> skin = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> tails = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> horns = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, EquipmentEntityLink> classOutfits = new SortedList<string, EquipmentEntityLink>();
        static private SortedList<string, BlueprintPortrait> portraits = new SortedList<string, BlueprintPortrait>();
        static private SortedList<string, BlueprintUnitAsksList> asks = new SortedList<string, BlueprintUnitAsksList>();
        static private Dictionary<string, DollState> characterDolls = new Dictionary<string, DollState>();
        static private List<string> customPortraits = new List<string>();
        static public List<string> CustomPortraits
        {
            get
            {
                if (!loaded) Init();
                return customPortraits;
            }
        }
        static public SortedList<string, BlueprintPortrait> Portrait
        {
            get
            {
                if (!loaded) Init();
                return portraits;
            }
        }
        static public SortedList<string, BlueprintUnitAsksList> Asks
        {
            get
            {
                if(!loaded) Init();
                return asks;
            }
        }
        static public SortedList<string, EquipmentEntityLink> ClassOutfits
        {
            get
            {
                if (!loaded) Init();
                return classOutfits;
            }
        }
        static private bool loaded = false;
        static private void AddLinks(SortedList<string, EquipmentEntityLink> dict, EquipmentEntityLink[] links)
        {
            foreach (var eel in links)
            {
                dict[eel.AssetId] = eel;
            }
        }
        static private void Init()
        {
            var races = BluePrintThing.GetBlueprints<BlueprintRace>();
            var racePresets = BluePrintThing.GetBlueprints<BlueprintRaceVisualPreset>();
            var classes = BluePrintThing.GetBlueprints<BlueprintCharacterClass>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                    AddLinks(head, customizationOptions.Heads);
                    AddLinks(hair, customizationOptions.Hair);
                    AddLinks(beard, customizationOptions.Beards);
                    AddLinks(eyebrows, customizationOptions.Eyebrows);
                    AddLinks(tails, customizationOptions.TailSkinColors);
                    AddLinks(horns, customizationOptions.Horns);
                }
            }
            foreach (var racePreset in racePresets)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    var raceSkin = racePreset.Skin;
                    if (raceSkin == null) continue;
                    AddLinks(skin, raceSkin.GetLinks(gender, racePreset.RaceId));
                }
            }
            foreach (var _class in classes)
            {
                foreach (var race in races)
                {
                    foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                    {
                        AddLinks(classOutfits, _class.GetClothesLinks(gender, race.RaceId).ToArray());
                    }
                }
            }
#pragma warning disable CS0103 // The name 'BluePrintThing' does not exist in the current context
            foreach(var bp in BluePrintThing.GetBlueprints<BlueprintPortrait>())
#pragma warning restore CS0103 // The name 'BluePrintThing' does not exist in the current context
            {
                //Note there are two wolf portraits
                if (bp == BlueprintRoot.Instance.CharGen.CustomPortrait || bp.Data.IsCustom)
                {
                    continue;
                }
                if (!portraits.ContainsKey(bp.name)) portraits.Add(bp.name, bp);
            }
            customPortraits.AddRange(CustomPortraitsManager.Instance.GetExistingCustomPortraitIds());
#pragma warning disable CS0103 // The name 'BluePrintThing' does not exist in the current context
            foreach (var bp in BluePrintThing.GetBlueprints<BlueprintUnitAsksList>())
#pragma warning restore CS0103 // The name 'BluePrintThing' does not exist in the current context
            {
                var component = bp.GetComponent<UnitAsksComponent>();
                if (component == null) continue;
                if (!component.Selected.HasBarks && bp.name != "PC_None_Barks") continue;
                asks.Add(bp.name, bp);
            }
            loaded = true;
        }
        static public DollState GetDoll(UnitEntityData unitEntityData)
        {
            if (!loaded) Init();
            if (unitEntityData.Descriptor.Doll == null) return null;
            if (!characterDolls.ContainsKey(unitEntityData.CharacterName))
            {
                characterDolls[unitEntityData.CharacterName] = CreateDollState(unitEntityData);
            }
            return characterDolls[unitEntityData.CharacterName];
        }
        static public string GetType(string assetID)
        {
            if (!loaded) Init();
            if (head.ContainsKey(assetID)) return "Head";
            if (hair.ContainsKey(assetID)) return "Hair";
            if (beard.ContainsKey(assetID)) return "Beard";
            if (eyebrows.ContainsKey(assetID)) return "Eyebrows";
            if (skin.ContainsKey(assetID)) return "Skin";
            if (horns.ContainsKey(assetID)) return "Horn";
            if (tails.ContainsKey(assetID)) return "Tail";
            if (classOutfits.ContainsKey(assetID)) return "ClassOutfit";
            return "Unknown";
        }
        static private DollState CreateDollState(UnitEntityData unitEntityData)
        {
            var dollState = new DollState();
            var dollData = unitEntityData.Descriptor.Doll;
            dollState.SetRace(unitEntityData.Descriptor.Progression.Race); //Race must be set before class
            //This is a hack to work around harmony not allowing calls to the unpatched method
            CharacterManager.disableEquipmentClassPatch = true; 
            dollState.SetClass(unitEntityData.Descriptor.Progression.GetEquipmentClass());
            CharacterManager.disableEquipmentClassPatch = false;
            dollState.SetGender(dollData.Gender);
            dollState.SetRacePreset(dollData.RacePreset);
            unitEntityData.Descriptor.LeftHandedOverride = true;

            dollState.SetEquipColors(dollData.ClothesPrimaryIndex, dollData.ClothesSecondaryIndex);
            foreach(var assetID in dollData.EquipmentEntityIds)
            {
                if (head.ContainsKey(assetID))
                {
                    dollState.SetHead(head[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetSkinColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (hair.ContainsKey(assetID))
                {
                    dollState.SetHair(hair[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetHairColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (beard.ContainsKey(assetID))
                {
                    dollState.SetBeard(beard[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetHairColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (skin.ContainsKey(assetID))
                {
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetSkinColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (horns.ContainsKey(assetID))
                {
                    dollState.SetHorn(horns[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetHornsColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (skin.ContainsKey(assetID))
                {
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetSkinColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (classOutfits.ContainsKey(assetID))
                {
                    if (dollData.EntityRampIdices.ContainsKey(assetID) &&
                        dollData.EntitySecondaryRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetEquipColors(dollData.EntityRampIdices[assetID], dollData.EntitySecondaryRampIdices[assetID]);
                    }
                }
            }
            dollState.Validate();
            return dollState;
        }
    }

}
