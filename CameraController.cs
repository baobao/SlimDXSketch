using SlimDX;

public class CameraController : Transform
{
    public Vector3 LookAtPos { get => _lookAtPos; set => _lookAtPos = value; }
    private Vector3 _lookAtPos;

    Matrix _vMatrix;
    Matrix _vpMatrix;
    Matrix _pMatrix;
    /// <summary>
    /// 並行投影orthographicsのVPMatrix
    /// </summary>
    Matrix _orthoVPMatrix;

    public Matrix VMatrix { get =>_vMatrix; }
    public Matrix PMatrix { get => _pMatrix; }
    public Matrix VPMatrix { get => _vpMatrix; }
    public Matrix OrthoVPMatrix { get => _orthoVPMatrix; }

    public void UpdateCamera()
    {
        _vMatrix = Matrix.LookAtLH(
            // eye
            Position,
            // target
            LookAtPos,
            // up
            new Vector3(0, 1, 0)
            );

        _pMatrix = Matrix.PerspectiveFovLH(
            (float)System.Math.PI / 2,
            SlimDXSketch.Instance.ClientSize.Width /
                 SlimDXSketch.Instance.ClientSize.Height,
            0.1f, 1000
            );


        _vpMatrix = _vMatrix * _pMatrix;

        Matrix orthoProjection = Matrix.OrthoLH(
            (float)SlimDXSketch.Instance.ClientSize.Width / 100f,
            (float)SlimDXSketch.Instance.ClientSize.Height / 100f,
            0.1f, 1000
            );
        _orthoVPMatrix = CreateUIViewMatrix() * orthoProjection;
    }

    Matrix CreateUIViewMatrix()
    {
        return Matrix.LookAtLH(
            // eye
            new Vector3(0, 0, -3f),
            // target
            new Vector3(),
            // up
            new Vector3(0, 1, 0)
            );
    }
}
