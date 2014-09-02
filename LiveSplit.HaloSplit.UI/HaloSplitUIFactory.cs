using System.Reflection;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

namespace LiveSplit.HaloSplit.UI
{
    public class HaloSplitUIFactory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "HaloSplit UI"; }
        }

        public string Description
        {
            get { return "HaloSplit death counter."; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Information; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new HaloSplitUIComponent(state);
        }

        public string UpdateName
        {
            get { return this.ComponentName; }
        }

        public string UpdateURL
        {
            get { return "http://fatalis.hive.ai/livesplit/update/"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL
        {
            get { return this.UpdateURL + "Components/update.LiveSplit.HaloSplit.UI.xml"; }
        }
    }
}
