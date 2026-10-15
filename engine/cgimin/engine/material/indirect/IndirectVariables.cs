namespace engine.cgimin.engine.material.indirect
{
    public class IndirectVariables
    {
        public struct UniformLocations {
            public static int modelviewProjectionMatrixLocation;
            public static int modelviewViewMatrixLocation;
            public static int cameraPositionLocation;
            public static int cameraPosition2Location;
            public static int cameraDirectionLocation;
            public static int chunkPosLocation;
            public static int elapsedTimeLocation;
            public static int chunkPositionLocation;
            public static int GPUCullingBoolLocation;
            public static int texture0Location;
            public static int texture1Location;
            public static int texture2Location;
        }

        public struct BufferVariables
        {
            public static int vertexBuffer;
            public static int indirectBuffer;
            public static int normalBuffer;
            public static int mappingBuffer;
            public static int uvBuffer;
            public static int indexBuffer;
            public static int visibleCountBuffer;
            public static int workGroupBuffer;
        }
    }
}
