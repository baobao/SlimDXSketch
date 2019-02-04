using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;

/// <summary>
/// さくっとライトにSlimDXを触りたいを思想に作成したスケッチ的なライブラリ
/// </summary>
public class SlimDXSketch : Form
{
    #region Property

    public static SlimDXSketch Instance => _instance ?? (_instance = new SlimDXSketch());

    public static SlimDX.Direct3D11.Device Device => Instance._device;

    public static DeviceContext ImmediateContext => Device.ImmediateContext;

    public static SwapChain SwapChain => Instance._swapChain;

    public static long FrameCount => Instance._frameCount;

    #endregion

    static SlimDXSketch _instance;
    static System.Action onDraw;
    static System.Action onClose;
    public static int fps = 30;
    public static SlimDX.Color4 clearColor = new SlimDX.Color4(1f, 0.8f, 0.8f, 0.8f);

    private SlimDX.Direct3D11.Device _device;
    private SwapChain _swapChain;
    private long _frameCount;
    private RenderTargetView _renderTarget;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    private DepthStencilView _depthStencil;


    static SlimDX.DirectInput.DirectInput _dxKeyboardInput;
    static SlimDX.DirectInput.Keyboard _keyboard;

    public static bool IsUseKeyboard => _dxKeyboardInput != null && _keyboard != null;

    static SlimDX.DirectInput.DirectInput _dxMouseInput;
    static SlimDX.DirectInput.Mouse _mouse;
    
    static bool IsUseMouse => _dxKeyboardInput != null && _mouse != null;

    public static CameraController camera { get; private set; }
    /// <summary>
    /// Constructor
    /// </summary>
    private SlimDXSketch(){}

    public static void Initialize(
        System.Action onDrawCallback,
        System.Action onCloseCallback,
        System.Action onSetupCallback
    )
    {
        onDraw = onDrawCallback;
        onClose = onCloseCallback;

        camera = new CameraController();

        Instance.InitDevice();
        onSetupCallback?.Invoke();

        Instance.InitRenderTarget();
        Instance.InitDepthStencil();
        ImmediateContext.
           OutputMerger.SetTargets(Instance._depthStencil, Instance._renderTarget);

        Instance.InitViewport();
        MessagePump.Run(Instance, Instance.Loop);
        onClose?.Invoke();
        onClose = null;
        Instance.DisposeContent();
    }

    /// <summary>
    /// GPU初期化
    /// </summary>
    private void InitDevice()
    {
        CreateDeviceAndSwapChain(this, out _device, out _swapChain);
    }

    private void InitViewport()
    {
        ImmediateContext.Rasterizer.SetViewports(
            new Viewport
            {
                Width = this.ClientSize.Width,
                Height = this.ClientSize.Height,
                MaxZ = 1
            });
    }

    /// <summary>
    /// 描画対象の初期化
    /// </summary>
    private void InitRenderTarget()
    {
        using (Texture2D backBuffer
            = SlimDX.Direct3D11.Resource.FromSwapChain<Texture2D>(_swapChain, 0)
            )
        {
            _renderTarget = new RenderTargetView(_device, backBuffer);
            ImmediateContext.OutputMerger.SetTargets(_renderTarget);
        }
    }

    private void InitDepthStencil()
    {
        Texture2DDescription depthBufferDesc = new Texture2DDescription
        {
            ArraySize = 1,
            BindFlags = BindFlags.DepthStencil,
            Format = Format.D32_Float,
            Width = ClientSize.Width,
            Height = ClientSize.Height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0)
        };

        using (Texture2D depthBuffer = new Texture2D(Device, depthBufferDesc))
        {
            _depthStencil = new DepthStencilView(Device, depthBuffer);
        }
    }

    /// <summary>
    /// ループ処理
    /// </summary>
    void Loop()
    {
        _stopwatch.Restart();

        ClearRenderTarget();

        ImmediateContext.ClearDepthStencilView(
            _depthStencil,
            DepthStencilClearFlags.Depth,
            1,
            0
            );


        // カメラ更新
        camera.UpdateCamera();
        // 描画
        onDraw?.Invoke();
        SwapChain.Present(0, PresentFlags.None);

        var elapsed = _stopwatch.ElapsedMilliseconds;
        var framePerMilliSec = 1000 / fps;
        var sleepTime = framePerMilliSec - elapsed;
        if (sleepTime > 0)
        {
            Thread.Sleep((int)sleepTime);
        }

        _frameCount++;
        if (_frameCount > long.MaxValue)
        {
            _frameCount = 0;
        }
    }

    /// <summary>
    /// 掃除
    /// </summary>
    void DisposeContent()
    {
        _renderTarget.Dispose();
        _swapChain.Dispose();
        _depthStencil.Dispose();
        _device.Dispose();
        onDraw = null;
        _instance = null;
        _dxKeyboardInput?.Dispose();
        _keyboard?.Dispose();
    }

    /// <summary>
    /// 背景クリア
    /// </summary>
    private void ClearRenderTarget()
    {
        ImmediateContext.ClearRenderTargetView(
            _renderTarget,
            clearColor
        );
    }
    
    static void CreateDeviceAndSwapChain(
        System.Windows.Forms.Form form,
        out SlimDX.Direct3D11.Device device,
        out SlimDX.DXGI.SwapChain swapChain
        )
    {
        SlimDX.Direct3D11.Device.CreateWithSwapChain(

            SlimDX.Direct3D11.DriverType.Hardware,
            SlimDX.Direct3D11.DeviceCreationFlags.None,
            new SlimDX.DXGI.SwapChainDescription
            {
                BufferCount = 1,
                OutputHandle = form.Handle,
                IsWindowed = true,
                SampleDescription = new SlimDX.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
                ModeDescription = new SlimDX.DXGI.ModeDescription
                {
                    Width = form.ClientSize.Width,
                    Height = form.ClientSize.Height,
                    RefreshRate = new SlimDX.Rational(60, 1),
                    Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm
                },
                Usage = SlimDX.DXGI.Usage.RenderTargetOutput

            },
            out device,
            out swapChain
            );
    }

    /// <summary>
    /// 頂点バッファ生成
    /// </summary>
    public static SlimDX.Direct3D11.Buffer CreateVertexBuffer(System.Array vertices)
    {
        using (SlimDX.DataStream vertexStream
            = new SlimDX.DataStream(vertices, true, true))
        {
            return new SlimDX.Direct3D11.Buffer(
                Instance._device,
                vertexStream,
                new BufferDescription
                {
                    SizeInBytes = (int)vertexStream.Length,
                    BindFlags = BindFlags.VertexBuffer,
                }
                );
        }
    }

    /// <summary>
    /// インデックスバッファ作成
    /// </summary>
    public static SlimDX.Direct3D11.Buffer CreateIndexBuffer(System.Array indexes)
    {
        using (SlimDX.DataStream indexStream
            = new SlimDX.DataStream(indexes, true, true))
        {
            return new SlimDX.Direct3D11.Buffer(
                Instance._device,
                indexStream,
                new BufferDescription
                {
                    SizeInBytes = (int)indexStream.Length,
                    BindFlags = BindFlags.IndexBuffer,
                }
                );
        }
    }

    /// <summary>
    /// シェーダコンパイルして返す
    /// </summary>
    public static Effect CompileFromFile(string path)
    {
        System.Console.WriteLine(path);

        using (
            ShaderBytecode shaderBytecode = ShaderBytecode.CompileFromFile
            (
                path,
                "fx_5_0",
                ShaderFlags.None,
                EffectFlags.None
            )
        )
        {
            return new Effect(Device, shaderBytecode);
        }
    }

    /// <summary>
    /// テクスチャをロード
    /// </summary>
    public static ShaderResourceView FromFileTexture(string path)
    {
        try
        {
            return ShaderResourceView.FromFile(Device, path);
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine(path + " / " + e.Message);
            return null;
        }
    }

    public static void UpdateInputAssembler(
        InputLayout vertexLayout,
        Buffer vertexBuffer,
        System.Type vertexDataType, 
        Buffer indexBuffer,
        PrimitiveTopology topology
        )
    {
        SetVertexBuffers(
            vertexLayout,
            vertexBuffer,
            vertexDataType);

        SetIndexBuffer(
            indexBuffer,
            topology
        );
    }

    public static void SetVertexBuffers(
        InputLayout vertexLayout,
        Buffer vertexBuffer,
        System.Type type
    )
    {
        ImmediateContext.InputAssembler.InputLayout = vertexLayout;
        ImmediateContext.InputAssembler.SetVertexBuffers(
            0,
            new VertexBufferBinding(
                vertexBuffer,
                    System.Runtime.InteropServices.Marshal.SizeOf(type),
            0
            )
        );
    }

    /// <summary>
    /// インデックスバッファのセット
    /// </summary>
    public static void SetIndexBuffer(
        Buffer indexBuffer,
        PrimitiveTopology topology = PrimitiveTopology.TriangleStrip
        )
    {
        ImmediateContext.InputAssembler.SetIndexBuffer(
            indexBuffer,
            Format.R32_UInt,
            0
        );
        SetPrimitiveTopology(topology);
    }

    public static void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        ImmediateContext.InputAssembler.PrimitiveTopology = topology;
    }


    public static void SetCullingMode(SlimDX.Direct3D11.CullMode mode, FillMode filleMode = FillMode.Solid) 
    {
        // http://memeplex.blog.shinobi.jp/directx11/c-%E3%81%A7directx11%20slimdx%E3%83%81%E3%ぷｂｌ83%A5%E3%83%BC%E3%83%88%E3%83%AA%E3%82%A2%E3%83%AB%E3%81%9D%E3%81%AE12%20%E3%83%AF%E3%82%A4%E3%83%A4%E3%83%BC%E3%83%95%E3%83%AC%E3%83%BC%E3%83%A0
        ImmediateContext.Rasterizer.State = RasterizerState.FromDescription(
            Device,
            new RasterizerStateDescription()
            {
                CullMode = mode,
                FillMode = filleMode
            }
        );
    }

    public static bool IsPressed(SlimDX.DirectInput.Key[] keys, bool isOnce = true)
    {
        if (IsUseKeyboard == false)
            SetupKeyboard();

        for(int i = 0; i < keys.Length; i++)
        {
            if (IsPressed(keys[i], isOnce) == false)
            {
                return false;
            }
        }
        return true;
    }

    public static SlimDX.DirectInput.Key GetPressedKey()
    {
        if (IsUseKeyboard == false)
            SetupKeyboard();

        var stateList = GetKeyboardBufferData();
        if (stateList != null)
        {
            for (int i = 0; i < stateList.Count; i++)
            {
                var state = stateList[i];
                foreach (var __key in state.PressedKeys)
                {
                    return __key;
                }
            }
        }
        return SlimDX.DirectInput.Key.Unknown;
    }

    public static bool IsPressed(SlimDX.DirectInput.Key key, bool isOnce = true)
    {
        if (IsUseKeyboard == false)
            SetupKeyboard();

        if (isOnce)
        {
            var stateList = GetKeyboardBufferData();
            if (stateList != null)
            {
                for (int i = 0; i < stateList.Count; i++)
                {
                    var state = stateList[i];
                    foreach(var __key in state.PressedKeys)
                    {
                        if (key == __key)
                        {
                            return true;
                        }
                    }
                    if (state.IsPressed(key))
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            // 押しっぱなしを検出
            var keyboard = GetKeyboard();
            if (keyboard != null && keyboard.GetCurrentState().IsPressed(key))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// キーボードを使用したい場合は実行してください
    /// その後GetKeyBoardでKeyboardを取得してください
    /// </summary>
    public static void SetupKeyboard()
    {
        if (IsUseKeyboard)
        {
            return;
        }

        // 参考サイト
        // http://io-fia.blogspot.com/2011/03/slimdxdirect2d.html
        _dxKeyboardInput = new SlimDX.DirectInput.DirectInput();
        _keyboard = new SlimDX.DirectInput.Keyboard(_dxKeyboardInput);
        _keyboard.SetCooperativeLevel(
            SlimDXSketch.Instance.Handle,
            SlimDX.DirectInput.CooperativeLevel.Foreground
            | SlimDX.DirectInput.CooperativeLevel.Nonexclusive
        );
        _keyboard.Properties.BufferSize = 4;
    }


    /// <summary>
    /// Keyboardを取得します
    /// </summary>
    static SlimDX.DirectInput.Keyboard GetKeyboard()
    {
        if (_keyboard != null && _keyboard.Acquire().IsSuccess)
        {
            return _keyboard;
        }
        return null;
    }
    
    static IList<SlimDX.DirectInput.KeyboardState> GetKeyboardBufferData()
    {
        var keyboard = GetKeyboard();
        return keyboard?.GetBufferedData();
    }


    /// <summary>
    /// マウスの移動Deltaを取得
    /// </summary>
    public static bool TryGetMouseDelta(out SlimDX.Vector2 result)
    {
        result = new SlimDX.Vector2(0, 0);
        var mouse = GetMouse();
        if (mouse != null)
        {
            var state = mouse.GetCurrentState();
            result = new SlimDX.Vector2(state.X, state.Y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Mouseを取得します
    /// </summary>
    static SlimDX.DirectInput.Mouse GetMouse()
    {
        if (IsUseMouse == false)
            SetupMouse();

        if (_mouse != null && _mouse.Acquire().IsSuccess)
        {
            return _mouse;
        }
        return null;
    }

    static void SetupMouse()
    {
        _dxMouseInput = new SlimDX.DirectInput.DirectInput();
        _mouse = new SlimDX.DirectInput.Mouse(_dxMouseInput);
        _mouse.Properties.BufferSize = 100;
        _mouse.SetCooperativeLevel(SlimDXSketch.Instance.Handle,
            SlimDX.DirectInput.CooperativeLevel.Foreground
            | SlimDX.DirectInput.CooperativeLevel.Nonexclusive
        );
    }
}
