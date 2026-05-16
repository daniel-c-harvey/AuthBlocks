namespace GenBlocks.Generators;

/// <summary>
/// Marks an enum for compile-time constant generation.
/// This attribute triggers the source generator to create a Constants class
/// with compile-time constants for each enum value.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GenerateConstantsAttribute : Attribute
{
    /// <summary>
    /// Optional suffix for the generated constants class name.
    /// Default is "Constants".
    /// </summary>
    public string? ClassNameSuffix { get; set; }

    /// <summary>
    /// Whether to generate constants as strings (default) or as the enum type.
    /// </summary>
    public bool GenerateAsEnumType { get; set; } = false;

    public GenerateConstantsAttribute()
    {
    }

    public GenerateConstantsAttribute(string classNameSuffix)
    {
        ClassNameSuffix = classNameSuffix;
    }
} 