using SlimDX;

public class Transform
{
    public Vector3 position
    {
        get
        {
            return localPosition + ParentPosition;
        }
        set
        {
            localPosition = value - ParentPosition;
        }
    }

    /// <summary>
    /// 親階層のワールド座標
    /// </summary>
    Vector3 ParentPosition => _parent != null ? _parent.position : new Vector3(0, 0, 0);


    public Vector3 localPosition;

    public Vector3 EulerAngles
    {
        get
        {
            return localEulerAngles + ParentEulerAnglse;
        }
        set
        {
            localEulerAngles = value - ParentEulerAnglse;
        }
    }


    public Vector3 localEulerAngles;

    Vector3 ParentEulerAnglse => _parent != null ? _parent.EulerAngles : new Vector3(0, 0, 0);

    public Vector3 localScale = new Vector3(1f, 1f, 1f);

    private Transform _parent;

    public Matrix MMatrix => _mMatrix;
    Matrix _mMatrix;
    Matrix _vpMatrix;
    Matrix _vMatrix;

    Matrix ParentMMatrix => _parent != null ? _parent.MMatrix : Matrix.Identity;


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
        var localScaleMatrix = Matrix.Scaling(localScale);

        // 回転行列
        var localRotateXMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(1, 0, 0), localEulerAngles.X)
        );
        var localRotateYMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(0, 1, 0), localEulerAngles.Y)
        );
        var localRotateZMatrix = Matrix.RotationQuaternion(
            Quaternion.RotationAxis(new Vector3(0, 0, 1), localEulerAngles.Z)
        );
        // ZXYの順で計算
        var localRotateMatrix = localRotateZMatrix * localRotateXMatrix * localRotateYMatrix;

        var localPositionMatrix = Matrix.Identity;
        localPositionMatrix.set_Rows(3, new Vector4(
            localPosition.X,
            localPosition.Y,
            localPosition.Z, 1f));

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