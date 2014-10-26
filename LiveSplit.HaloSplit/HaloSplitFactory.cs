using System.Reflection;
using LiveSplit.HaloSplit;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

[assembly: ComponentFactory(typeof(HaloSplitFactory))]

namespace LiveSplit.HaloSplit
{
    public class HaloSplitFactory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "HaloSplit"; }
        }

        public string Description
        {
            get { return "Auto-splitter for Halo PC"; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new HaloSplitComponent(state);
        }

        public string UpdateName
        {
            get { return this.ComponentName; }
        }

        public string UpdateURL
        {
            get { return "http://fatalis.pw/livesplit/update/"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL
        {
            get { return this.UpdateURL + "Components/update.LiveSplit.HaloSplit.xml"; }
        }
    }
}
