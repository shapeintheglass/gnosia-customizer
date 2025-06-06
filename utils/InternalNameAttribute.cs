[System.AttributeUsage(System.AttributeTargets.Property)]
public class InternalNameAttribute : System.Attribute
{
    public string Name { get; }
    public InternalNameAttribute(string name)
    {
        Name = name;
    }
}
