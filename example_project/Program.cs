

#region --- Using Directives ---

using OpenTK.Graphics.OpenGL;
using cgimin.engine.object3d;
using cgimin.engine.camera;
using cgimin.engine.light;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using engine.cgimin.engine.material.ambientdiffuse;
using cgi.control;
using cgimin.engine.material.simpletexture;
using cgi.shaders.gerstner;
using cgi.shaders.tessellation.Gerstner;
using cgi.game_objects.Buoyancy;
using cgi.game_objects.Buoyancy.Sampling;
using cgimin.engine.texture;
using cgi.settings;
using cgimin.engine.material.skyboxmaterial;
using cgi.game_objects.particles;
using cgimin.engine.material.simpleui;
using cgimin.engine.object2d;
using cgi.game_objects;
using cgi.utils;


#endregion --- Using Directives ---

namespace Examples.Tutorial
{
    enum WaveType
    {
        GerstnerWave,
        FFTWave
    }
    public class ExampleProject : GameWindow
    {
        private Vector2i windowedSize;
        private Vector2i windowedLocation;

        private float deltaTime = 0;
        public static volatile float _fps;

        static bool cursorStateGrabbed = true;
        static bool windowMode = true;
        static bool pauseSimulation = false;

        public static bool wireframe = false;

        static WaveType toggleWaveModel = WaveType.GerstnerWave;

        float accumulator = 0f;
        const float fixedStep = 0.02f;// 50 Hz

        int crateTexture;

        //Reused
        QuadOceanSurface quadTessSurface;
        CubeObject3D debugVoxel;
        SimpleTextureMaterial simpleTextureMaterial;

        //Gerstner
        GerstnerTessellationMaterial? tessellationMaterial;
        List<CubeBuoyGerstner> gerstnerBuoys = new List<CubeBuoyGerstner>();  
        
        //FFT
        FFTOcean fftOcean = new FFTOcean(512);
        List<CubeBuoyFFT> fftBuoys = new List<CubeBuoyFFT>();
        FFTBuoyancySampling fftBuoyancy;
        SprayParticleSystem spray = new SprayParticleSystem();

        //Post Processing
        PostProcessing postProcessing;
        Object2D screenQuad;
        SimpleUIMaterial simpleUIMaterial;
        DebugMaterial debugMaterial;

        //Post Processing Layers
        int fbo;
        public static int depthTexture;
        public static int colorTexture;
        public static int depthCopy;

        public ExampleProject(int width, int height, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) {
            Size = (width, height);
            this.KeyDown += KeyboardKeyDown;
            this.KeyUp += KeyboardKeyUp;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (CursorState == CursorState.Grabbed)
            {
                const float mouseSensitivity = 0.0005f;

                float deltaX = e.DeltaX;
                float deltaY = e.DeltaY;

                Camera.rotation.Y += deltaX * mouseSensitivity;
                Camera.rotation.X -= deltaY * mouseSensitivity;

                float maxPitch = MathHelper.DegreesToRadians(89f);
                Camera.rotation.X = MathHelper.Clamp(Camera.rotation.X, -maxPitch, maxPitch);

                if (Camera.rotation.Y > MathHelper.TwoPi)
                    Camera.rotation.Y -= MathHelper.TwoPi;
                else if (Camera.rotation.Y < -MathHelper.TwoPi)
                    Camera.rotation.Y += MathHelper.TwoPi;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            float offset = e.Offset.Y;

            float sensitivity = 0.5f; 

            Camera.moveSpeed += offset * sensitivity;
            Camera.moveSpeed = MathHelper.Clamp(Camera.moveSpeed, 0f, 150f);
        }

        void KeyboardKeyDown(KeyboardKeyEventArgs keyevent)
        {
            if (keyevent.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
            {
                if (cursorStateGrabbed)
                {
                    CursorState = CursorState.Normal;
                }
                else
                {
                    CursorState = CursorState.Grabbed;
                }
                cursorStateGrabbed = !cursorStateGrabbed;
            }

            if (keyevent.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11)
            {
                if (windowMode)
                {
                    windowedSize = this.Size;
                    windowedLocation = this.Location;
                    this.WindowBorder = WindowBorder.Hidden;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.WindowBorder = WindowBorder.Fixed;
                    this.Size = windowedSize;
                    this.Location = windowedLocation;
                }

                windowMode = !windowMode;
            }

            switch (keyevent.Key)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.W: Camera.moveForward = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.S: Camera.moveBackward = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.A: Camera.moveLeft = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.D: Camera.moveRight = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space: Camera.moveUp = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl: Camera.moveDown = true; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter: pauseSimulation = !pauseSimulation; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace: wireframe = !wireframe; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.X: SkyboxMaterial.whiteOut = !SkyboxMaterial.whiteOut; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1: OceanControlWindow.updateControlWindowGerstner(); toggleWaveModel = WaveType.GerstnerWave; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.D2: OceanControlWindow.updateControlWindowFFT(GlobalSettings.selectedParameter); toggleWaveModel = WaveType.FFTWave; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Tab: GlobalSettings.nextParameters(); OceanControlWindow.updateControlWindowFFT(GlobalSettings.selectedParameter); break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift:toggleSettings(); break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.T: setCameraAndTime(); break;

            }
        }

        public static bool isSceneStatic = false;
        private void setCameraAndTime()
        {
            Camera.position = new Vector3(53, 29, -38);
            Camera.rotation = new Vector3(0, 1.5f, 0);
            ElapsedTime.elapsedTime = 0;
            isSceneStatic = !isSceneStatic;
        }

        private void toggleSettings()
        {
            GlobalSettings.showSettings = !GlobalSettings.showSettings;
            if (GlobalSettings.showSettings) {
                OceanControlWindow.Show();
                PBRControlWindow.Show();
                ParticleControlWindow.Show();
                BuoyancyControlWindow.Show();
            }
            else
            {
                OceanControlWindow.Hide();
                PBRControlWindow.Hide();
                ParticleControlWindow.Hide();
                BuoyancyControlWindow.Hide();
            }
        }

        void KeyboardKeyUp(KeyboardKeyEventArgs keyevent)
        {
            switch (keyevent.Key)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.W: Camera.moveForward = false; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.S: Camera.moveBackward = false; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.A: Camera.moveLeft = false; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.D: Camera.moveRight = false; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space: Camera.moveUp = false; break;
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl: Camera.moveDown = false; break;
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            Console.WriteLine("-= Master Thesis =-");

            CursorState = CursorState.Grabbed;
            Light.SetDirectionalLight(new Vector3(1,1,0), new Vector4(0.3f, 0.3f, 0.3f, 1), new Vector4(1f, 1.0f, 1.0f, 1), new Vector4(1.0f, 1.0f, 1.0f, 1));

            Camera.Init();
            Camera.SetWidthHeightFov(1920, 1080, 60);

            Camera.Transformation *= Matrix4.CreateTranslation(-8, -1, 0);
            Camera.Transformation *= Matrix4.CreateRotationY(-3.1415f / 2f);

            GlobalSettings.Init();
            PBRSettings.Init();

            tessellationMaterial = new GerstnerTessellationMaterial();

            quadTessSurface = new QuadOceanSurface(1700f, 32);

            debugVoxel = new CubeObject3D(0.5f);
            gerstnerBuoys.Add(new CubeBuoyGerstner(new Vector2(0, 0), 5, 3));
            //gerstnerBuoys.Add(new CubeBuoyGerstner(new Vector2(10, 0), 5, 3));
            //gerstnerBuoys.Add(new CubeBuoyGerstner(new Vector2(0, 10), 5, 3));
            //gerstnerBuoys.Add(new CubeBuoyGerstner(new Vector2(10, 10), 5, 3));


            //2 always fail! Not gerstner buoy
            //fftBuoys.Add(new CubeBuoyFFT(new Vector2(0, 0), 5f, 1));
            //fftBuoys.Add(new CubeBuoyFFT(new Vector2(40, 0), 5f, 1));

            for(int i = 0;i<10;i++)
                for (int j = 0; j < 10; j++)
                    fftBuoys.Add(new CubeBuoyFFT(new Vector2(i*6, j*6), 5f, 3));

            simpleTextureMaterial = new SimpleTextureMaterial();
            debugMaterial = new DebugMaterial();
            crateTexture = TextureManager.LoadTexture("shaders/crate.png");

            CursorState = CursorState.Grabbed;

            GerstnerControlWindow.ShowControlWindow();
            PBRControlWindow.ShowShaderControlWindow();
            ParticleControlWindow.ShowParticleControlWindow();

            BuoyancyControlWindow.ShowBuoyancyControlWindow();

            GerstnerWaves.Instance.CalculateWavesFromJONSWAP(GlobalSettings.parameters0);


            fftOcean.Load(quadTessSurface);

            fftBuoyancy = new FFTBuoyancySampling();
            foreach (CubeBuoyFFT buoy in fftBuoys)
            {
                fftBuoyancy.Register(buoy);
            }

            fftBuoyancy.Init();

            spray.Load(fftOcean.init0.offsetTex, fftOcean.init1.offsetTex, fftOcean.init2.offsetTex, GlobalSettings.parameters0, GlobalSettings.parameters1, GlobalSettings.parameters2);




            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            depthTexture = ShaderUtils.GenerateDepthTexture();
            //Bind to fbo
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D,
                depthTexture,
                0
            );

            depthCopy = ShaderUtils.GenerateDepthTexture();

            colorTexture = ShaderUtils.GenerateScreenTexture();
            //Bind to fbo
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                colorTexture,
            0
            );


            postProcessing = new PostProcessing();
            postProcessing.Load(colorTexture);


            screenQuad = new ScreenObject2D(GlobalSettings.screenSize);
            simpleUIMaterial = new SimpleUIMaterial((int)GlobalSettings.screenSize.X, (int)GlobalSettings.screenSize.Y);

        }

        private void UpdateSkyboxIfChanged()
        {
            if (PBRSettings.PendingSkybox != null)
            {
                GlobalSettings.skyboxMaterial.LoadSkybox(PBRSettings.PendingSkybox);
                PBRSettings.PendingSkybox = null;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            deltaTime = (float)e.Time;
            float instantFps = 1f / deltaTime;
            _fps = (_fps * 0.9f + instantFps * 0.1f);

            UpdateSkyboxIfChanged();

            if (toggleWaveModel == WaveType.GerstnerWave)
            {
                if (GlobalSettings.parameters0.wasUpdated)
                {
                    GerstnerWaves.Instance.CalculateWavesFromJONSWAP(GlobalSettings.parameters0);
                    fftOcean.init0.GenerateInitialSpectrum();
                    GlobalSettings.parameters0.wasUpdated = false;
                }
            }


            if (toggleWaveModel == WaveType.FFTWave)
            {
                if (!pauseSimulation)
                {
                    fftOcean.Update(deltaTime);
                }
            }


            //Infinity-grid snap
            quadTessSurface.Update(Camera.position);

            if (isSceneStatic)
                ElapsedTime.elapsedTime = 0;

            if (!isSceneStatic)
                if (!pauseSimulation)
            {

                //FIXED UPDATE
                accumulator += deltaTime;
                while (accumulator >= fixedStep)
                {

                OnFixedUpdate(fixedStep);

                accumulator -= fixedStep;
                }

                
                ElapsedTime.elapsedTime += 2.0f * deltaTime;
                ElapsedTime.frameIndex++;
                
            }

            
        }

        protected void OnFixedUpdate(float rate) {

            spray.FixedUpdate(
                   GlobalSettings.parameters0.uvScale, GlobalSettings.parameters1.uvScale, GlobalSettings.parameters2.uvScale,
                   GlobalSettings.parameters0.foamMult, GlobalSettings.parameters1.foamMult, GlobalSettings.parameters2.foamMult,
                   GlobalSettings.parameters0.isActive, GlobalSettings.parameters1.isActive, GlobalSettings.parameters2.isActive
               );

            if (toggleWaveModel == WaveType.FFTWave)
            {
                //Retrieve all buoyancy sample-points from gpu 
                fftBuoyancy.Update(fftOcean.init0, fftOcean.init1, fftOcean.init2, GlobalSettings.parameters0, GlobalSettings.parameters1, GlobalSettings.parameters2);
                //Do buoyancy sim with retrieved sample-points from last frames. Acurate enough
                foreach (CubeBuoyFFT buoy in fftBuoys)
                {
                    if (BuoyancySettings.doCollision)
                        buoy.FixedUpdate(rate, fftBuoys);
                    else
                        buoy.FixedUpdate<CubeBuoyFFT>(rate);
                }

            }
                if (toggleWaveModel == WaveType.GerstnerWave)
                {
                    foreach (CubeBuoyGerstner buoy in gerstnerBuoys)
                    {
                        if (BuoyancySettings.doCollision)
                            buoy.FixedUpdate(rate, gerstnerBuoys);
                        else
                            buoy.FixedUpdate<CubeBuoyGerstner>(rate);
                    }
                }
            }



        protected override void OnRenderFrame(FrameEventArgs frameevent)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);

            Camera.Update(deltaTime);

            PBRSettings.skybox!.Transformation = Matrix4.CreateScale(800);
            PBRSettings.skybox.Transformation *= Matrix4.CreateTranslation(Camera.position.X, Camera.position.Y - PBRSettings.verticalSkyboxOffset, Camera.position.Z); //Skybox fixed to camera

            GlobalSettings.skyboxMaterial!.Draw(PBRSettings.skybox);

            if (toggleWaveModel == WaveType.GerstnerWave) 
            {

                //BOUYANCY
                if (wireframe)
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                foreach (CubeBuoyGerstner buoy in gerstnerBuoys)
                {
                    simpleTextureMaterial!.Draw(buoy, crateTexture);
                    for (int i = 0; i < buoy.voxelGlobalPositions.Count; i++)
                    {
                        debugVoxel.Transformation = Matrix4.CreateTranslation(buoy.voxelGlobalPositions[i].Xyz);
                        debugMaterial!.Draw(debugVoxel);
                    }
                } 
                if (wireframe)
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                FreezeDepth();

                tessellationMaterial!.Draw(quadTessSurface, GlobalSettings.screenSize, GlobalSettings.skyboxMaterial.skyboxTexture,depthCopy);
            }
            else
            {
                foreach (CubeBuoyFFT buoy in fftBuoys)
                {
                    if (wireframe)
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    simpleTextureMaterial!.Draw(buoy, crateTexture);
                    if (wireframe)
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                    for (int i = 0; i < buoy.voxelGlobalPositions.Count; i++)
                    {
                        debugVoxel.Transformation = Matrix4.CreateTranslation(buoy.voxelGlobalPositions[i].Xyz);
                        bool submerged = false;
                        if(buoy.sampledData.Count > i) {
                            float voxelh = buoy.voxelGlobalPositions[i].Y;
                            float wh = buoy.sampledData[i].waveHeight;
                            float depth = wh - voxelh;
                            submerged = depth > 0.0f;
                        }
                        debugMaterial!.Draw(debugVoxel, submerged);
                    }
                }

                FreezeDepth();

                fftOcean.Render();

                spray.Render();
            }

            //Only do Post-Processing if needed (big performance hit)
            if (PBRSettings.hdrStrenght <= 0)
            {
                FullscreenPass(colorTexture);
            }
            else
            {
                int finalScene = postProcessing.RunPostProcessing();
                FullscreenPass(finalScene);
            }
            

            GL.Enable(EnableCap.CullFace);
            SwapBuffers();
        }

        private void FullscreenPass(int finalScene)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(0.3f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            simpleUIMaterial.Draw(screenQuad, finalScene, 1, 0, 0);
        }

        private void FreezeDepth() {
            GL.CopyImageSubData(
                    depthTexture, (ImageTarget)TextureTarget.Texture2D, 0, 0, 0, 0,
                    depthCopy, (ImageTarget)TextureTarget.Texture2D, 0, 0, 0, 0,
                    (int)GlobalSettings.screenSize.X,
                    (int)GlobalSettings.screenSize.Y, 1
                );
        }

        protected override void OnUnload()
        {
        }


        [STAThread]
        public static void Main()
        {
            var windowSettings = GameWindowSettings.Default;
            windowSettings.UpdateFrequency = 500; //max 500, confirmed reaches max on empty on rtx 3070ti

            var nativeWindowSettings = NativeWindowSettings.Default;
            nativeWindowSettings.Flags = ContextFlags.ForwardCompatible | ContextFlags.Default;
            nativeWindowSettings.Title = "Master Thesis";
            nativeWindowSettings.WindowState = WindowState.Normal;
            nativeWindowSettings.WindowBorder = WindowBorder.Fixed;

            using var example = new ExampleProject((int)GlobalSettings.screenSize.X, (int)GlobalSettings.screenSize.Y, windowSettings, nativeWindowSettings);
            example.Run();
        }
    }


}

