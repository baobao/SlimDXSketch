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

    internal Matrix MMatrix => 
        (LocalScaleMatrix * LocalRotateMatrix * LocalPositionMatrix) 
        * (_parent != null ? _parent.MMatrix : Matrix.Identity);

    private Matrix LocalScaleMatrix => Matrix.Scaling(LocalScale);

    private Matrix LocalRotateMatrix
    {
        get
        {
            // X軸回転行列
            var localRotateXMatrix = Matrix.RotationQuaternion(
                Quaternion.RotationAxis(new Vector3(1, 0, 0), LocalEulerAngles.X)
            );
            // Y軸回転行列
            var localRotateYMatrix = Matrix.RotationQuaternion(
                Quaternion.RotationAxis(new Vector3(0, 1, 0), LocalEulerAngles.Y)
            );
            // Z軸回転行列
            var localRotateZMatrix = Matrix.RotationQuaternion(
                Quaternion.RotationAxis(new Vector3(0, 0, 1), LocalEulerAngles.Z)
            );
            // ZXYの順で計算
            return localRotateZMatrix * localRotateXMatrix * localRotateYMatrix;
        }
    }

    private Matrix LocalPositionMatrix
    {
        get
        {
            var localPositionMatrix = Matrix.Identity;
            localPositionMatrix.set_Rows(3, new Vector4(
                LocalPosition.X,
                LocalPosition.Y,
                LocalPosition.Z, 1f));
            return localPositionMatrix;
        }
    }

    public Matrix MVPMatrix => MMatrix * _vpMatrix;

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

    private Matrix GetBillBoardMatrix()
    {
        Matrix ivMatrix = _vMatrix;
        ivMatrix.Invert();
        return ivMatrix;
    }
}