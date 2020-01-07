[System.Serializable]
public class NodeColor
{
    public string name;

    public int offId;
    public int onId;

    public NodeColor(string name,int onId, int offId)
    {
        this.name = name;
        this.offId = offId;
        this.onId = onId;
    }

    public NodeColor(NodeColor source)
    {
        this.name = source.name;
        this.offId = source.offId;
        this.onId = source.onId;
    }
}