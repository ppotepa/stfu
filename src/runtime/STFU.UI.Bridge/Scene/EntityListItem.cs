using STFU.Common.Primitives;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Scene;

public sealed class EntityListItem : BindableObject
{
    private string _name;
    private string _role;
    private float _positionX;
    private float _positionY;
    private float _positionZ;
    private float _rotationX;
    private float _rotationY;
    private float _rotationZ;
    private float _scaleX;
    private float _scaleY;
    private float _scaleZ;

    public EntityListItem(
        EntityId id,
        string name,
        string meshLabel,
        string meshSourcePath,
        int vertexCount,
        int triangleCount,
        string status,
        string role,
        float positionX,
        float positionY,
        float positionZ,
        float rotationX,
        float rotationY,
        float rotationZ,
        float scaleX,
        float scaleY,
        float scaleZ,
        SceneMeshBoundsInfo bounds)
    {
        Id = id;
        _name = name;
        MeshLabel = meshLabel;
        MeshSourcePath = meshSourcePath;
        VertexCount = vertexCount;
        TriangleCount = triangleCount;
        Status = status;
        _role = role;
        _positionX = positionX;
        _positionY = positionY;
        _positionZ = positionZ;
        _rotationX = rotationX;
        _rotationY = rotationY;
        _rotationZ = rotationZ;
        _scaleX = scaleX;
        _scaleY = scaleY;
        _scaleZ = scaleZ;
        Bounds = bounds;
    }

    public EntityId Id { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? _name : value);
    }

    public string MeshLabel { get; }

    public string MeshSourcePath { get; }

    public int VertexCount { get; }

    public int TriangleCount { get; }

    public string Status { get; }

    public SceneMeshBoundsInfo Bounds { get; }

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public float PositionX
    {
        get => _positionX;
        set
        {
            if (SetProperty(ref _positionX, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float PositionY
    {
        get => _positionY;
        set
        {
            if (SetProperty(ref _positionY, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float PositionZ
    {
        get => _positionZ;
        set
        {
            if (SetProperty(ref _positionZ, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float RotationX
    {
        get => _rotationX;
        set
        {
            if (SetProperty(ref _rotationX, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float RotationY
    {
        get => _rotationY;
        set
        {
            if (SetProperty(ref _rotationY, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float RotationZ
    {
        get => _rotationZ;
        set
        {
            if (SetProperty(ref _rotationZ, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float ScaleX
    {
        get => _scaleX;
        set
        {
            if (SetProperty(ref _scaleX, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float ScaleY
    {
        get => _scaleY;
        set
        {
            if (SetProperty(ref _scaleY, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public float ScaleZ
    {
        get => _scaleZ;
        set
        {
            if (SetProperty(ref _scaleZ, value))
            {
                RaiseTransformLabels();
            }
        }
    }

    public string IdLabel => $"EntityId({Id.Value})";

    public bool HasMesh => VertexCount > 0 || TriangleCount > 0 || MeshLabel.StartsWith("MeshHandle(", StringComparison.Ordinal);

    public bool IsRenderable => VertexCount > 0 && TriangleCount > 0;

    public string MeshStatsLabel => IsRenderable
        ? $"{VertexCount} vertices / {TriangleCount} triangles"
        : Status;

    public string MeshSourceLabel => string.IsNullOrWhiteSpace(MeshSourcePath)
        ? MeshLabel
        : System.IO.Path.GetFileName(MeshSourcePath);

    public string PositionLabel => $"{PositionX:0.###}, {PositionY:0.###}, {PositionZ:0.###}";

    public string RotationLabel => $"{RotationX:0.###}, {RotationY:0.###}, {RotationZ:0.###}";

    public string ScaleLabel => $"{ScaleX:0.###}, {ScaleY:0.###}, {ScaleZ:0.###}";

    public string TransformSummary => $"P {PositionLabel} / R {RotationLabel} / S {ScaleLabel}";

    private void RaiseTransformLabels()
    {
        OnPropertyChanged(nameof(PositionLabel));
        OnPropertyChanged(nameof(RotationLabel));
        OnPropertyChanged(nameof(ScaleLabel));
        OnPropertyChanged(nameof(TransformSummary));
    }
}
