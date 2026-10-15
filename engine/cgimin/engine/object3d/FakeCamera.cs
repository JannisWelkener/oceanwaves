using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cgi.model
{
    public class FakeCamera
    {
        public Matrix4 Transformation;
        public Matrix4 PerspectiveProjection;
        public Vector3 Position;
        public Vector3 Rotation;
    }
}
