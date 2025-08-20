using System.Collections;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using nadena.dev.resonity.remote.puppeteer.rpc;

using F = FrooxEngine;
using P = nadena.dev.ndmf.proto;

namespace nadena.dev.resonity.remote.puppeteer.filters;

public class AvatarExpressionFilter(TranslateContext ctx)
{
    public async Task ApplyEyeLinearDriver(Slot slot, P.AvatarDescriptor spec)
    {
        var vc = spec.VisemeConfig;
        var targetMesh = ctx.Object<F.SkinnedMeshRenderer>(spec.VisemeConfig.VisemeMesh);

        var blendshapeIndices = new Dictionary<string, int>();
        if (targetMesh == null) return;
        
        if (await ctx.WaitForAssetLoad(targetMesh.Mesh.Target) == null)
        {
            throw new Exception("Failed to load viseme mesh: " + targetMesh.Mesh.Target);
        }
        
        foreach (var (bs, i) in targetMesh.Mesh.Asset.Data.BlendShapes.Select((bs, i) => (bs.Name, i)))
        {
            if (bs != null) blendshapeIndices[bs] = i;
        }
        
        var isProbablyUnifiedExpressions = ItCouldBeUnifiedExpressions(blendshapeIndices);
        var isProbablyARKit = ItCouldBeARKit(blendshapeIndices);
        if (!isProbablyUnifiedExpressions && !isProbablyARKit) return;
        
        var linearDriver = slot.GetComponentInChildren<EyeLinearDriver>();
        if (linearDriver.Eyes.Count == 0)
        {
            var left = linearDriver.Eyes.Add();
            var right = linearDriver.Eyes.Add();
            
            left.Side.Value = EyeSide.Left;
            right.Side.Value = EyeSide.Right;
        }
        
        foreach (var eye in linearDriver.Eyes)
        {
            var isLeftSide = eye.Side.Value == EyeSide.Left;
            TryGetFieldAndThen(UnifiedExpressionsBasics.EyeClosed, isLeftSide, field => eye.OpenCloseTarget.ForceLink(field));
            TryGetFieldAndThen(UnifiedExpressionsBasics.EyeWide, isLeftSide, field => eye.WidenTarget.ForceLink(field));
            TryGetFieldAndThen(UnifiedExpressionsBasics.EyeSquint, isLeftSide, field => eye.SqueezeTarget.ForceLink(field)); // This is not a 1:1
            TryGetFieldAndThen(UnifiedExpressionsBasics.BrowOuterUp, isLeftSide, field => eye.FrownTarget.ForceLink(field));
            TryGetFieldAndThen(UnifiedExpressionsBasics.EyeLookUp, isLeftSide, field => eye.LookUp.ForceLink(field));
            TryGetFieldAndThen(UnifiedExpressionsBasics.EyeLookDown, isLeftSide, field => eye.LookDown.ForceLink(field));
            // TODO: LookLeft need to be bound to EyeLookOutLeft and EyeLookInRight 
            // TODO: LookRight need to be bound to EyeLookInLeft and EyeLookOutRight
        }
        
        void TryGetFieldAndThen(ConventionElement element, bool isLeftSide, Action<Sync<float>> callbackFn)
        {
            var shapeName = isLeftSide ? element.Left : element.Right;
            
            if (shapeName is nameof(Ignore) or nameof(Todo) or nameof(Unavailable)) return;

            if (!blendshapeIndices.TryGetValue(shapeName, out var index)) return;
            if (targetMesh.BlendShapeWeights.Count <= index) return;

            var destinationField = targetMesh.BlendShapeWeights.GetElement(index);
            callbackFn(destinationField);
        }
    }
    
    public async Task ApplyAvatarExpressionDriver(Slot slot, P.AvatarDescriptor spec)
    {
        var vc = spec.VisemeConfig;
        var targetMesh = ctx.Object<F.SkinnedMeshRenderer>(spec.VisemeConfig.VisemeMesh);

        var blendshapeIndices = new Dictionary<string, int>();
        if (targetMesh == null) return;
        
        if (await ctx.WaitForAssetLoad(targetMesh.Mesh.Target) == null)
        {
            throw new Exception("Failed to load viseme mesh: " + targetMesh.Mesh.Target);
        }
        
        foreach (var (bs, i) in targetMesh.Mesh.Asset.Data.BlendShapes.Select((bs, i) => (bs.Name, i)))
        {
            if (bs != null) blendshapeIndices[bs] = i;
        }

        var isProbablyUnifiedExpressions = ItCouldBeUnifiedExpressions(blendshapeIndices);
        var isProbablyARKit = ItCouldBeARKit(blendshapeIndices);
        if (!isProbablyUnifiedExpressions && isProbablyARKit) return;
        
        var associations = isProbablyUnifiedExpressions ? BuildUnifiedExpressionsBinding() : BuildArKitBinding();

        var prevDriver = slot.GetComponentInChildren<AvatarExpressionDriver>();
        prevDriver?.Destroy();
        
        var driver = targetMesh.Slot.AttachComponent<AvatarExpressionDriver>();

        foreach (var association in associations)
        {
            TryCreateExpressionDriverFor(association.Expression, association.BlendShape);
        }

        bool TryCreateExpressionDriverFor(AvatarExpression expression, string shapeName)
        {
            return TryCreateExpressionDriverAndThen(expression, shapeName, _ => { });
        }

        bool TryCreateExpressionDriverAndThen(AvatarExpression expression, string shapeName, Action<AvatarExpressionDriver.ExpressionDriver> callbackFn)
        {
            if (shapeName is nameof(Ignore) or nameof(Todo) or nameof(Unavailable)) return false;
            
            if (!blendshapeIndices.TryGetValue(shapeName, out var index)) return false;
            if (targetMesh.BlendShapeWeights.Count <= index) return false;
            
            var destinationField = targetMesh.BlendShapeWeights.GetElement(index);
            
            var expressionDriver = driver.ExpressionDrivers.Add();
            expressionDriver.Target.ForceLink(destinationField);
            expressionDriver.Expression.Value = expression;
            expressionDriver.Min.Value = 0f;
            expressionDriver.Max.Value = 1f;
            expressionDriver.VolumeSupressionStrength.Value = 0f;
            
            callbackFn(expressionDriver);
            return true;
        }
    }

    private List<Binding> BuildUnifiedExpressionsBinding()
    {
        return new List<Binding>
        {
            // Eyebrow Drivers
            new(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.BrowDown.Left),
            new(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.BrowInnerUp.Left),
            new(AvatarExpression.FrownRight, UnifiedExpressionsBasics.BrowDown.Right),
            new(AvatarExpression.FrownRight, UnifiedExpressionsBasics.BrowInnerUp.Right),

            // Mouth Drivers
            new(AvatarExpression.Smile, Ignore),
            new(AvatarExpression.SmileLeft, UnifiedExpressionsBasics.MouthSmile.Left),
            new(AvatarExpression.SmileRight, UnifiedExpressionsBasics.MouthSmile.Right),
            new(AvatarExpression.SmirkLeft, UnifiedExpressionsBasics.Mouth.Left),
            new(AvatarExpression.SmirkRight, UnifiedExpressionsBasics.Mouth.Right),
            new(AvatarExpression.Frown, Ignore),
            new(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.MouthFrown.Left),
            new(AvatarExpression.FrownRight, UnifiedExpressionsBasics.MouthFrown.Right),
            new(AvatarExpression.MouthDimple, Ignore),
            new(AvatarExpression.MouthDimpleLeft, UnifiedExpressionsBasics.MouthDimple.Left),
            new(AvatarExpression.MouthDimpleRight, UnifiedExpressionsBasics.MouthDimple.Right),
            new(AvatarExpression.TongueOut, UnifiedExpressionsBasics.TongueOut.Center),
            new(AvatarExpression.TongueRaise, Ignore),
            new(AvatarExpression.TongueExtend, Ignore),
            new(AvatarExpression.TongueLeft, UnifiedExpressionsBasics.TongueLeft.Center),
            new(AvatarExpression.TongueRight, UnifiedExpressionsBasics.TongueRight.Center),
            new(AvatarExpression.TongueDown, UnifiedExpressionsBasics.TongueDown.Center),
            new(AvatarExpression.TongueUp, UnifiedExpressionsBasics.TongueUp.Center),

            new(AvatarExpression.TongueRoll, Unavailable),
            new(AvatarExpression.TongueHorizontal, Unavailable),
            new(AvatarExpression.TongueVertical, Unavailable),
            new(AvatarExpression.TongueUpLeft, Unavailable),
            new(AvatarExpression.TongueUpRight, Unavailable),
            new(AvatarExpression.TongueDownLeft, Unavailable),
            new(AvatarExpression.TongueDownRight, Unavailable),

            new(AvatarExpression.SmileClosed, Todo), // TODO: What is this?
            new(AvatarExpression.SmileClosedLeft, Todo), // TODO: What is this?
            new(AvatarExpression.SmileClosedRight, Todo), // TODO: What is this?
            new(AvatarExpression.Grin, Ignore),
            new(AvatarExpression.GrinLeft, UnifiedExpressionsBasics.LipPucker.Left),
            new(AvatarExpression.GrinRight, UnifiedExpressionsBasics.LipPucker.Right),
            new(AvatarExpression.Angry, Todo),
            new(AvatarExpression.CheekPuffLeft, UnifiedExpressionsBasics.CheekPuff.Left),
            new(AvatarExpression.CheekPuffRight, UnifiedExpressionsBasics.CheekPuff.Right),
            new(AvatarExpression.CheekPuff, Ignore),
            new(AvatarExpression.CheekSuckLeft, UnifiedExpressionsBasics.CheekSuck.Left),
            new(AvatarExpression.CheekSuckRight, UnifiedExpressionsBasics.CheekSuck.Right),
            new(AvatarExpression.CheekSuck, Ignore),
            new(AvatarExpression.CheekRaiseLeft, UnifiedExpressionsBasics.CheekSquint.Left),
            new(AvatarExpression.CheekRaiseRight, UnifiedExpressionsBasics.CheekSquint.Right),
            new(AvatarExpression.CheekRaise, Ignore),
            new(AvatarExpression.LipRaiseUpperLeft, UnifiedExpressionsBasics.MouthRaiserUpper.Left),
            new(AvatarExpression.LipRaiseUpperRight, UnifiedExpressionsBasics.MouthRaiserUpper.Right),
            new(AvatarExpression.LipRaiseLowerLeft, UnifiedExpressionsBasics.MouthRaiserLower.Left),
            new(AvatarExpression.LipRaiseLowerRight, UnifiedExpressionsBasics.MouthRaiserLower.Right),

            new(AvatarExpression.LipRaiseUpper, UnifiedExpressionsBasics.MouthUpper.Left),
            new(AvatarExpression.LipRaiseUpper, UnifiedExpressionsBasics.MouthUpper.Right),
            new(AvatarExpression.LipRaiseLower, UnifiedExpressionsBasics.MouthLower.Left),
            new(AvatarExpression.LipRaiseLower, UnifiedExpressionsBasics.MouthLower.Right),

            new(AvatarExpression.LipMoveLeftUpper, Todo),
            new(AvatarExpression.LipMoveRightUpper, Todo),
            new(AvatarExpression.LipMoveLeftLower, Todo),
            new(AvatarExpression.LipMoveRightLower, Todo),
            new(AvatarExpression.LipMoveHorizontalUpper, Todo),
            new(AvatarExpression.LipMoveHorizontalLower, Todo),
            new(AvatarExpression.LipTopLeftOverturn, Todo),
            new(AvatarExpression.LipTopRightOverturn, Todo),
            new(AvatarExpression.LipTopOverturn, Todo),
            new(AvatarExpression.LipBottomLeftOverturn, Todo),
            new(AvatarExpression.LipBottomRightOverturn, Todo),
            new(AvatarExpression.LipBottomOverturn, Todo),
            new(AvatarExpression.LipOverlayUpper, Todo),
            new(AvatarExpression.LipOverlayUpperLeft, Todo),
            new(AvatarExpression.LipOverlayUpperRight, Todo),
            new(AvatarExpression.LipUnderlayUpper, Todo),
            new(AvatarExpression.LipUnderlayUpperLeft, Todo),
            new(AvatarExpression.LipUnderlayUpperRight, Todo),
            new(AvatarExpression.LipOverlayLower, Todo),
            new(AvatarExpression.LipOverlayLowerLeft, Todo),
            new(AvatarExpression.LipOverlayLowerRight, Todo),
            new(AvatarExpression.LipUnderlayLower, Todo),
            new(AvatarExpression.LipUnderlayLowerLeft, Todo),
            new(AvatarExpression.LipUnderlayLowerRight, Todo),
            new(AvatarExpression.LipStretch, Ignore),
            new(AvatarExpression.LipStretchLeft, UnifiedExpressionsBasics.MouthStretch.Left),
            new(AvatarExpression.LipStretchRight, UnifiedExpressionsBasics.MouthStretch.Right),
            new(AvatarExpression.LipTighten, Ignore),
            new(AvatarExpression.LipTightenLeft, UnifiedExpressionsBasics.MouthTightener.Left),
            new(AvatarExpression.LipTightenRight, UnifiedExpressionsBasics.MouthTightener.Right),
            new(AvatarExpression.LipsPress, Ignore),
            new(AvatarExpression.LipsPressLeft, UnifiedExpressionsBasics.MouthPress.Left),
            new(AvatarExpression.LipsPressRight, UnifiedExpressionsBasics.MouthPress.Right),
            new(AvatarExpression.JawLeft, UnifiedExpressionsBasics.Jaw.Left),
            new(AvatarExpression.JawRight, UnifiedExpressionsBasics.Jaw.Right),
            new(AvatarExpression.JawHorizontal, Todo),
            new(AvatarExpression.JawForward, UnifiedExpressionsBasics.JawForward.Center),

            // list.Add(new BS(AvatarExpression.JawDown, UnifiedExpressionsBasics.MouthClosed.Center); // TODO: This is incorrect, it may be a compound blendshape)
            new(AvatarExpression.JawDown, Todo),

            new(AvatarExpression.JawOpen, UnifiedExpressionsBasics.JawOpen.Center),
            new(AvatarExpression.Pout, Ignore),

            new(AvatarExpression.PoutLeft, UnifiedExpressionsBasics.LipFunnelUpper.Left),
            new(AvatarExpression.PoutLeft, UnifiedExpressionsBasics.LipFunnelLower.Left),
            new(AvatarExpression.PoutRight, UnifiedExpressionsBasics.LipFunnelUpper.Right),
            new(AvatarExpression.PoutRight, UnifiedExpressionsBasics.LipFunnelLower.Right),

            new(AvatarExpression.NoseWrinkle, Todo),
            new(AvatarExpression.NoseWrinkleLeft, UnifiedExpressionsBasics.NoseSneer.Left),
            new(AvatarExpression.NoseWrinkleRight, UnifiedExpressionsBasics.NoseSneer.Right),
            new(AvatarExpression.ChinRaise, Todo),
            new(AvatarExpression.ChinRaiseBottom, Todo),
            new(AvatarExpression.ChinRaiseTop, Todo),
        };
    }

    private List<Binding> BuildArKitBinding()
    {
        // TODO
        return new List<Binding>();
    }

    private static bool ItCouldBeARKit(Dictionary<string, int> blendshapeIndices)
    {
        return blendshapeIndices.Select(pair => pair.Key.ToLowerInvariant()).Any(s => s == "mouthShrugLower".ToLowerInvariant());
    }

    private static bool ItCouldBeUnifiedExpressions(Dictionary<string, int> blendshapeIndices)
    {
        return blendshapeIndices.Select(pair => pair.Key.ToLowerInvariant()).Any(s => s == "MouthRaiserLower".ToLowerInvariant() || s == "MouthRaiserLowerLef".ToLowerInvariant());
    }

    private readonly struct Binding(AvatarExpression expression, string blendShape)
    {
        public AvatarExpression Expression { get; } = expression;
        public string BlendShape { get; } = blendShape;
    }

    // The following class is based on the Unified Expressions naming convention:
    // - https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes
    //
    // Some metadata may be incorrect, refer to the source on that link.
    internal static class UnifiedExpressionsBasics
    {
        public static readonly ConventionElement EyeLookUp = new(nameof(EyeLookUp), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeLookDown = new(nameof(EyeLookDown), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeLookIn = new(nameof(EyeLookIn), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeLookOut = new(nameof(EyeLookOut), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeClosed = new(nameof(EyeClosed), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeSquint = new(nameof(EyeSquint), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeWide = new(nameof(EyeWide), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeDilation = new(nameof(EyeDilation), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement EyeConstrict = new(nameof(EyeConstrict), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement BrowDown = new(nameof(BrowDown), ConventionElementKind.CenteredAndSided, true);
        public static readonly ConventionElement BrowInnerUp = new(nameof(BrowInnerUp), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement BrowOuterUp = new(nameof(BrowOuterUp), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement NoseSneer = new(nameof(NoseSneer), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement CheekSquint = new(nameof(CheekSquint), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement CheekPuff = new(nameof(CheekPuff), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement CheekSuck = new(nameof(CheekSuck), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement JawOpen = new(nameof(JawOpen), ConventionElementKind.Centered);
        public static readonly ConventionElement MouthClosed = new(nameof(MouthClosed), ConventionElementKind.Centered);
        public static readonly ConventionElement Jaw = new(nameof(Jaw), ConventionElementKind.Sided);
        public static readonly ConventionElement JawForward = new(nameof(JawForward), ConventionElementKind.Centered);
        public static readonly ConventionElement LipSuckUpper = new(nameof(LipSuckUpper), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement LipSuckLower = new(nameof(LipSuckLower), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement LipFunnel = new(nameof(LipFunnel), ConventionElementKind.Centered);
        public static readonly ConventionElement LipFunnelUpper = new(nameof(LipFunnelUpper), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement LipFunnelLower = new(nameof(LipFunnelLower), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement LipPucker = new(nameof(LipPucker), ConventionElementKind.CenteredAndSided, true);
        public static readonly ConventionElement MouthUpperUp = new(nameof(MouthUpperUp), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthLowerDown = new(nameof(MouthLowerDown), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement Mouth = new(nameof(Mouth), ConventionElementKind.Sided, true);
        public static readonly ConventionElement MouthUpper = new(nameof(MouthUpper), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthLower = new(nameof(MouthLower), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthFrown = new(nameof(MouthFrown), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthSmile = new(nameof(MouthSmile), ConventionElementKind.CenteredAndSided, true);
        public static readonly ConventionElement MouthStretch = new(nameof(MouthStretch), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthDimple = new(nameof(MouthDimple), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthRaiserUpper = new(nameof(MouthRaiserUpper), ConventionElementKind.Centered);
        public static readonly ConventionElement MouthRaiserLower = new(nameof(MouthRaiserLower), ConventionElementKind.Centered);
        public static readonly ConventionElement MouthPress = new(nameof(MouthPress), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement MouthTightener = new(nameof(MouthTightener), ConventionElementKind.CenteredAndSided);
        public static readonly ConventionElement TongueOut = new(nameof(TongueOut), ConventionElementKind.Centered);
        public static readonly ConventionElement TongueUp = new(nameof(TongueUp), ConventionElementKind.Centered);
        public static readonly ConventionElement TongueDown = new(nameof(TongueDown), ConventionElementKind.Centered);
        public static readonly ConventionElement TongueLeft = new(nameof(TongueLeft), ConventionElementKind.Centered);
        public static readonly ConventionElement TongueRight = new(nameof(TongueRight), ConventionElementKind.Centered);
    }

    internal class ConventionElement(string prefix, ConventionElementKind kind, bool isBlended = false)
    {
        public string Left => $"{prefix}Left";
        public string Right => $"{prefix}Right";
        public string Center => prefix;
    }

    internal enum ConventionElementKind
    {
        CenteredAndSided,
        Centered,
        Sided,
    }

    public static readonly string Ignore = nameof(Ignore);
    public static readonly string Todo = nameof(Todo);
    public static readonly string Unavailable = nameof(Unavailable);
}