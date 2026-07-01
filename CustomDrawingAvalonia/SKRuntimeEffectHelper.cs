using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace CustomDrawingAvalonia.Controls
{
    public class SKRuntimeEffectHelper
    {
        public static readonly AttachedProperty<string> SourceProperty =
         AvaloniaProperty.RegisterAttached<SKRuntimeEffectHelper, Control, string>("Source");
        public static string GetSource(Control obj) => obj.GetValue(SourceProperty);
        public static void SetSource(Control obj, string value) => obj.SetValue(SourceProperty, value);

        static SKRuntimeEffectHelper()
        {
            var dict = new Dictionary<Control, CompositionVisualHost>();
            SourceProperty.Changed.AddClassHandler<Control>((s, e) =>
            {
                if (dict.TryGetValue(s, out var host))
                {
                    host.Dispose();
                    dict.Remove(s);
                }
                if (e.NewValue is string code)
                {
                    dict[s] = new CompositionVisualHost(s, code);
                }
            });
        }

        public const string RippleSksl = @"
            uniform shader iContent;
            uniform float2 iResolution;
            uniform float iTime;
            uniform float3 iRipple0;  // (centerX, centerY, startTime)
            uniform float3 iRipple1;
            uniform float3 iRipple2;
            uniform float3 iRipple3;

            // 计算单个波纹的 UV 偏移（像素空间算距离，保证正圆）
            float2 calcRipple(float2 uv, float2 center, float age, float duration, float2 res) {
                if (age < 0.0 || age > duration) return float2(0.0);

                // 转到像素空间计算距离 → 正圆
                float2 diff = (uv - center) * res;
                float dist = length(diff) / length(res); // 归一化到对角线
                float radius = age * 0.3;  // 扩散速度
                float ringDist = abs(dist - radius);

                // 波纹强度：环形波前 + 时间衰减
                float strength = smoothstep(0.06, 0.0, ringDist) * (1.0 - age / duration);

                // UV 偏移方向：径向（像素空间角度）
                float angle = atan(diff.y, diff.x);
                float wave = sin(dist * 60.0 - age * 14.0) * 0.018 * strength;

                // 偏移回归一化空间
                return float2(cos(angle), sin(angle)) * wave / res * length(res);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / iResolution;
                float duration = 2.5;

                // 累加所有活跃波纹的偏移（波叠加 = 干涉）
                float2 offset = float2(0.0);
                offset += calcRipple(uv, iRipple0.xy, iTime - iRipple0.z, duration, iResolution);
                offset += calcRipple(uv, iRipple1.xy, iTime - iRipple1.z, duration, iResolution);
                offset += calcRipple(uv, iRipple2.xy, iTime - iRipple2.z, duration, iResolution);
                offset += calcRipple(uv, iRipple3.xy, iTime - iRipple3.z, duration, iResolution);

                float2 distortedCoord = (uv + offset) * iResolution;
                half4 col = iContent.eval(distortedCoord);

                // 波纹区域叠加淡淡高光
                float totalStrength = length(offset) * 40.0;
                col.rgb += totalStrength * 0.15;

                return col;
            }
        ";

        public const string sksl = @"
uniform shader source;
uniform float time;

half4 main(float2 coord) {
    half4 color = source.eval(coord);
    color.rgb *= 0.5 + 0.5 * sin(time);
    return color;
}
";
    }

    sealed class CompositionVisualHost : IDisposable
    {
        private readonly Control _target;
        private CompositionCustomVisual? _visual;
        private Handler? _handler;

        public CompositionVisualHost(Control control, string code)
        {
            _target = control;
            _target.SizeChanged += _target_SizeChanged;
            _target.PointerPressed += OnPointerPressed;

            var visual = ElementComposition.GetElementVisual(_target);
            if (visual == null)
            {
                return;
            }

            if (_visual?.Compositor == visual.Compositor)
            {
                return;
            }
            _visual = visual.Compositor.CreateCustomVisual(new RippleHandler(_target));
            _visual.Size = new Vector(Math.Max(1, _target.Bounds.Width), Math.Max(1, _target.Bounds.Height));
            ElementComposition.SetElementChildVisual(_target, _visual);
            _visual.SendHandlerMessage("start");
        }

        private void _target_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            _visual!.Size = new Vector(Math.Max(1, e.NewSize.Width), Math.Max(1, e.NewSize.Height));
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(_target);
            // 发送归一化坐标给 Handler
            var normalizedX = pos.X / _target.Bounds.Width;
            var normalizedY = pos.Y / _target.Bounds.Height;
            _visual?.SendHandlerMessage(new Point(normalizedX, normalizedY));
        }

        public void Dispose()
        {
            _target.SizeChanged -= _target_SizeChanged;
            _target.PointerPressed -= OnPointerPressed;
            ElementComposition.SetElementChildVisual(_target, null);
            _visual = null;
            _handler?.Dispose();
        }

        sealed class Handler : CompositionCustomVisualHandler, IDisposable
        {
            private SKRuntimeEffect? _effect;
            private bool _running;

            public Handler(string code)
            {
                _effect = SKRuntimeEffect.CreateShader(code, out var errors);
            }

            public override void OnRender(ImmediateDrawingContext context)
            {
                if (_effect == null)
                {
                    return;
                }

                var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (feature is null)
                    return;

                using var lease = feature.Lease();

                if (lease?.SkSurface == null)
                    return;

                using var snapshot = lease.SkSurface.Snapshot();

                var bounds = this.GetRenderBounds();
                float width = (float)bounds.Width;
                float height = (float)bounds.Height;
                var scaleX = width / snapshot.Width;
                var scaleY = height / snapshot.Height;

                //lease.SkCanvas.Clear();

                // 3. 设置 Uniforms
                using var uniforms = new SKRuntimeEffectUniforms(_effect)
                {
                    ["time"] = DateTimeOffset.Now.Millisecond / 1000f,
                };
                using var source = snapshot.ToShader(
                    SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                    SKMatrix.CreateScale(scaleX, scaleY));
                // 4. 设置子着色器 (输入图像)
                using var children = new SKRuntimeEffectChildren(_effect)
                {
                    ["source"] = source
                };

                // 5. 创建着色器
                using var shader = _effect.ToShader(uniforms, children);

                // 6. 用 Paint 应用到整个画布
                using var paint = new SKPaint { Shader = shader };
                lease.SkCanvas.DrawRect(new SKRect(0, 0, width, height), paint);

                Debug.WriteLine("{0:HH:mm:ss.fff} ({1}) ({2})", DateTimeOffset.Now, bounds.Size, snapshot.Info.Size);

            }

            public override void OnAnimationFrameUpdate()
            {
                if (!_running)
                {
                    return;
                }

                Invalidate();
                RegisterForNextAnimationFrameUpdate();
            }

            public void Dispose()
            {
                _effect?.Dispose();
                _effect = null;
            }

        }
    }



    public class MyControl : ContentControl
    {
        private CompositionCustomVisual? _visual;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var visual = ElementComposition.GetElementVisual(this);
            if (visual == null)
            {
                return;
            }

            if (_visual != null && _visual.Compositor == visual.Compositor)
            {
                return;
            }

            _visual = visual.Compositor.CreateCustomVisual(new RippleHandler(this));
            _visual.Size = new Vector(Bounds.Width, Bounds.Height);
            ElementComposition.SetElementChildVisual(this, _visual);
            _visual.SendHandlerMessage("start");
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            _visual!.Size = new Vector(e.NewSize.Width, e.NewSize.Height);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var pos = e.GetPosition(this);
            // 发送归一化坐标给 Handler
            var normalizedX = pos.X / Bounds.Width;
            var normalizedY = pos.Y / Bounds.Height;
            _visual?.SendHandlerMessage(new Point(normalizedX, normalizedY));
        }


    }

    class RippleHandler : CompositionCustomVisualHandler
    {
        private const int MaxRipples = 4;

        private readonly SKRuntimeEffect? _rippleEffect;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Control? _target;
        private readonly TopLevel? _root;
        private Point _location;
        private float _scale = 1.0f;

        // 波纹队列：(normalizedX, normalizedY, startTime)
        private readonly float[] _rippleData = new float[MaxRipples * 3];
        private int _rippleIndex = 0;

        // SkSL 水波纹扭曲着色器 —— 支持 4 个并发波纹，点击触发

        public RippleHandler(Control control)
        {
            _target = control;
            _root = TopLevel.GetTopLevel(_target);
            _location = _target!.TransformToVisual(_root!)?.Transform(new Point(0, 0)) ?? new Point(0, 0);
            if (_root != null)
            {
                _scale = (float)_root.RenderScaling;
                _location *= _scale;
                _root.ScalingChanged += TopLevel_ScalingChanged;
            }
            _target.LayoutUpdated += _target_LayoutUpdated;
            _rippleEffect = SKRuntimeEffect.CreateShader(SKRuntimeEffectHelper.RippleSksl, out var errors);
            if (_rippleEffect == null)
            {
                Debug.WriteLine($"SKRuntimeEffect compile error: {errors}");
            }
        }

        private void _target_LayoutUpdated(object? sender, EventArgs e)
        {
            _location = _target!.TransformToVisual(_root!)?.Transform(new Point(0, 0)) ?? new Point(0, 0);
            _location *= _scale;
        }

        private void TopLevel_ScalingChanged(object? sender, EventArgs e)
        {
            _scale = (float)((sender as TopLevel)?.RenderScaling ?? 1.0);
        }

        public override void OnRender(ImmediateDrawingContext context)
        {
            if (_rippleEffect == null)
                return;

            var api = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (api == null)
                return;
            using var lease = api.Lease();
            if (lease?.SkSurface == null)
                return;

            var bounds = this.GetRenderBounds();
            float width = (float)bounds.Size.Width;
            float height = (float)bounds.Size.Height;

            //报错
            //var location = _target!.TransformToVisual(_root!)?.Transform(new Point(0, 0)) ?? new Point(0, 0);
            //不报错，但没有后续了
            //var location = _root!.Dispatcher.Invoke(() => _target!.TranslatePoint(new Point(), _root!));

            //SkSurface是整棵树(窗口)的渲染结果
            using var snapshot = lease.SkSurface.Snapshot(new SKRectI(
                                                                (int)_location.X,
                                                                (int)_location.Y,
                                                                (int)(_location.X + _target!.Bounds.Width * _scale),
                                                                (int)(_location.Y + _target!.Bounds.Height * _scale)));

            //if (!Design.IsDesignMode)
            //{
            //    using var stream = File.Create("snapshot2.png");
            //    snapshot.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
            //}


            using var contentShader = snapshot.ToShader();

            var uniforms = new SKRuntimeEffectUniforms(_rippleEffect)
            {
                ["iResolution"] = new[] { snapshot.Width, snapshot.Height },
                ["iTime"] = (float)_stopwatch.Elapsed.TotalSeconds,
                ["iRipple0"] = new[] { _rippleData[0], _rippleData[1], _rippleData[2] },
                ["iRipple1"] = new[] { _rippleData[3], _rippleData[4], _rippleData[5] },
                ["iRipple2"] = new[] { _rippleData[6], _rippleData[7], _rippleData[8] },
                ["iRipple3"] = new[] { _rippleData[9], _rippleData[10], _rippleData[11] },
            };
            var children = new SKRuntimeEffectChildren(_rippleEffect)
            {
                ["iContent"] = contentShader
            };

            using var shader = _rippleEffect.ToShader(uniforms, children);
            using var paint = new SKPaint() { Shader = shader };
            lease.SkCanvas.Clear();
            lease.SkCanvas.DrawRect(0, 0, snapshot.Width, snapshot.Height, paint);
        }

        public override void OnAnimationFrameUpdate()
        {
            this.Invalidate();
            this.RegisterForNextAnimationFrameUpdate();
        }

        public override void OnMessage(object message)
        {
            base.OnMessage(message);
            if (message is string)
            {
                this.RegisterForNextAnimationFrameUpdate();
            }
            else if (message is Point clickPos)
            {
                // 写入新波纹：归一化中心 + 当前时间戳
                int baseIdx = (_rippleIndex % MaxRipples) * 3;
                _rippleData[baseIdx] = (float)clickPos.X;
                _rippleData[baseIdx + 1] = (float)clickPos.Y;
                _rippleData[baseIdx + 2] = (float)_stopwatch.Elapsed.TotalSeconds;
                _rippleIndex++;
                //this.Invalidate();
            }
        }
    }
}
