using cgi.shaders.gerstner;
using engine.cgimin.engine.material.ambientdiffuse;
using OpenTK.Mathematics;

namespace cgi.game_objects.Buoyancy.Sampling
{
    public class GerstnerBuoyancySampling
    {
        private static GerstnerWaves gerstnerWaves = GerstnerWaves.Instance;

        internal static WaveData SampleAccurateHeightAndNormal(Vector3 target, int maxRecursions)
        {
            Vector2 guess = new Vector2(target.X, target.Z);

            for (int i = 0; i < maxRecursions; i++)
            {
                Vector3 displaced = EvaluateGerstnerWaves(
                    new Vector3(guess.X, 0, guess.Y)
                ).position;

                Vector2 error = new Vector2(
                    displaced.X - target.X,
                    displaced.Z - target.Z
                );

                if (error.LengthSquared < 0.0001f)
                    break;

                guess -= error;
            }

            WaveData finalPos = EvaluateGerstnerWaves(
                new Vector3(guess.X, 0, guess.Y)
            );

            return finalPos;
        }

        private static WaveData EvaluateGerstnerWaves(Vector3 position)
        {
            Vector3 displaced = position;
            List<Wave> waves = gerstnerWaves.waves;
            float time = ElapsedTime.elapsedTime * gerstnerWaves.playbackSpeed;
            float steepness = gerstnerWaves.steepness;

            Vector3 velocity = Vector3.Zero;

            foreach (var wave in waves)
            {
                float A = wave.A / gerstnerWaves.scaling;
                A = A * gerstnerWaves.strengthAmplifier;
                float phase = wave.k * Vector2.Dot(Vector2.Normalize(wave.D), new Vector2(position.X, position.Z) * gerstnerWaves.scaling) + wave.speed * time + wave.phase;
                float sinP = MathF.Sin(phase);
                float cosP = MathF.Cos(phase);
                float QA = steepness * A;

                displaced.X += wave.D.X * QA * cosP;
                displaced.Y += A * sinP;
                displaced.Z += wave.D.Y * QA * cosP;

                velocity.X += -wave.D.X * QA * wave.speed * sinP;
                velocity.Z += -wave.D.Y * QA * wave.speed * sinP;
            }

            return new WaveData(displaced, velocity);
        }
    }
}
