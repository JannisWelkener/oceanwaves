using cgi.game_objects.Buoyancy.Sampling;
using cgi.settings;
using OpenTK.Mathematics;

namespace cgi.game_objects.Buoyancy
{
    readonly struct WaveData
    {
        public readonly Vector3 position;
        public readonly Vector3 velocity;
        public WaveData(Vector3 p, Vector3 n)
        {
            position = p;
            velocity = n;
        }
    }
    public class CubeBuoyGerstner : CubeBuoy
    {

        public CubeBuoyGerstner(Vector2 startPos, float size, int voxelsPerAxis) : base(startPos, size, voxelsPerAxis) { }


        public override void SimulateBuoyancy(float fixedRate)
        {
            Mass = BuoyancySettings.mass;

            Vector3 AccumulatedForce = Vector3.Zero;
            Vector3 AccumulatedTorque = Vector3.Zero;

            float cubeVolume = cubeSize * cubeSize * cubeSize;
            float voxelHeight = cubeSize / VoxelsPerAxis;

            Vector3 Gravity = BuoyancySettings.gravity;

            float density = Mass / cubeVolume;
            float unitForce = (1.0f - density) / voxelLocalPositions.Count;

            int i = 0;

            Vector3 AccumulatedVelocity = Vector3.Zero;
            Vector3 VelocityAtCenter = Vector3.Zero;
            float submergedVolume = 0.0f;

            foreach (var voxel in voxelGlobalPositions)
            {
                Vector3 worldVoxel = voxel.Xyz;
                WaveData wave = GerstnerBuoyancySampling.SampleAccurateHeightAndNormal(worldVoxel, 8);

                float waterHeight = wave.position.Y;

                float depth = waterHeight - worldVoxel.Y + voxelHeight;
                Vector3 waterVelocity = wave.velocity;

                //Center Point flow
                if (i == voxelGlobalPositions.Count / 2)
                    VelocityAtCenter = waterVelocity;

                if (depth > 0.0f)
                {

                    float submergedFactor =
                        Math.Clamp(depth / voxelHeight, 0.0f, 1.0f);

                    float displacement = depth;

                    float submerged = Math.Clamp(depth / voxelHeight, 0f, 1f);
                    submergedVolume += submerged;

                    float buoyancyStrength = BuoyancySettings.buoyancyStrenght;
                    Vector3 buoyancyForce =
                        -Gravity * Mass * unitForce * submerged * buoyancyStrength;


                    AccumulatedForce += buoyancyForce;

                    Vector3 r = worldVoxel - Position;

                    AccumulatedTorque += Vector3.Cross(r, buoyancyForce);
                }
                i++;
            }
            //If any part is underwater:
            AccumulatedVelocity += VelocityAtCenter * submergedVolume / (VoxelsPerAxis * VoxelsPerAxis * VoxelsPerAxis);

            AccumulatedForce += Gravity * Mass;

            Vector3 acceleration = AccumulatedForce / Mass;

            Velocity += acceleration * fixedRate;

            //Correct damping (prevents objects jumping out of water)
            float damping = Lerp(0.995f, 0.90f, submergedVolume / voxelLocalPositions.Count);

            Velocity *= damping;
            Position += Velocity * fixedRate;

            horizontalVelocity += (AccumulatedVelocity - horizontalVelocity) * BuoyancySettings.flowStrength;

            horizontalVelocity *= BuoyancySettings.damping;

            Position += horizontalVelocity * BuoyancySettings.flowStrength;

            //Rotation
            float inertia =
                (1f / 6f) * Mass * cubeSize * cubeSize;

            Vector3 angularAcceleration =
                AccumulatedTorque / inertia;

            AngularVelocity += angularAcceleration * fixedRate;
            AngularVelocity *= 0.995f;

            float angularSpeed = AngularVelocity.Length;

            if (angularSpeed > 0.0001f)
            {
                Vector3 axis = AngularVelocity.Normalized();

                Quaternion deltaRotation =
                    Quaternion.FromAxisAngle(axis, angularSpeed * fixedRate);

                Orientation = deltaRotation * Orientation;
                Orientation.Normalize();
            }

            Transformation =
                Matrix4.CreateFromQuaternion(Orientation) *
                Matrix4.CreateTranslation(Position);
        }

    }
}

