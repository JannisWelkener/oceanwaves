using cgi.game_objects;
using cgi.shaders.tessellation.FFT;
using cgimin.engine.material.simpleui;
using cgimin.engine.object2d;
using cgimin.engine.object3d;
using Examples.Tutorial;

namespace cgi.settings
{
    public class FFTOcean
    {
        public int fftSize = 128 * 2 * 2;

        private _2DIFFT ifft;

        public JonswapSpectrumGenerator init0;
        public JonswapSpectrumGenerator init1;
        public JonswapSpectrumGenerator init2;

        private FFTTessellationMaterial fftTessellationMaterial;
        private QuadOceanSurface quadTessSurface;

        private JONSWAP_SpectrumParameters parameters0 = GlobalSettings.parameters0;
        private JONSWAP_SpectrumParameters parameters1 = GlobalSettings.parameters1;
        private JONSWAP_SpectrumParameters parameters2 = GlobalSettings.parameters2;

        private SimpleUIMaterial simpleUIMaterial;
        private Object2D object2D;

        public FFTOcean(int size){
            fftSize = size;
        }

        public void Load(QuadOceanSurface surface)
        {
            quadTessSurface = surface;
            fftTessellationMaterial = new FFTTessellationMaterial();

            ifft = new _2DIFFT(fftSize);

            init0 = new JonswapSpectrumGenerator(parameters0, ifft, fftSize);
            init1 = new JonswapSpectrumGenerator(parameters1, ifft, fftSize);
            init2 = new JonswapSpectrumGenerator(parameters2, ifft, fftSize);

            init0.InitTexturesAndShaders();
            init0.GenerateInitialSpectrum();

            init1.InitTexturesAndShaders();
            init1.GenerateInitialSpectrum();

            init2.InitTexturesAndShaders();
            init2.GenerateInitialSpectrum();

            simpleUIMaterial = new SimpleUIMaterial((int)GlobalSettings.screenSize.X, (int)GlobalSettings.screenSize.Y);
            object2D = new Object2D(GlobalSettings.uiSize);
        }

        public void Update(float deltaTime)
        {
            if (parameters0.wasUpdated)
            {
                init0.GenerateInitialSpectrum();
                parameters0.wasUpdated = false;
            }
            if (parameters1.wasUpdated)
            {
                init1.GenerateInitialSpectrum();
                parameters1.wasUpdated = false;
            }
            if (parameters2.wasUpdated)
            {
                init2.GenerateInitialSpectrum();
                parameters2.wasUpdated = false;
            }

            init0.Update(deltaTime);
            init1.Update(deltaTime);
            init2.Update(deltaTime);
        }

        public void Render()
        {
            fftTessellationMaterial!.Draw(quadTessSurface, GlobalSettings.screenSize, init0, init1, init2, parameters0, parameters1, parameters2, GlobalSettings.toggleGrid, GlobalSettings.skyboxMaterial.skyboxTexture, ExampleProject.depthCopy);

            int screenSizeX = (int)GlobalSettings.screenSize.X;
            int screenSizeY = (int)GlobalSettings.screenSize.Y;

            if (GlobalSettings.showSettings)
            {
                JonswapSpectrumGenerator spectrum = init0;

                simpleUIMaterial.Draw(object2D, ifft.precomputedTwiddleTexture, 1, screenSizeX - object2D.uiSize * 2, screenSizeY - object2D.uiSize);

                simpleUIMaterial.Draw(object2D, spectrum.flowTex, 1, screenSizeX - object2D.uiSize * 2, screenSizeY - object2D.uiSize * 2);

                simpleUIMaterial.Draw(object2D, spectrum.H0Tex, 100000, screenSizeX - object2D.uiSize, screenSizeY - object2D.uiSize);
                simpleUIMaterial.Draw(object2D, spectrum.DxDz_DyDxzCOPYTex, 100000, screenSizeX - object2D.uiSize, screenSizeY - object2D.uiSize * 2);
                simpleUIMaterial.Draw(object2D, spectrum.DxDz_DyDxzTex, 1000, screenSizeX - object2D.uiSize, screenSizeY - object2D.uiSize * 3);
                simpleUIMaterial.Draw(object2D, spectrum.DyxDyz_DxxDzzTex, 1000, screenSizeX - object2D.uiSize, screenSizeY - object2D.uiSize * 4);
                simpleUIMaterial.Draw(object2D, spectrum.offsetTex, 1000, screenSizeX - object2D.uiSize, screenSizeY - object2D.uiSize * 5);
            }

            
        }
    }
}
