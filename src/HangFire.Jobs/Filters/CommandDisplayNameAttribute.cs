namespace HangFire.Jobs.Filters;

/// <summary>
///     Defines the display name template for a command when enqueued as a Hangfire job
///     via <c>ISender.Send(command)</c>.
///     The template supports <c>{0}</c> placeholder which is replaced by the command's
///     <c>ToString()</c> result.
///     This attribute is read by <see cref="CommandAttributeJobFilter" /> and stored
///     as a Hangfire job parameter.
/// </summary>
/// <example>
///     <code>
///     [CommandDisplayName("Process Asset: {0}")]
///     public sealed record ProcessAssetCommand : IRequest&lt;Result&gt;
///     {
///         public override string ToString() =&gt; $"{Code} - {Name}";
///     }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class CommandDisplayNameAttribute(string displayName) : Attribute
{
    public string DisplayName { get; } = displayName;
}
