using OpenTK.Mathematics;


namespace cgimin.engine.camera
{
    public class Camera
    {
        // Camera movement state
        public static bool moveForward;
        public static bool moveBackward;
        public static bool moveLeft;
        public static bool moveRight;
        public static bool moveUp;
        public static bool moveDown;

        public static float moveSpeed = 15.0f;

        public static float nearPlane = 0.1f;
        public static float farPlane = 5000.0f;

        // Matrix for the transformation
        private static Matrix4 transformation;

        // ... and the petrspective projection
        public static Matrix4 perspectiveProjection;

        // position for the camera is saved
        public static Vector3 position = new Vector3(0,1,0);
        public static Vector3 rotation = new Vector3(0.1f,3.1415f + 3.1415f/2,0);


        public static void Update(float deltaTime)
        {

            Vector3 forward = new Vector3(
                (float)Math.Sin(rotation.Y) * (float)Math.Cos(rotation.X),
                (float)Math.Sin(rotation.X),
                (float)-Math.Cos(rotation.Y) * (float)Math.Cos(rotation.X)
            ).Normalized();

            // Right vector
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

            Vector3 up = Vector3.UnitY;

            Vector3 movement = Vector3.Zero;

            if (moveForward) movement += forward;
            if (moveBackward) movement -= forward;
            if (moveRight) movement += right;
            if (moveLeft) movement -= right;
            if (moveUp) movement += up;
            if (moveDown) movement -= up;

            if (movement.LengthSquared > 0)
                movement = movement.Normalized();

            position += movement * moveSpeed * deltaTime;

            Vector3 target = position + forward;
            Transformation = Matrix4.LookAt(position, target, Vector3.UnitY);
        }




        static Vector3 GetVectorFromAngle(float yaw, float pitch)
        {
            float x = MathF.Cos(pitch) * MathF.Sin(yaw);
            float y = MathF.Sin(pitch);
            float z = MathF.Cos(pitch) * MathF.Cos(yaw);
            return new Vector3(x, y, z);
        }

        public static void Init()
        {
            perspectiveProjection = Matrix4.Identity;
            transformation = Matrix4.Identity;
        }


        // width, height = size of screen in pixeln, fov = "field of view", der opening-angle for the camera lense
        public static void SetWidthHeightFov(int width, int height, float fov)
        {
            float aspectRatio = width / (float)height;
            Matrix4.CreatePerspectiveFieldOfView((float)(fov * Math.PI / 180.0f), aspectRatio, nearPlane, farPlane, out perspectiveProjection);
        }


        // generation of the camera-transformation using LookAt
        // position of the camera-"eye", look-at poinmt, "up" direction of camera
        public static void SetLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            position = eye;
            transformation = Matrix4.LookAt(eye, target, up);
        }


        // Getter
        public static Vector3 Position
        {
            get { return position; }
        }


        public static Matrix4 Transformation
        {
            get { return transformation; }
            set { transformation = value; }
        }




        public static Matrix4 PerspectiveProjection
        {
            get { return perspectiveProjection; }
        }

        public static Matrix4 GetViewMatrix()
        {
            return Transformation;
        }

    }
}
