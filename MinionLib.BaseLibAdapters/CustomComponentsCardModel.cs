using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component;

namespace MinionLib.BaseLibAdapters;

internal abstract class CustomComponentsCardModel : ComponentsCardModel, ICustomModel, ILocalizationProvider
{
    /// <summary>
    /// For convenience; can be manually overridden if necessary.
    /// </summary>
    public override bool GainsBlock => DynamicVars.Any(dynVar => dynVar.Value is BlockVar or CalculatedBlockVar);

    public CustomComponentsCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true) : base(baseCost, type, rarity, target, showInCardLibrary)
    {
        if (autoAdd) CustomContentDictionary.AddModel(GetType());
    }

    /// <summary>
    /// Allows a custom texture to be used as the card's back frame.
    /// A new texture loaded through ResourceLoader.Load&lt;Texture2D> should be returned.
    /// </summary>
    public virtual Texture2D? CustomFrame => null;

    private bool _initializedFrameMaterial;
    private Material? _frameMaterial;

    private bool _initializedBannerMaterial;
    private Material? _bannerMaterial;

    /// <summary>
    /// Returns a custom ShaderMaterial defined by CreateCustomFrameMaterial.
    /// </summary>
    public Material? CustomFrameMaterial
    {
        get
        {
            if (!_initializedFrameMaterial)
            {
                _frameMaterial = CreateCustomFrameMaterial;
                _initializedFrameMaterial = true;
            }

            return _frameMaterial;
        }
    }
    /// <summary>
    /// Returns a custom ShaderMaterial defined by CreateCustomBannerMaterial.
    /// </summary>
    public Material? CustomBannerMaterial
    {
        get
        {
            if (!_initializedBannerMaterial)
            {
                _bannerMaterial = CreateCustomBannerMaterial;
                _initializedBannerMaterial = true;
            }

            return _bannerMaterial;
        }
    }

    /// <summary>
    /// Override this to use a custom ShaderMaterial only for this card.<seealso cref="BaseLib.Utils.ShaderUtils.GenerateHsv" />
    /// </summary>
    public virtual Material? CreateCustomFrameMaterial => null;

    /// <summary>
    /// Override this to use a custom ShaderMaterial for this card's banner.<seealso cref="BaseLib.Utils.ShaderUtils.GenerateHsv" />
    /// If using a basegame banner material override the path method instead.
    /// </summary>
    public virtual Material? CreateCustomBannerMaterial => null;
    /// <summary>
    /// See CardModel.BannerMaterialPath for basegame material paths
    /// </summary>
    public virtual string? CustomBannerMaterialPath => null;

    public virtual string? CustomPortraitPath => null;
    public virtual Texture2D? CustomPortrait => null;
    public virtual List<(string, string)>? Localization => null;


    //Utility methods
    /// <summary>
    /// Returns the received calculated var and the base and extra variables that it needs.
    /// Rather than use this method directly, you are suggested to use
    /// MakeCalculatedVar, MakeCalculatedDamage, or MakeCalculatedBlock.
    /// </summary>
    public static IEnumerable<DynamicVar> FinishMakeCalculatedVar(CalculatedVar var, int baseVal, int bonusVal)
    {
        switch (var)
        {
            case CustomCalculatedVar:
            case CustomCalculatedBlockVar:
                yield return new DynamicVar($"{var.Name}Base", baseVal);
                yield return new DynamicVar($"{var.Name}Extra", bonusVal);
                break;
            case CustomCalculatedDamageVar:
                yield return new DynamicVar($"{var.Name}Base", baseVal);
                yield return new CustomExtraDamageVar(var.Name, bonusVal);
                break;
            case CalculatedDamageVar:
                yield return new CalculationBaseVar(baseVal);
                yield return new ExtraDamageVar(bonusVal);
                break;
            default:
                yield return new CalculationBaseVar(baseVal);
                yield return new CalculationExtraVar(bonusVal);
                break;
        }

        yield return var;
    }

    /// <summary>
    /// Returns the 3 variables needed for a calculated var. Use this when defining your card's variables like so:
    /// <code>
    /// CanonicalVars => [
    ///     ..MakeCalculatedVar(5, (card, target) => bonus calc)
    /// ]
    /// </code>
    /// </summary>
    public static IEnumerable<DynamicVar> MakeCalculatedVar(string name, int baseVal,
        Func<CardModel, Creature?, decimal> bonus, int mult = 1)
    {
        return FinishMakeCalculatedVar(new CustomCalculatedVar(name).WithMultiplier(bonus), baseVal, mult);
    }

    /// <summary>
    /// Makes a CalculatedDamageVar with the default name and an accompanying base and extra var.
    /// <code>
    /// CanonicalVars => [
    ///     ..MakeCalculatedDamage(5, (card, target) => bonus calc)
    /// ]
    /// </code>
    /// </summary>
    /// <param name="baseVal">Base value</param>
    /// <param name="bonus">Calculation for bonus.</param>
    /// <param name="mult">Multiplier applied to result of calculation. Value of the "Extra" var.</param>
    /// <param name="props">Move props. Affects behavior of damage/calculation.</param>
    public static IEnumerable<DynamicVar> MakeCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus,
        int mult = 1, ValueProp props = ValueProp.Move)
    {
        return FinishMakeCalculatedVar(new CalculatedDamageVar(props).WithMultiplier(bonus), baseVal, mult);
    }

    /// <summary>
    /// Makes a CustomCalculatedDamageVar with a specified name and an accompanying base and extra var.
    /// <code>
    /// CanonicalVars => [
    ///     ..MakeCalculatedDamage("Special", 5, (card, target) => bonus calc)
    /// ]
    /// </code>
    /// </summary>
    /// <param name="name">The calculated var's name. The base var's name is the same with "Base" added, and the extra var has "Extra" added.</param>
    /// <param name="baseVal">Base value</param>
    /// <param name="bonus">Calculation for bonus.</param>
    /// <param name="mult">Multiplier applied to result of calculation. Value of the "Extra" var.</param>
    /// <param name="props">Move props. Affects behavior of damage/calculation.</param>
    public static IEnumerable<DynamicVar> MakeCalculatedDamage(string name, int baseVal,
        Func<CardModel, Creature?, decimal> bonus,
        int mult = 1, ValueProp props = ValueProp.Move)
    {
        return FinishMakeCalculatedVar(new CustomCalculatedDamageVar(name, props).WithMultiplier(bonus), baseVal, mult);
    }

    /// <summary>
    /// Makes a CalculatedBlockVar with the default name and an accompanying base and extra var.
    /// <code>
    /// CanonicalVars => [
    ///     ..MakeCalculatedBlock(5, (card, target) => bonus calc)
    /// ]
    /// </code>
    /// </summary>
    /// <param name="baseVal">Base value</param>
    /// <param name="bonus">Calculation for bonus.</param>
    /// <param name="mult">Multiplier applied to result of calculation. Value of the "Extra" var.</param>
    /// <param name="props">Move properties. Default value should almost always be correct for Block.</param>
    public static IEnumerable<DynamicVar> MakeCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus,
        int mult = 1, ValueProp props = ValueProp.Move)
    {
        return FinishMakeCalculatedVar(new CalculatedBlockVar(props).WithMultiplier(bonus), baseVal, mult);
    }

    /// <summary>
    /// Makes a CustomCalculatedBlockVar with a specified name and an accompanying base and extra var.
    /// <code>
    /// CanonicalVars => [
    ///     ..MakeCalculatedBlock("Special", 5, (card, target) => bonus calc)
    /// ]
    /// </code>
    /// </summary>
    /// <param name="name">The calculated var's name. The base var's name is the same with "Base" added, and the extra var has "Extra" added.</param>
    /// <param name="baseVal">Base value</param>
    /// <param name="bonus">Calculation for bonus.</param>
    /// <param name="mult">Multiplier applied to result of calculation. Value of the "Extra" var.</param>
    /// <param name="props">Move properties. Default value should almost always be correct for Block.</param>
    public static IEnumerable<DynamicVar> MakeCalculatedBlock(string name, int baseVal,
        Func<CardModel, Creature?, decimal> bonus,
        int mult = 1, ValueProp props = ValueProp.Move)
    {
        return FinishMakeCalculatedVar(new CustomCalculatedBlockVar(name, props).WithMultiplier(bonus), baseVal, mult);
    }
}
