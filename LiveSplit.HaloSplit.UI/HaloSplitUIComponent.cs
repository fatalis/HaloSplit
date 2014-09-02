using System.Globalization;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.HaloSplit.UI
{
    public class HaloSplitUIComponent : IComponent
    {
        public string ComponentName
        {
            get { return "HaloSplit UI"; }
        }

        public IDictionary<string, Action> ContextMenuControls { get; protected set; }
        protected InfoTextComponent InternalComponent;

        private LiveSplitState _state;
        private int _deaths;

        public HaloSplitUIComponent(LiveSplitState state)
        {
            this.ContextMenuControls = new Dictionary<String, Action>();
            this.InternalComponent = new InfoTextComponent("Death Count", "0");

            _state = state;
            _state.OnReset += state_OnReset;
        }

        public void Dispose()
        {
            _state.OnReset -= state_OnReset;
        }

        public void AddDeath()
        {
            _deaths++;
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            string deaths = _deaths.ToString(CultureInfo.InvariantCulture);

            if (invalidator != null && this.InternalComponent.InformationValue != deaths)
            {
                this.InternalComponent.InformationValue = deaths;
                invalidator.Invalidate(0f, 0f, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region region)
        {
            this.PrepareDraw(state);
            this.InternalComponent.DrawVertical(g, state, width, region);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region region)
        {
            this.PrepareDraw(state);
            this.InternalComponent.DrawHorizontal(g, state, height, region);
        }

        void PrepareDraw(LiveSplitState state)
        {
            this.InternalComponent.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.NameLabel.HasShadow
                = this.InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;
        }

        void state_OnReset(object sender, TimerPhase t)
        {
            _deaths = 0;
        }

        public XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
        public Control GetSettingsControl(LayoutMode mode) { return null; }
        public void SetSettings(XmlNode settings) { }
        public void RenameComparison(string oldName, string newName) { }
        public float MinimumWidth { get { return this.InternalComponent.MinimumWidth; } }
        public float MinimumHeight { get { return this.InternalComponent.MinimumHeight; } }
        public float VerticalHeight { get { return this.InternalComponent.VerticalHeight; } }
        public float HorizontalWidth { get { return this.InternalComponent.HorizontalWidth; } }
        public float PaddingLeft { get { return this.InternalComponent.PaddingLeft; } }
        public float PaddingRight { get { return this.InternalComponent.PaddingRight; } }
        public float PaddingTop { get { return this.InternalComponent.PaddingTop; } }
        public float PaddingBottom { get { return this.InternalComponent.PaddingBottom; } }
    }
}
