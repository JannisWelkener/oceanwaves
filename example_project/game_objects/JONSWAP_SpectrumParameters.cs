using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cgi.game_objects
{
    public sealed class JONSWAP_SpectrumParameters
    {
        public JONSWAP_SpectrumParameters(string _name) {
            name = _name;
        }
        public readonly string name;
        public bool wasUpdated = false;

        /*   
            settings.scale = display.scale;
            settings.angle = display.windDirection / 180 * Mathf.PI; //Wind direction in radians
            settings.spreadBlend = display.spreadBlend; //Controls how directional the waves are
            settings.swell = Mathf.Clamp(display.swell, 0.01f, 1); //Controls how much long-period swell dominates
            settings.alpha = JonswapAlpha(g, display.fetch, display.windSpeed); //The Phillips constant, determines total wave energy
            settings.peakOmega = JonswapPeakFrequency(g, display.fetch, display.windSpeed); //Peak angular frequency
            settings.gamma = display.peakEnhancement; //Controls how sharp the peak around peakOmega is
            settings.shortWavesFade = display.shortWavesFade; //Artificial high-frequency damping
         */

        public bool isActive = true;

        private float _windSpeed = 5f;
        private float _fetch = 100000f;
        private float _gravity = 9.81f;
        private float _scale = 1f;
        private float _degree = -29.81f;
        private float _spreadBlend = 0.9f;
        private float _swell = 0.9f;
        private float _gamma = 3.3f;
        private float _shortWavesFade = 0.01f;

        private float _lengthScale = 10f;
        private float _depth = 500f;

        private float _cutoffHigh = 100f;
        private float _cutoffLow = 7.539823f;

        private float _playbackSpeed = 0.25f;

        private float _uvScale = 1f;
        private float _foamMult = 1f;

        private float _lambda = 1f;

        private float _foamBias = -0.5f;
        private float _foamDecayRate = 20f;
        private float _foamAdd = 0.5f;

        public float foamAdd
        {
            get { return _foamAdd; }
            set { _foamAdd = value; wasUpdated = true; }
        }

        public float foamDecayRate
        {
            get { return _foamDecayRate; }
            set { _foamDecayRate = value; wasUpdated = true; }
        }

        public float foamBias
        {
            get { return _foamBias; }
            set { _foamBias = value; wasUpdated = true; }
        }

        public float lambda
        {
            get { return _lambda; }
            set { _lambda = value; wasUpdated = true; }
        }
        public float foamMult
        {
            get { return _foamMult; }
            set { _foamMult = value; wasUpdated = true; }
        }

        public float uvScale
        {
            get { return _uvScale; }
            set { _uvScale = value; wasUpdated = true; }
        }

        public float playbackSpeed
        {
            get { return _playbackSpeed; }
            set { _playbackSpeed = value; wasUpdated = true; }
        }

        public float cutoffLow
        {
            get { return _cutoffLow; }
            set { _cutoffLow = value; wasUpdated = true; }
        }

        public float cutoffHigh
        {
            get { return _cutoffHigh; }
            set { _cutoffHigh = value; wasUpdated = true; }
        }

        public float depth
        {
            get { return _depth; }
            set { _depth = value; wasUpdated = true; }
        }

        public float lengthScale
        {
            get { return _lengthScale; }
            set { _lengthScale = value; wasUpdated = true; }
        }

        public float windSpeed
        {
            get { return _windSpeed; }
            set { _windSpeed = value; wasUpdated = true; }
        }

        public float fetch
        {
            get { return _fetch; }
            set { _fetch = value; wasUpdated = true; }
        }

        public float gravity
        {
            get { return _gravity; }
            set { _gravity = value; wasUpdated = true; }
        }

        public float scale
        {
            get { return _scale; }
            set { _scale = value; wasUpdated = true; }
        }

        public float degree
        {
            get { return _degree; }
            set { _degree = value; wasUpdated = true; }
        }

        public float angleRadian
        {
            get { return degreeToRadian(_degree); }
        }

        public float spreadBlend
        {
            get { return _spreadBlend; }
            set { _spreadBlend = value; wasUpdated = true; }
        }

        public float swell
        {
            get { return _swell; }
            set { _swell = value; wasUpdated = true; }
        }

        public float alpha
        {
            get { return JonswapAlpha(gravity, fetch, windSpeed); }
        }

        public float peakOmega 
        { 
            get { return JonswapPeakFrequency(gravity, fetch, windSpeed); }
        }

        public float gamma {
            get { return _gamma; }
            set { _gamma = value; wasUpdated = true; } 
        }

        public float shortWavesFade
        {
            get { return _shortWavesFade; }
            set { _shortWavesFade = value; wasUpdated = true; }
        }
 
        private static float JonswapAlpha(float g, float fetch, float windSpeed)
        {
            return (float)(0.076f * Math.Pow(g * fetch / windSpeed / windSpeed, -0.22f));
        }

        private static float JonswapPeakFrequency(float g, float fetch, float windSpeed)
        {
            return (float)(22f * Math.Pow(windSpeed * fetch / g / g, -0.33f));
        }

        private static float degreeToRadian(float angle)
        {
            return angle / 180f * 3.1415926f;
        }



        public void saveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine($"windSpeed={_windSpeed.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"fetch={_fetch.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"gravity={_gravity.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"scale={_scale.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"degree={_degree.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"spreadBlend={_spreadBlend.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"swell={_swell.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"gamma={_gamma.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"shortWavesFade={_shortWavesFade.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"lengthScale={_lengthScale.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"depth={_depth.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"cutoffHigh={_cutoffHigh.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"cutoffLow={_cutoffLow.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"playbackSpeed={_playbackSpeed.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"uvScale={_uvScale.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"foamMult={_foamMult.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"lambda={_lambda.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"foamBias={_foamBias.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"foamDecayRate={_foamDecayRate.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"foamAdd={_foamAdd.ToString(CultureInfo.InvariantCulture)}");
            }
            Console.WriteLine("FFT Setting Saved");
        }

        public void loadFromFile(string filename)
        {
            if (!File.Exists(filename))
                return;

            var lines = File.ReadAllLines(filename);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    continue;

                var parts = line.Split('=');
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                float parsedValue;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
                    continue;

                switch (key)
                {
                    case "windSpeed": _windSpeed = parsedValue; break;
                    case "fetch": _fetch = parsedValue; break;
                    case "gravity": _gravity = parsedValue; break;
                    case "scale": _scale = parsedValue; break;
                    case "degree": _degree = parsedValue; break;
                    case "spreadBlend": _spreadBlend = parsedValue; break;
                    case "swell": _swell = parsedValue; break;
                    case "gamma": _gamma = parsedValue; break;
                    case "shortWavesFade": _shortWavesFade = parsedValue; break;
                    case "lengthScale": _lengthScale = parsedValue; break;
                    case "depth": _depth = parsedValue; break;
                    case "cutoffHigh": _cutoffHigh = parsedValue; break;
                    case "cutoffLow": _cutoffLow = parsedValue; break;
                    case "playbackSpeed": _playbackSpeed = parsedValue; break;
                    case "uvScale": _uvScale = parsedValue; break;
                    case "foamMult": _foamMult = parsedValue; break;
                    case "lambda": _lambda = parsedValue; break;
                    case "foamBias": _foamBias = parsedValue; break;
                    case "foamDecayRate": _foamDecayRate = parsedValue; break;
                    case "foamAdd": _foamAdd = parsedValue; break;
                }
            }

            wasUpdated = true;
        }
    }
}

