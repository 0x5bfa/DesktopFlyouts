// Polyfill attribute for CommunityToolkit DependencyPropertyGenerator.
// The source generator does not run under Uno.Sdk, so this dummy attribute
// allows the [GeneratedDependencyProperty] usage in shared code to compile.
// Manual dependency property implementations are provided elsewhere.

namespace CommunityToolkit.WinUI;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class GeneratedDependencyPropertyAttribute : Attribute
{
    public object? DefaultValue { get; set; }
}
