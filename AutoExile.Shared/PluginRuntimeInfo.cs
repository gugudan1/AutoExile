namespace AutoExile
{
    /// <summary>
    /// Small static holder for plugin-level runtime info (e.g. the current
    /// plugin's folder on disk) that shared systems occasionally need for
    /// debug dumps, without depending on any specific mode plugin's type.
    /// Set once by <see cref="PluginHostBase{TSettings}"/> during Initialise.
    /// </summary>
    public static class PluginRuntimeInfo
    {
        public static string PluginDirectory { get; set; } = "";
    }
}
