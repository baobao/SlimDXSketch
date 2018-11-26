using SlimDX;

public class Transform
{
    public Vector3 Position
    {
        get
        {
            return LocalPosition + (_parent != null ? _parent.Position : new Vector3(0, 0, 0));
        }
        set
        {
            LocalPosition = value - (_parent != null ? _parent.Position : new Vector3(0, 0, 0));
        }
    }
    
    public Vector3 LocalPosition { get; set; }

    public Vector3 EulerAngles
    {
        get
        {
            return LocalEulerAngles + (_parent != null ? _parent.EulerAngles : new Vector3(0, 0, 0));
        }
        set
        {
            LocalEulerAngles = value - (_parent != null ? _parent.EulerAngles : new Vector3(0, 0, 0));
        }
    }

    public Vector3 LocalEulerAngles { get; set; }

    public Vector3 LocalScale { get; set; } = new Vector3(1f, 1f, 1f);

    private Transform _parent;

    public Matrix MMatrix => _mMatrix;
    Matrix _mMatrix;
    Matrix _vpMatrix;
    Matrix _vMatrix;
    
    public Transform SetVPMatrix(Matrix vpMatrix)
    {
        _vpMatrix = vpMatrix;
        return this;
    }

    public Transform SetVMatrix(Matrix vMatrix)
    {
        _vMatrix = vMatrix;
        return this;
    }

    public Transform SetParent(DrawableObject parent)
    {
        _parent = parent;
        return this;
    }
    protected Matrix ResolveMVPMatrix()
    {
        // スケールMatrix
        var localScaleMatrix = Matrix.Scaling(LocalScale);

        // 回転行列
        var localRotateXMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(1, 0, 0), LocalEulerAngles.X)
        );
        var localRotateYMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(0, 1, 0), LocalEulerAngles.Y)
        );
        var localRotateZMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(0, 0, 1), LocalEulerAngles.Z)
        );
        // ZXYの順で計算
        var localRotateMatrix = localRotateZMatrix * localRotateXMatrix * localRotateYMatrix;

        var localPositionMatrix = Matrix.Identity;
        localPositionMatrix.set_Rows(3, new Vector4(
            LocalPosition.X,
            LocalPosition.Y,
            LocalPosition.Z, 1f));

        var localMMatrix = localScaleMatrix * localRotateMatrix * localPositionMatrix;

        _mMatrix = localMMatrix * (_parent != null ? _parent.MMatrix : Matrix.Identity);

        return _mMatrix * _vpMatrix;
    }


    private Matrix GetBillBoardMatrix()
    {
        Matrix ivMatrix = _vMatrix;
        ivMatrix.Invert();
        return ivMatrix;
    }
}