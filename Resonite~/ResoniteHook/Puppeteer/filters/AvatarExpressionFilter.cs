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
        }
        
        void TryGetFieldAndThen(ConventionElement element, bool isLeftSide, Action<Sync<float>> callbackFn)
        {
            var shapeName = isLeftSide ? element.Left : element.Right;
            
            if (shapeName is nameof(UnifiedExpressionsBasics.Ignore) or nameof(UnifiedExpressionsBasics.Todo)) return;

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

        var prevDriver = slot.GetComponentInChildren<AvatarExpressionDriver>();
        prevDriver?.Destroy();
        
        // TODO: Check which naming convention this avatar uses.
        var driver = targetMesh.Slot.AttachComponent<AvatarExpressionDriver>();
        {
            // Eyebrow Drivers
            if (TryCreateExpressionDriverFor(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.BrowDown.Left))
            {
                TryCreateExpressionDriverFor(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.BrowInnerUp.Left);
            }
            if (TryCreateExpressionDriverFor(AvatarExpression.FrownRight, UnifiedExpressionsBasics.BrowDown.Right))
            {
                TryCreateExpressionDriverFor(AvatarExpression.FrownRight, UnifiedExpressionsBasics.BrowInnerUp.Right);
            }
        }
        {
            // Mouth Drivers
            TryCreateExpressionDriverFor(AvatarExpression.Smile, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.SmileLeft, UnifiedExpressionsBasics.MouthSmile.Left);
            TryCreateExpressionDriverFor(AvatarExpression.SmileRight, UnifiedExpressionsBasics.MouthSmile.Right);
            TryCreateExpressionDriverFor(AvatarExpression.SmirkLeft, UnifiedExpressionsBasics.Mouth.Left);
            TryCreateExpressionDriverFor(AvatarExpression.SmirkRight, UnifiedExpressionsBasics.Mouth.Right);
            TryCreateExpressionDriverFor(AvatarExpression.Frown, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.FrownLeft, UnifiedExpressionsBasics.MouthFrown.Left);
            TryCreateExpressionDriverFor(AvatarExpression.FrownRight, UnifiedExpressionsBasics.MouthFrown.Right);
            TryCreateExpressionDriverFor(AvatarExpression.MouthDimple, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.MouthDimpleLeft, UnifiedExpressionsBasics.MouthDimple.Left);
            TryCreateExpressionDriverFor(AvatarExpression.MouthDimpleRight, UnifiedExpressionsBasics.MouthDimple.Right);
            TryCreateExpressionDriverFor(AvatarExpression.TongueOut, UnifiedExpressionsBasics.TongueOut.Center);
            TryCreateExpressionDriverFor(AvatarExpression.TongueRaise, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.TongueExtend, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.TongueLeft, UnifiedExpressionsBasics.TongueLeft.Center);
            TryCreateExpressionDriverFor(AvatarExpression.TongueRight, UnifiedExpressionsBasics.TongueRight.Center);
            TryCreateExpressionDriverFor(AvatarExpression.TongueDown, UnifiedExpressionsBasics.TongueDown.Center);
            TryCreateExpressionDriverFor(AvatarExpression.TongueUp, UnifiedExpressionsBasics.TongueUp.Center);
            TryCreateExpressionDriverFor(AvatarExpression.TongueRoll, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueHorizontal, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueVertical, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueUpLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueUpRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueDownLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.TongueDownRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.SmileClosed, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.SmileClosedLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.SmileClosedRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.Grin, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.GrinLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.GrinRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.Angry, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.CheekPuffLeft, UnifiedExpressionsBasics.CheekPuff.Left);
            TryCreateExpressionDriverFor(AvatarExpression.CheekPuffRight, UnifiedExpressionsBasics.CheekPuff.Right);
            TryCreateExpressionDriverFor(AvatarExpression.CheekPuff, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.CheekSuckLeft, UnifiedExpressionsBasics.CheekSuck.Left);
            TryCreateExpressionDriverFor(AvatarExpression.CheekSuckRight, UnifiedExpressionsBasics.CheekSuck.Right);
            TryCreateExpressionDriverFor(AvatarExpression.CheekSuck, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.CheekRaiseLeft, UnifiedExpressionsBasics.CheekSquint.Left);
            TryCreateExpressionDriverFor(AvatarExpression.CheekRaiseRight, UnifiedExpressionsBasics.CheekSquint.Right);
            TryCreateExpressionDriverFor(AvatarExpression.CheekRaise, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipRaiseUpperLeft, UnifiedExpressionsBasics.MouthRaiserUpper.Left);
            TryCreateExpressionDriverFor(AvatarExpression.LipRaiseUpperRight, UnifiedExpressionsBasics.MouthRaiserUpper.Right);
            TryCreateExpressionDriverFor(AvatarExpression.LipRaiseLowerLeft, UnifiedExpressionsBasics.MouthRaiserLower.Left);
            TryCreateExpressionDriverFor(AvatarExpression.LipRaiseLowerRight, UnifiedExpressionsBasics.MouthRaiserLower.Right);
            {
                TryCreateExpressionDriverFor(AvatarExpression.LipRaiseUpper, UnifiedExpressionsBasics.MouthUpper.Left);
                TryCreateExpressionDriverFor(AvatarExpression.LipRaiseUpper, UnifiedExpressionsBasics.MouthUpper.Right);
                TryCreateExpressionDriverFor(AvatarExpression.LipRaiseLower, UnifiedExpressionsBasics.MouthLower.Left);
                TryCreateExpressionDriverFor(AvatarExpression.LipRaiseLower, UnifiedExpressionsBasics.MouthLower.Right);
            }
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveLeftUpper, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveRightUpper, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveLeftLower, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveRightLower, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveHorizontalUpper, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipMoveHorizontalLower, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipTopLeftOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipTopRightOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipTopOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipBottomLeftOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipBottomRightOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipBottomOverturn, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayUpper, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayUpperLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayUpperRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayUpper, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayUpperLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayUpperRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayLower, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayLowerLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipOverlayLowerRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayLower, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayLowerLeft, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipUnderlayLowerRight, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.LipStretch, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.LipStretchLeft, UnifiedExpressionsBasics.MouthStretch.Left);
            TryCreateExpressionDriverFor(AvatarExpression.LipStretchRight, UnifiedExpressionsBasics.MouthStretch.Right);
            TryCreateExpressionDriverFor(AvatarExpression.LipTighten, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.LipTightenLeft, UnifiedExpressionsBasics.MouthTightener.Left);
            TryCreateExpressionDriverFor(AvatarExpression.LipTightenRight, UnifiedExpressionsBasics.MouthTightener.Right);
            TryCreateExpressionDriverFor(AvatarExpression.LipsPress, UnifiedExpressionsBasics.Ignore);
            TryCreateExpressionDriverFor(AvatarExpression.LipsPressLeft, UnifiedExpressionsBasics.MouthPress.Left);
            TryCreateExpressionDriverFor(AvatarExpression.LipsPressRight, UnifiedExpressionsBasics.MouthPress.Right);
            TryCreateExpressionDriverFor(AvatarExpression.JawLeft, UnifiedExpressionsBasics.Jaw.Left);
            TryCreateExpressionDriverFor(AvatarExpression.JawRight, UnifiedExpressionsBasics.Jaw.Right);
            TryCreateExpressionDriverFor(AvatarExpression.JawHorizontal, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.JawForward, UnifiedExpressionsBasics.JawForward.Center);
            {
                // TryCreateExpressionDriverFor(AvatarExpression.JawDown, UnifiedExpressionsBasics.MouthClosed.Center); // TODO: This is incorrect
                TryCreateExpressionDriverFor(AvatarExpression.JawDown, UnifiedExpressionsBasics.Todo);
            }
            TryCreateExpressionDriverFor(AvatarExpression.JawOpen, UnifiedExpressionsBasics.JawOpen.Center);
            TryCreateExpressionDriverFor(AvatarExpression.Pout, UnifiedExpressionsBasics.Ignore);
            {
                TryCreateExpressionDriverFor(AvatarExpression.PoutLeft, UnifiedExpressionsBasics.LipFunnelUpper.Left);
                TryCreateExpressionDriverFor(AvatarExpression.PoutLeft, UnifiedExpressionsBasics.LipFunnelLower.Left);
                TryCreateExpressionDriverFor(AvatarExpression.PoutRight, UnifiedExpressionsBasics.LipFunnelUpper.Right);
                TryCreateExpressionDriverFor(AvatarExpression.PoutRight, UnifiedExpressionsBasics.LipFunnelLower.Right);
            }
            TryCreateExpressionDriverFor(AvatarExpression.NoseWrinkle, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.NoseWrinkleLeft, UnifiedExpressionsBasics.NoseSneer.Left);
            TryCreateExpressionDriverFor(AvatarExpression.NoseWrinkleRight, UnifiedExpressionsBasics.NoseSneer.Right);
            TryCreateExpressionDriverFor(AvatarExpression.ChinRaise, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.ChinRaiseBottom, UnifiedExpressionsBasics.Todo);
            TryCreateExpressionDriverFor(AvatarExpression.ChinRaiseTop, UnifiedExpressionsBasics.Todo);
        }

        bool TryCreateExpressionDriverFor(AvatarExpression expression, string shapeName)
        {
            return TryCreateExpressionDriverAndThen(expression, shapeName, _ => { });
        }

        bool TryCreateExpressionDriverAndThen(AvatarExpression expression, string shapeName, Action<AvatarExpressionDriver.ExpressionDriver> callbackFn)
        {
            if (shapeName is nameof(UnifiedExpressionsBasics.Ignore) or nameof(UnifiedExpressionsBasics.Todo)) return false;
            
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
    
    // The following class is based on the Unified Expressions naming convention:
    // - https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes
    //
    // Some metadata may be incorrect, refer to the source on that link.
    internal static class UnifiedExpressionsBasics
    {
        public static readonly string Ignore = nameof(Ignore);
        public static readonly string Todo = nameof(Todo);
        
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
}