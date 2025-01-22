namespace Loupedeck.HololivePlugin
{
    using System;

    public class HololivePlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;
        public override Boolean HasNoApplication => true;

        public HololivePlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            base.Load();
            PluginLog.Info("Loaded Hololive Plugin");
        }
    }
}
